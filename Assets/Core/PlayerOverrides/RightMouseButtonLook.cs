using UnityEngine;

namespace StarterAssets {

    // Look now always follows the mouse (no right-click hold needed), so the cursor just stays
    // locked the whole time instead of toggling based on the right mouse button.
    public class RightMouseButtonLook : MonoBehaviour
    {
        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

}
