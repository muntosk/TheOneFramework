using System;
using TMPro;
using UnityEngine;

// Gives each speaker's name in the dialogue its own colour (S.A.F.E.-T cool white-blue, Wes warm orange, ...),
// like Portal's subtitles. Sits next to the LinePresenter and recolours the name whenever it changes.
public class DialogueCharacterColors : MonoBehaviour
{
    [Serializable]
    public struct Speaker
    {
        public string name;
        public Color color;
    }

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Color defaultColor = new Color(0.85f, 0.85f, 0.85f);
    [SerializeField] private Speaker[] speakers =
    {
        new Speaker { name = "S.A.F.E.-T", color = new Color(0.62f, 0.86f, 1.0f) },
        new Speaker { name = "Wes", color = new Color(1.0f, 0.72f, 0.38f) },
    };

    private string lastName;

    private void LateUpdate()
    {
        if (nameText == null || nameText.text == lastName) return;
        lastName = nameText.text;

        Color color = defaultColor;
        foreach (Speaker speaker in speakers)
        {
            if (string.Equals(speaker.name, lastName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                color = speaker.color;
                break;
            }
        }
        nameText.color = color;
    }
}
