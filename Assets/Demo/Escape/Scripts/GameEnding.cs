using System.Collections;
using StarterAssets;
using TheOneFramework.Portals;
using TheOneFramework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

// The very end: fades to black, shows the title and a few credit lines, then goes back to the main
// menu. Started from Yarn with <<end_game Ending>> (Ending = this object's name).
public class GameEnding : MonoBehaviour
{
    [SerializeField] private string title = "You Are No Longer Required";
    [SerializeField, TextArea] private string credits = "Saxion - Unity Fundamentals & Design Fundamentals\nThanks for playing.";
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private string menuScene = "MainMenu";
    [SerializeField] private float fadeTime = 2.5f;
    [SerializeField] private float holdTime = 6.0f;

    private bool running;

    [YarnCommand("end_game")]
    public void EndGame()
    {
        if (!running) StartCoroutine(EndRoutine());
    }

    private IEnumerator EndRoutine()
    {
        running = true;

        var player = FindAnyObjectByType<ThirdPersonControllerFixed>();
        if (player != null)
        {
            player.enabled = false;
            var gun = player.GetComponent<PortalGun>();
            if (gun != null) gun.enabled = false;
        }

        yield return ScreenFade.Out(fadeTime);

        // The title card brings its own black background, so the shared fade (which sits on top of
        // every other canvas) can be cleared - otherwise it would hide the card and the menu after it.
        var canvasObject = new GameObject("EndCard", typeof(Canvas));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var background = new GameObject("Background", typeof(RectTransform)).AddComponent<UnityEngine.UI.Image>();
        background.transform.SetParent(canvasObject.transform, false);
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.sizeDelta = Vector2.zero;
        background.color = Color.black;

        var textGroup = new GameObject("Texts", typeof(RectTransform)).AddComponent<CanvasGroup>();
        textGroup.transform.SetParent(canvasObject.transform, false);
        textGroup.alpha = 0.0f;
        AddText(textGroup.transform, title, 64, new Vector2(0.0f, 40.0f));
        AddText(textGroup.transform, credits, 26, new Vector2(0.0f, -60.0f));

        yield return ScreenFade.In(0.05f);

        for (float t = 0.0f; t < 1.5f; t += Time.unscaledDeltaTime)
        {
            textGroup.alpha = t / 1.5f;
            yield return null;
        }
        textGroup.alpha = 1.0f;

        yield return new WaitForSecondsRealtime(holdTime);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(menuScene);
    }

    private void AddText(Transform parent, string text, float size, Vector2 offset)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1400.0f, 200.0f);
        rect.anchoredPosition = offset;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }
}
