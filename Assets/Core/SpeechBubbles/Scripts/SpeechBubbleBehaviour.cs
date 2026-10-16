using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpeechBubbleBehaviour : MonoBehaviour
{    
    const float MinWidth = 0.1f;
    const float MaxWidth = 2.0f;
    const float MinHeight = 0.12f;
    const float MaxHeight = 1.5f;

    private float borderSize = 0.05f;
    private float tailSize = 0.05f;

    // Bubbles are drawn on top of everything (walls, glass), so someone talking behind a window or
    // around a corner can still be read. Materials are shared between all bubbles.
    private static Material overlayImageMaterial;
    private static readonly System.Collections.Generic.Dictionary<Material, Material> overlayTextMaterials = new System.Collections.Generic.Dictionary<Material, Material>();

    void Awake() {
        int always = (int)UnityEngine.Rendering.CompareFunction.Always;
        foreach (var image in GetComponentsInChildren<UnityEngine.UI.Image>(true)) {
            if (overlayImageMaterial == null) {
                overlayImageMaterial = new Material(image.material);
                overlayImageMaterial.SetInt("unity_GUIZTestMode", always);
                overlayImageMaterial.renderQueue = 4000;
            }
            image.material = overlayImageMaterial;
        }
        foreach (var text in GetComponentsInChildren<TextMeshPro>(true)) {
            Material source = text.fontSharedMaterial;
            if (!overlayTextMaterials.TryGetValue(source, out Material overlay) || overlay == null) {
                overlay = new Material(source);
                overlay.SetInt("unity_GUIZTestMode", always);
                overlay.renderQueue = 4001;
                overlayTextMaterials[source] = overlay;
            }
            text.fontSharedMaterial = overlay;
        }
    }

    public float GetHeight() {
        var bodyNode = transform.Find("Body");
        //0.02=border 0.12=tail size
        return bodyNode.GetComponent<RectTransform>().sizeDelta.y + borderSize + tailSize;
    }

    public void SetMessage(string text) {
        var bodyNode = transform.Find("Body");
        var textNode = bodyNode.Find("Text");
        var textMeshPro = textNode.GetComponent<TextMeshPro>(); 

        Vector2 preferredValues = textMeshPro.GetPreferredValues(text, MaxWidth, float.MaxValue);
        float width = Mathf.Min(MaxWidth, preferredValues.x);
        float height = preferredValues.y;
        if (width < MinWidth) width = MinWidth;
        if (height < MinHeight) height = MinHeight;
        if (height > MaxHeight) height = MaxHeight;
        bodyNode.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        textMeshPro.text = text;
    }
}
