using Unity.Cinemachine;
using UnityEngine;
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

        private Camera cam;
        private CinemachineBrain brain;
        private ThirdPersonControllerFixed thirdPersonController;
        private float thirdPersonFieldOfView;
        private bool isFirstPerson;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            brain = GetComponent<CinemachineBrain>();
            thirdPersonController = FindFirstObjectByType<ThirdPersonControllerFixed>();
            thirdPersonFieldOfView = cam.fieldOfView;
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
            brain.enabled = !isFirstPerson;
            cam.fieldOfView = isFirstPerson ? firstPersonFieldOfView : thirdPersonFieldOfView;
        }
    }
}
