using UnityEngine;
using TMPro;

public class LogTextSelection : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 10/08/2026
     */

    [System.Serializable]
    private class LogButtonTexts
    {
        public TMP_Text[] texts;
    }

    [Header("Log Buttons")]
    [SerializeField] private LogButtonTexts[] logButtons;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color selectedColor = Color.black;


    public void SelectLog(int index)
    {
        for (int i = 0; i < logButtons.Length; i++)
        {
            foreach (TMP_Text text in logButtons[i].texts)
            {
                if (text == null)
                    continue;

                text.color = i == index
                    ? selectedColor
                    : normalColor;
            }
        }
    }
}