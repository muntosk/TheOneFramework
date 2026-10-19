using UnityEngine;

namespace TheOneFramework.Portals
{
    // Portal's "antlines": low-key dotted lines on the floor and walls running from a button to whatever it
    // drives, blue while it's off and orange once it's on, so the player can see what a button does.
    // Every renderer under this object is a piece of the line; wire TurnOn()/TurnOff() to the
    // button's or plate's events (PoleButton, PressurePlate, RequireAllTriggers, ...).
    public class IndicatorLine : MonoBehaviour
    {
        [SerializeField] private Color offColor = new Color(0.35f, 0.62f, 0.85f);
        [SerializeField] private Color onColor = new Color(0.95f, 0.55f, 0.15f);
        [SerializeField, Min(0.0f)] private float glow = 0.8f;
        [SerializeField] private bool startOn = false;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private Renderer[] pieces;
        private MaterialPropertyBlock block;

        private void Awake()
        {
            pieces = GetComponentsInChildren<Renderer>(true);
            block = new MaterialPropertyBlock();
            Set(startOn);
        }

        public void TurnOn() => Set(true);
        public void TurnOff() => Set(false);

        public void Set(bool on)
        {
            if (pieces == null) return;
            Color color = on ? onColor : offColor;
            block.SetColor(BaseColorId, color);
            block.SetColor(EmissionColorId, color * glow);
            foreach (Renderer piece in pieces)
            {
                piece.SetPropertyBlock(block);
            }
        }
    }
}
