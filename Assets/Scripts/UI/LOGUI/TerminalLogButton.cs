using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TerminalLogButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 10/08/2026
     */

    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text subjectText;
    [SerializeField] private TMP_Text logText;
    [SerializeField] private TMP_Text dateText;

    [Header("Background Colors")]
    [SerializeField] private Color normalBackgroundColor = Color.black;
    [SerializeField] private Color hoveredBackgroundColor = Color.gray;
    [SerializeField] private Color selectedBackgroundColor = Color.green;

    [Header("Text Colors")]
    [SerializeField] private Color normalTextColor = Color.green;
    [SerializeField] private Color hoveredTextColor = Color.white;
    [SerializeField] private Color selectedTextColor = Color.black;

    private LogData logData;
    private Action<LogData> onSelected;

    private bool isSelected;
    private bool isHovered;

    public LogData LogData => logData;

    public void Initialize(LogData data, Action<LogData> callback)
    {
        logData = data;
        onSelected = callback;

        subjectText.text = data.subject;
        logText.text = data.log;
        dateText.text = data.date;

        button.onClick.RemoveListener(OnClicked);
        button.onClick.AddListener(OnClicked);

        isHovered = false;
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateColors();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateColors();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateColors();
    }

    private void UpdateColors()
    {
        Color backgroundColor;
        Color textColor;

        if (isSelected)
        {
            backgroundColor = selectedBackgroundColor;
            textColor = selectedTextColor;
        }
        else if (isHovered)
        {
            backgroundColor = hoveredBackgroundColor;
            textColor = hoveredTextColor;
        }
        else
        {
            backgroundColor = normalBackgroundColor;
            textColor = normalTextColor;
        }

        backgroundImage.color = backgroundColor;

        subjectText.color = textColor;
        logText.color = textColor;
        dateText.color = textColor;
    }

    private void OnClicked()
    {
        onSelected?.Invoke(logData);
    }

    private void OnDisable()
    {
        isHovered = false;
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClicked);
    }
}
