using UnityEngine;
using UnityEngine.UI;

namespace TheOneFramework.Portals
{
    public class Crosshair : MonoBehaviour
    {
        [SerializeField]
        private PortalPair portalPair;

        [SerializeField]
        private Image inPortalImg;

        [SerializeField]
        private Image outPortalImg;

        [SerializeField]
        private Color defaultColour = Color.gray;

        private Color inPortalColour;
        private Color outPortalColour;

        private void Start()
        {
            var portals = portalPair.Portals;

            // Force full opacity regardless of Portal.PortalColour's own alpha - that value is also
            // fed into the outline shader, which doesn't use alpha, so a forgotten alpha=0 there
            // silently makes the outline still look fine while any UI Image using it stays invisible.
            inPortalColour = WithFullAlpha(portals[0].PortalColour);
            outPortalColour = WithFullAlpha(portals[1].PortalColour);

            // Crosshair is always visible (grey by default) instead of only appearing once a
            // portal is placed, so there's always something to aim with.
            inPortalImg.color = defaultColour;
            outPortalImg.color = defaultColour;
        }

        private static Color WithFullAlpha(Color color)
        {
            color.a = 1.0f;
            return color;
        }

        public void SetPortalPlaced(int portalID, bool isPlaced)
        {
            if (portalID == 0)
            {
                inPortalImg.color = isPlaced ? inPortalColour : defaultColour;
            }
            else
            {
                outPortalImg.color = isPlaced ? outPortalColour : defaultColour;
            }
        }
    }
}
