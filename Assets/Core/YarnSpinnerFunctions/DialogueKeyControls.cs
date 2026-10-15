using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Yarn.Unity;

// Keyboard control for Yarn's option list in every level: E picks the highlighted option (W/S or the
// arrow keys move the highlight, the mouse still works too). Also puts a small key hint under the
// line box ("E continue") and the option box ("W/S select, E choose"). The Dialogue System prefab
// lives in the Yarn package, so this attaches itself to its presenters whenever a scene loads.
public class DialogueKeyControls : MonoBehaviour
{
    private const string LineHint = "<b>E</b>  continue";
    private const string OptionsHint = "<b>W/S</b>  select      <b>E</b>  choose";

    private static bool hooked;

    private CanvasGroup optionsGroup;
    private Transform hint;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (!hooked)
        {
            hooked = true;
            SceneManager.sceneLoaded += (scene, mode) => AttachAll();
        }
        AttachAll();
    }

    private static void AttachAll()
    {
        foreach (var options in FindObjectsByType<OptionsPresenter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (options.GetComponent<DialogueKeyControls>() != null) continue;
            var controls = options.gameObject.AddComponent<DialogueKeyControls>();
            controls.optionsGroup = options.GetComponent<CanvasGroup>();
            controls.hint = AddHint(options.transform, OptionsHint);
        }

        foreach (var line in FindObjectsByType<LinePresenter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (line.transform.Find("KeyHint") == null) AddHint(line.transform, LineHint);
        }
    }

    // A small grey line of text at the bottom of a presenter's layout, in the presenter's own font.
    private static Transform AddHint(Transform parent, string text)
    {
        var template = parent.GetComponentInChildren<TextMeshProUGUI>(true);
        var label = new GameObject("KeyHint", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(parent, false);
        if (template != null)
        {
            label.font = template.font;
            label.fontSize = template.fontSize * 0.75f;
        }
        label.text = text;
        label.color = new Color(1.0f, 1.0f, 1.0f, 0.75f);
        label.alignment = TextAlignmentOptions.Right;
        label.raycastTarget = false;
        label.transform.SetAsLastSibling();
        return label.transform;
    }

    private void Update()
    {
        // The option items are spawned below the hint; keep the hint at the bottom.
        if (hint != null && hint.GetSiblingIndex() != transform.childCount - 1) hint.SetAsLastSibling();

        bool shown = optionsGroup == null || (optionsGroup.interactable && optionsGroup.alpha > 0.99f);
        if (!shown || EventSystem.current == null || Keyboard.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !selected.transform.IsChildOf(transform))
        {
            // Nothing highlighted (e.g. the mouse took it away): highlight the first option again.
            var first = GetComponentInChildren<OptionItem>();
            if (first != null) first.Select();
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }
    }
}
