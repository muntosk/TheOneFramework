using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TheOneFramework.EditorTools
{
    // One-click wiring of the Portal 2 sounds in Core/Portal/Audio onto every component that has
    // audio slots, in all portal prefabs and all scenes under Demo/Scenes. Prefabs are done first;
    // scene objects that are instances of those prefabs are skipped so they keep inheriting the
    // prefab's clips instead of getting per-scene overrides. Safe to run again - it just reassigns.
    public static class AssignPortalSounds
    {
        private const string AudioRoot = "Assets/Core/Portal/Audio/";
        private static readonly string[] PrefabRoots = { "Assets/Core/Portal/Prefabs", "Assets/Demo" };
        private const string SceneRoot = "Assets/Demo/Scenes";

        // Component type name -> (serialized field -> clip path(s) relative to AudioRoot).
        // A path ending in '*' means "every clip in that folder whose name starts with this".
        private static readonly Dictionary<string, Dictionary<string, string[]>> Mapping =
            new Dictionary<string, Dictionary<string, string[]>>
            {
                ["PortalGun"] = new Dictionary<string, string[]>
                {
                    ["shootPortal1Clips"] = new[] { "PortalGun/wpn_portal_gun_fire_blue_*" },
                    ["shootPortal2Clips"] = new[] { "PortalGun/wpn_portal_gun_fire_red_*" },
                    ["portalOpenClips"] = new[] { "PortalGun/portal_open*" },
                    ["invalidSurfaceClips"] = new[] { "PortalGun/portal_invalid_surface_*" },
                },
                ["ThirdPersonControllerFixed"] = new Dictionary<string, string[]>
                {
                    ["FootstepAudioClips"] = new[] { "Player/p2_fs_walk_tile_*" },
                    ["LandingAudioClip"] = new[] { "Player/p2_fs_jump_land_tile_01.wav" },
                },
                ["Door"] = new Dictionary<string, string[]>
                {
                    ["openClip"] = new[] { "Door/horizontal_sliding_door_open_01.wav" },
                    ["closeClip"] = new[] { "Door/horizontal_sliding_door_close_01.wav" },
                },
                ["PoleButton"] = new Dictionary<string, string[]>
                {
                    ["pressClip"] = new[] { "Button/og_switch_press_01.wav" },
                },
                ["PressurePlate"] = new Dictionary<string, string[]>
                {
                    ["pressClip"] = new[] { "Button/portal_button_down_01.wav" },
                    ["releaseClip"] = new[] { "Button/portal_button_up_01.wav" },
                },
                ["CubeDispenser"] = new Dictionary<string, string[]>
                {
                    ["dispenseClip"] = new[] { "Dispenser/dropper_iris_open_01.wav" },
                },
                ["PortalFizzleField"] = new Dictionary<string, string[]>
                {
                    ["loopClip"] = new[] { "Fizzler/fizzler_lp_01.wav" },
                    ["portalFizzleClips"] = new[] { "PortalGun/portal_fizzle_*" },
                    ["objectFizzleClip"] = new[] { "Fizzler/material_emancipation_01.wav" },
                },
                ["ExtendingPlatform"] = new Dictionary<string, string[]>
                {
                    ["extendClip"] = new[] { "Platform/og_ramp_raise_01.wav" },
                    ["retractClip"] = new[] { "Platform/og_ramp_lower_01.wav" },
                },
                ["Lift"] = new Dictionary<string, string[]>
                {
                    ["startClip"] = new[] { "Lift/elevator_start1.wav" },
                    ["travelLoopClip"] = new[] { "Lift/elevator_move_loop1.wav" },
                    ["stopClip"] = new[] { "Lift/elevator_stop1.wav" },
                },
            };

        [MenuItem("Tools/Portal/Assign Portal 2 Sounds")]
        private static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            int changed = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", PrefabRoots))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                int count = AssignAll(root.GetComponentsInChildren<MonoBehaviour>(true));
                if (count > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed += count;
                    Debug.Log($"[AssignPortalSounds] {path}: {count} component(s)");
                }
                PrefabUtility.UnloadPrefabContents(root);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { SceneRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var components = new List<MonoBehaviour>();
                foreach (GameObject go in scene.GetRootGameObjects())
                {
                    components.AddRange(go.GetComponentsInChildren<MonoBehaviour>(true));
                }

                int count = AssignAll(components);
                if (count > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    changed += count;
                    Debug.Log($"[AssignPortalSounds] {path}: {count} component(s)");
                }
            }

            if (originalSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }

            Debug.Log($"[AssignPortalSounds] Done - assigned sounds on {changed} component(s).");
        }

        private static int AssignAll(IEnumerable<MonoBehaviour> components)
        {
            int count = 0;
            foreach (MonoBehaviour component in components)
            {
                if (component == null || !Mapping.TryGetValue(component.GetType().Name, out var fields))
                {
                    continue;
                }

                // Instance of one of our own prefabs: that prefab gets filled in itself, so let
                // the instance inherit from it rather than writing an override.
                if (IsFromOwnPrefab(component))
                {
                    continue;
                }

                var so = new SerializedObject(component);
                foreach (var field in fields)
                {
                    SerializedProperty prop = so.FindProperty(field.Key);
                    if (prop == null)
                    {
                        Debug.LogWarning($"[AssignPortalSounds] {component.GetType().Name} has no field '{field.Key}'");
                        continue;
                    }

                    List<AudioClip> clips = LoadClips(field.Value);
                    if (prop.isArray)
                    {
                        prop.arraySize = clips.Count;
                        for (int i = 0; i < clips.Count; i++)
                        {
                            prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
                        }
                    }
                    else
                    {
                        prop.objectReferenceValue = clips.Count > 0 ? clips[0] : null;
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                count++;
            }
            return count;
        }

        private static bool IsFromOwnPrefab(Component component)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(component);
            if (source == null)
            {
                return false;
            }

            string path = AssetDatabase.GetAssetPath(source);
            foreach (string root in PrefabRoots)
            {
                if (path.StartsWith(root + "/")) return true;
            }
            return false;
        }

        private static List<AudioClip> LoadClips(string[] patterns)
        {
            var clips = new List<AudioClip>();
            foreach (string pattern in patterns)
            {
                if (!pattern.EndsWith("*"))
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + pattern);
                    if (clip != null) clips.Add(clip);
                    else Debug.LogWarning($"[AssignPortalSounds] Missing clip {AudioRoot + pattern}");
                    continue;
                }

                string folder = AudioRoot + Path.GetDirectoryName(pattern).Replace('\\', '/');
                string prefix = Path.GetFileName(pattern).TrimEnd('*');
                var paths = new List<string>();
                foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetFileName(path).StartsWith(prefix)) paths.Add(path);
                }
                paths.Sort();
                foreach (string path in paths)
                {
                    clips.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
                }
            }
            return clips;
        }
    }
}
