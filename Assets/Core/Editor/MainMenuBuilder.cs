using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheOneFramework.Audio;
using TheOneFramework.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace TheOneFramework.EditorTools
{
    // Builds the start menu scene (Demo/Scenes/MainMenu.unity): a screenshot of the level as
    // background, title, Start / Settings / Quit, volume sliders and menu music. The background is
    // Demo/UI/Textures/MenuBackground.png - Capture Menu Background overwrites that file from the
    // Scene view, so the menu picks up a new screenshot without rebuilding.
    public static class MainMenuBuilder
    {
        private const string ScenePath = "Assets/Demo/Scenes/MainMenu.unity";
        private const string LevelsFolder = "Assets/Demo/Scenes/Levels";
        private const string TextureFolder = "Assets/Demo/UI/Textures";
        private const string BackgroundPath = TextureFolder + "/MenuBackground.png";
        private const string GradientPath = TextureFolder + "/MenuGradient.png";
        private const string FontFolder = "Assets/Demo/UI/Fonts/TitilliumWeb";
        private const string MusicPath = "Assets/Demo/Audio/Music/Surface_Start.ogg";

        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        private static readonly Color Accent = new Color(0.25f, 0.66f, 1.0f);
        private static readonly Color TextColor = new Color(0.94f, 0.96f, 0.98f);
        private static readonly Color DimText = new Color(0.94f, 0.96f, 0.98f, 0.6f);

        [MenuItem("Tools/UI/Capture Menu Background (Scene View)")]
        private static void CaptureBackground()
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null || view.camera == null)
            {
                EditorUtility.DisplayDialog("Capture Menu Background", "Open a Scene view first and frame the shot you want.", "OK");
                return;
            }

            EnsureFolder(TextureFolder);

            // A temporary game camera at the Scene view's position, so post-processing and the
            // skybox render like in the game (the Scene view camera itself has gizmos/grid).
            var go = new GameObject("MenuCapture") { hideFlags = HideFlags.HideAndDontSave };
            var rt = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            try
            {
                Camera source = view.camera;
                Camera cam = go.AddComponent<Camera>();
                cam.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                cam.fieldOfView = source.fieldOfView;
                cam.nearClipPlane = source.nearClipPlane;
                cam.farClipPlane = Mathf.Max(source.farClipPlane, 1000.0f);
                cam.targetTexture = rt;
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

                var request = new RenderPipeline.StandardRequest { destination = rt };
                if (RenderPipeline.SupportsRenderRequest(cam, request))
                {
                    RenderPipeline.SubmitRenderRequest(cam, request);
                }
                else
                {
                    cam.Render();
                }

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;

                File.WriteAllBytes(BackgroundPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                Object.DestroyImmediate(go);
                rt.Release();
                Object.DestroyImmediate(rt);
            }

            ImportAsSprite(BackgroundPath);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath));
            Debug.Log($"[MainMenu] Background saved to {BackgroundPath}.");
        }

        [MenuItem("Tools/UI/Build Main Menu Scene")]
        private static void Build()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Build Main Menu Scene",
                    "MainMenu.unity already exists and will be rebuilt (changes made by hand in that scene are lost).", "Rebuild", "Cancel"))
            {
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(TextureFolder);
            if (!File.Exists(BackgroundPath)) WritePlaceholderBackground();
            if (!File.Exists(GradientPath)) WriteGradient();

            TMP_FontAsset regular = FontAsset("TitilliumWeb-Regular");
            TMP_FontAsset bold = FontAsset("TitilliumWeb-Bold");

            SceneAsset firstLevel = FirstLevel();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Camera / systems ---
            Label("=== CAMERA ===");
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            var musicGo = new GameObject("MenuMusic");
            AudioSource music = musicGo.AddComponent<AudioSource>();
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0.0f;
            music.volume = 0.8f;

            Spacer();
            Label("=== UI ===");

            // --- Canvas ---
            var canvasGo = new GameObject("MenuCanvas", typeof(RectTransform));
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            RectTransform root = (RectTransform)canvasGo.transform;

            // Background screenshot, covering the screen at any aspect ratio without stretching.
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            RectTransform bg = UIObject("Background", root);
            Center(bg);
            Image bgImage = bg.gameObject.AddComponent<Image>();
            bgImage.sprite = bgSprite;
            bgImage.raycastTarget = false;
            AspectRatioFitter fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = bgSprite != null ? bgSprite.rect.width / bgSprite.rect.height : 16.0f / 9.0f;

            // Dark gradient on the left so the text stays readable on any screenshot.
            RectTransform shade = UIObject("Shade", root);
            Stretch(shade);
            Image shadeImage = shade.gameObject.AddComponent<Image>();
            shadeImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GradientPath);
            shadeImage.color = Color.black;
            shadeImage.raycastTarget = false;

            // --- Main panel ---
            RectTransform main = Panel("MainPanel", root);

            Text(main, "Kicker", "SAFE FUTURE PROGRAM", regular, 26, Accent, 12.0f);
            Text(main, "Title", "YOU ARE NO\nLONGER REQUIRED", bold, 84, TextColor, 2.0f, 0.85f);
            Text(main, "Subtitle", "Volunteer 32, please proceed to the elevator.", regular, 28, DimText);
            Gap(main, 40);

            var menu = canvasGo.AddComponent<MainMenu>();
            Button start = MenuButton(main, "StartButton", "START", bold);
            Button settings = MenuButton(main, "SettingsButton", "SETTINGS", bold);
            Button quit = MenuButton(main, "QuitButton", "QUIT", bold);
            UnityEventTools.AddPersistentListener(start.onClick, menu.StartGame);
            UnityEventTools.AddPersistentListener(settings.onClick, menu.ShowSettings);
            UnityEventTools.AddPersistentListener(quit.onClick, menu.Quit);

            // --- Settings panel ---
            RectTransform settingsPanel = Panel("SettingsPanel", root);
            Text(settingsPanel, "Title", "SETTINGS", bold, 64, TextColor, 4.0f);
            Text(settingsPanel, "Subtitle", "Volume", regular, 28, DimText);
            Gap(settingsPanel, 20);
            VolumeRow(settingsPanel, "Master", MixerVolume.Master, regular);
            VolumeRow(settingsPanel, "Music", MixerVolume.Music, regular);
            VolumeRow(settingsPanel, "Effects", MixerVolume.Sfx, regular);
            VolumeRow(settingsPanel, "Voice", MixerVolume.Voice, regular);
            VolumeRow(settingsPanel, "Ambience", MixerVolume.Ambience, regular);
            Gap(settingsPanel, 30);
            Button back = MenuButton(settingsPanel, "BackButton", "BACK", bold);
            UnityEventTools.AddPersistentListener(back.onClick, menu.ShowMain);
            settingsPanel.gameObject.SetActive(false);

            // Footer line, bottom left.
            RectTransform footer = UIObject("Footer", root);
            footer.anchorMin = footer.anchorMax = new Vector2(0, 0);
            footer.pivot = new Vector2(0, 0);
            footer.anchoredPosition = new Vector2(140, 50);
            footer.sizeDelta = new Vector2(1200, 40);
            TextMeshProUGUI footerText = footer.gameObject.AddComponent<TextMeshProUGUI>();
            Style(footerText, "S.A.F.E.-T  ·  Your safety is our number one priority.", regular, 22, DimText);

            // Black fade over everything, last so it draws on top.
            RectTransform fade = UIObject("Fade", root);
            Stretch(fade);
            Image fadeImage = fade.gameObject.AddComponent<Image>();
            fadeImage.color = Color.black;
            CanvasGroup fadeGroup = fade.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0.0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;

            menu.SetFirstLevel(firstLevel);
            menu.mainPanel = main.gameObject;
            menu.settingsPanel = settingsPanel.gameObject;
            menu.fade = fadeGroup;
            menu.background = bg;
            menu.music = music;

            eventSystem.GetComponent<EventSystem>().firstSelectedGameObject = start.gameObject;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"[MainMenu] Built {ScenePath}. Start loads '{(firstLevel != null ? firstLevel.name : "nothing")}' (change it on MenuCanvas > MainMenu).");
        }

        // ---------- UI pieces ----------

        private static RectTransform Panel(string name, RectTransform parent)
        {
            RectTransform panel = UIObject(name, parent);
            panel.anchorMin = new Vector2(0, 0);
            panel.anchorMax = new Vector2(0, 1);
            panel.pivot = new Vector2(0, 0.5f);
            panel.anchoredPosition = new Vector2(140, 0);
            panel.sizeDelta = new Vector2(820, 0);

            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return panel;
        }

        private static TextMeshProUGUI Text(RectTransform parent, string name, string text, TMP_FontAsset font,
            float size, Color color, float spacing = 0.0f, float lineSpacingScale = 1.0f)
        {
            RectTransform rt = UIObject(name, parent);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            Style(tmp, text, font, size, color);
            tmp.characterSpacing = spacing;
            tmp.lineSpacing = (lineSpacingScale - 1.0f) * 100.0f;
            return tmp;
        }

        private static void Style(TextMeshProUGUI tmp, string text, TMP_FontAsset font, float size, Color color)
        {
            tmp.text = text;
            if (font != null) tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
        }

        private static Button MenuButton(RectTransform parent, string name, string label, TMP_FontAsset font)
        {
            RectTransform rt = UIObject(name, parent);
            LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 420;
            le.preferredHeight = 64;

            Image image = rt.gameObject.AddComponent<Image>();
            image.color = Color.white;

            Button button = rt.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1, 1, 1, 0.06f);
            colors.highlightedColor = new Color(Accent.r, Accent.g, Accent.b, 0.9f);
            colors.selectedColor = new Color(Accent.r, Accent.g, Accent.b, 0.9f);
            colors.pressedColor = new Color(Accent.r * 0.7f, Accent.g * 0.7f, Accent.b * 0.7f, 1.0f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            // Thin accent bar on the left edge.
            RectTransform bar = UIObject("Bar", rt);
            bar.anchorMin = new Vector2(0, 0);
            bar.anchorMax = new Vector2(0, 1);
            bar.pivot = new Vector2(0, 0.5f);
            bar.sizeDelta = new Vector2(4, 0);
            Image barImage = bar.gameObject.AddComponent<Image>();
            barImage.color = Accent;
            barImage.raycastTarget = false;

            RectTransform textRt = UIObject("Label", rt);
            Stretch(textRt);
            textRt.offsetMin = new Vector2(28, 0);
            TextMeshProUGUI tmp = textRt.gameObject.AddComponent<TextMeshProUGUI>();
            Style(tmp, label, font, 30, TextColor);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.characterSpacing = 8.0f;
            return button;
        }

        private static void VolumeRow(RectTransform parent, string label, MixerVolume volume, TMP_FontAsset font)
        {
            RectTransform row = UIObject(label + "Volume", parent);
            LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 640;
            le.preferredHeight = 50;
            HorizontalLayoutGroup h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 20;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            TextMeshProUGUI text = Text(row, "Label", label.ToUpperInvariant(), font, 24, TextColor, 4.0f);
            LayoutElement textLe = text.gameObject.AddComponent<LayoutElement>();
            textLe.preferredWidth = 200;

            var resources = new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            };
            GameObject sliderGo = DefaultControls.CreateSlider(resources);
            sliderGo.name = "Slider";
            sliderGo.transform.SetParent(row, false);
            LayoutElement sliderLe = sliderGo.AddComponent<LayoutElement>();
            sliderLe.preferredWidth = 400;
            sliderLe.preferredHeight = 20;

            Slider slider = sliderGo.GetComponent<Slider>();
            sliderGo.transform.Find("Background").GetComponent<Image>().color = new Color(1, 1, 1, 0.15f);
            sliderGo.transform.Find("Fill Area/Fill").GetComponent<Image>().color = Accent;
            slider.targetGraphic.color = Color.white;
            ColorBlock colors = slider.colors;
            colors.highlightedColor = Accent;
            colors.selectedColor = Accent;
            slider.colors = colors;

            VolumeSlider vs = sliderGo.AddComponent<VolumeSlider>();
            var so = new SerializedObject(vs);
            so.FindProperty("volume").enumValueIndex = (int)volume;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Gap(RectTransform parent, float height)
        {
            RectTransform rt = UIObject("Gap", parent);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private static RectTransform UIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Center(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void Label(string text) => new GameObject(text);

        private static void Spacer() => new GameObject("   ");

        // ---------- Assets ----------

        // Makes "<font> SDF.asset" next to the .ttf the first time (dynamic, so any character works).
        private static TMP_FontAsset FontAsset(string fontName)
        {
            string path = $"{FontFolder}/{fontName} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/{fontName}.ttf");
            if (font == null)
            {
                Debug.LogWarning($"[MainMenu] {fontName}.ttf not found, using the default TMP font.");
                return null;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
            asset.name = fontName + " SDF";
            AssetDatabase.CreateAsset(asset, path);
            asset.atlasTextures[0].name = fontName + " SDF Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = fontName + " SDF Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void WritePlaceholderBackground()
        {
            var tex = new Texture2D(CaptureWidth / 4, CaptureHeight / 4, TextureFormat.RGB24, false);
            var top = new Color(0.16f, 0.2f, 0.26f);
            var bottom = new Color(0.05f, 0.06f, 0.08f);
            for (int y = 0; y < tex.height; y++)
            {
                Color c = Color.Lerp(bottom, top, y / (float)tex.height);
                for (int x = 0; x < tex.width; x++) tex.SetPixel(x, y, c);
            }
            File.WriteAllBytes(BackgroundPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            ImportAsSprite(BackgroundPath);
        }

        // Black on the left fading to see-through on the right (only the alpha is used).
        private static void WriteGradient()
        {
            var tex = new Texture2D(256, 4, TextureFormat.RGBA32, false);
            for (int x = 0; x < tex.width; x++)
            {
                float t = x / (float)(tex.width - 1);
                float a = Mathf.Lerp(0.85f, 0.1f, Mathf.SmoothStep(0.0f, 1.0f, t));
                for (int y = 0; y < tex.height; y++) tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            File.WriteAllBytes(GradientPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            ImportAsSprite(GradientPath);
        }

        private static void ImportAsSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        // The level Start loads by default: the first scene in Demo/Scenes/Levels.
        private static SceneAsset FirstLevel()
        {
            string first = AssetDatabase.FindAssets("t:SceneAsset", new[] { LevelsFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p, System.StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            return first != null ? AssetDatabase.LoadAssetAtPath<SceneAsset>(first) : null;
        }

        private static void AddToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes.Where(s => s.path != ScenePath));
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
