using TMPro;
using UnityEngine;

namespace TheOneFramework.Portals
{
    // Portal-style test chamber sign: a big chamber number, "/ total" and a progress bar. Set the
    // numbers here or on the ChamberShell (which passes them on). Built by Tools > Portal > Build Level Kit.
    [ExecuteAlways]
    public class ChamberSign : MonoBehaviour
    {
        [Min(0)] public int number = 1;
        [Min(1)] public int total = 19;

        [SerializeField] private TMP_Text numberText;
        [SerializeField] private TMP_Text totalText;
        [Tooltip("Scaled along X from 0 to 1 with number / total; pivot on its left edge.")]
        [SerializeField] private Transform progressFill;

        public void Set(int chamber, int chambers)
        {
            number = chamber;
            total = chambers;
            Apply();
        }

        private void OnEnable() => Apply();
        private void OnValidate() => Apply();

        private void Apply()
        {
            if (numberText != null) numberText.text = number.ToString("00");
            if (totalText != null) totalText.text = "/" + total.ToString("00");
            if (progressFill != null)
            {
                Vector3 s = progressFill.localScale;
                s.x = Mathf.Clamp01(total > 0 ? number / (float)total : 0.0f);
                progressFill.localScale = s;
            }
        }
    }
}
