using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    // Switches the Main Camera between the existing Cinemachine third-person rig and a plain
    // first-person view. First person just mirrors CinemachineCameraTarget's transform directly -
    // that's the same transform ThirdPersonControllerFixed.CameraRotation() already drives from
    // mouse look, so no separate first-person look logic is needed.
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(CinemachineBrain))]
    public class CameraModeToggle : MonoBehaviour
    {
        [SerializeField]
        private float firstPersonFieldOfView = 75.0f;

        [Tooltip("The player's body meshes are moved to this layer and the camera stops rendering it in first person, so you don't see the inside of your own head. Portal cameras still render it, so you can see yourself through portals. If the layer doesn't exist the body is switched to shadows-only instead.")]
        [SerializeField]
        private string hiddenBodyLayerName = "FirstPersonHidden";

        private Camera cam;
        private CinemachineBrain brain;
        private ThirdPersonControllerFixed thirdPersonController;
        private float thirdPersonFieldOfView;
        private int thirdPersonCullingMask;
        private int hiddenBodyLayer = -1;
        private SkinnedMeshRenderer[] bodyRenderers;

        // Static so it belongs to the class rather than this scene's camera: it survives scene loads,
        // so the next level (e.g. after a Lift) starts in the same view the player left in.
        private static bool isFirstPerson;

        // Domain reload is off in this project, so statics would otherwise carry over between
        // Play sessions in the editor too. Start each Play session in third person again.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            isFirstPerson = false;
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            brain = GetComponent<CinemachineBrain>();
            thirdPersonController = FindFirstObjectByType<ThirdPersonControllerFixed>();
            thirdPersonFieldOfView = cam.fieldOfView;
            thirdPersonCullingMask = cam.cullingMask;
            SetupBodyHiding();
            ApplyMode();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
            {
                Toggle();
            }
#endif
        }

        private void LateUpdate()
        {
            if (!isFirstPerson)
            {
                return;
            }

            // The Cinemachine Brain is disabled while in first person, so nothing else is
            // driving this transform - update it after Cinemachine's own LateUpdate would have run.
            Transform target = thirdPersonController.CinemachineCameraTarget.transform;
            transform.SetPositionAndRotation(target.position, target.rotation);
        }

        public void Toggle()
        {
            isFirstPerson = !isFirstPerson;
            ApplyMode();
        }

        private void ApplyMode()
        {
            brain.enabled = !isFirstPerson;
            cam.fieldOfView = isFirstPerson ? firstPersonFieldOfView : thirdPersonFieldOfView;
            ApplyBodyVisibility();
        }

        private void SetupBodyHiding()
        {
            if (thirdPersonController == null)
            {
                return;
            }

            bodyRenderers = thirdPersonController.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            hiddenBodyLayer = LayerMask.NameToLayer(hiddenBodyLayerName);

            if (hiddenBodyLayer < 0)
            {
                Debug.LogWarning($"[CameraModeToggle] Layer '{hiddenBodyLayerName}' does not exist - add it under Tags & Layers. Falling back to shadows-only body in first person (it won't show in portals either).");
                return;
            }

            foreach (var bodyRenderer in bodyRenderers)
            {
                bodyRenderer.gameObject.layer = hiddenBodyLayer;
            }
        }

        private void ApplyBodyVisibility()
        {
            if (hiddenBodyLayer >= 0)
            {
                cam.cullingMask = isFirstPerson ? thirdPersonCullingMask & ~(1 << hiddenBodyLayer) : thirdPersonCullingMask;
                return;
            }

            if (bodyRenderers == null)
            {
                return;
            }

            foreach (var bodyRenderer in bodyRenderers)
            {
                bodyRenderer.shadowCastingMode = isFirstPerson ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }
        }
    }
}
