using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TerminalLogViewer : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 10/08/2026
     */

    [Header("Available Logs")]
    [SerializeField] private List<LogData> logs = new();

    [Header("Log Buttons")]
    [SerializeField] private TerminalLogButton logButtonPrefab;
    [SerializeField] private Transform logListParent;

    [Header("Log Viewer UI")]
    [SerializeField] private TMP_Text subjectText;
    [SerializeField] private TMP_Text logText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text authorText;
    [SerializeField] private TMP_Text messageText;

    [Header("Scrolling")]
    [SerializeField] private ScrollRect messageScrollRect;

    private readonly List<TerminalLogButton> spawnedButtons = new();

    private void Start()
    {
        SetLogs(logs);
    }

    public void SetLogs(IEnumerable<LogData> newLogs)
    {
        // Copy the collection before rebuilding the UI.
        List<LogData> incomingLogs = new();

        if (newLogs != null)
        {
            foreach (LogData log in newLogs)
            {
                if (log != null)
                    incomingLogs.Add(log);
            }
        }

        logs = incomingLogs;

        // Remove previously generated buttons.
        foreach (TerminalLogButton logButton in spawnedButtons)
        {
            if (logButton != null)
            {
                logButton.gameObject.SetActive(false);
                Destroy(logButton.gameObject);
            }
        }

        spawnedButtons.Clear();

        // Generate a button for each log.
        foreach (LogData log in logs)
        {
            TerminalLogButton newButton =
                Instantiate(logButtonPrefab, logListParent);

            newButton.Initialize(log, DisplayLog);
            spawnedButtons.Add(newButton);
        }

        if (logs.Count > 0)
        {
            DisplayLog(logs[0]);
        }
        else
        {
            ClearViewer();
        }
    }

    public void DisplayLog(LogData log)
    {
        if (log == null)
            return;

        subjectText.text = log.subject;
        logText.text = $"LOG: {log.log}";
        dateText.text = $"DATE: {log.date}";
        authorText.text = $"AUTHOR: {log.author}";
        messageText.text = log.message;

        UpdateButtonSelection(log);

        // Reset scrolling.
        if (messageScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            messageScrollRect.StopMovement();
            messageScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void UpdateButtonSelection(LogData selectedLog)
    {
        foreach (TerminalLogButton button in spawnedButtons)
        {
            if (button != null)
            {
                button.SetSelected(button.LogData == selectedLog);
            }
        }
    }

    private void ClearViewer()
    {
        subjectText.text = "";
        logText.text = "LOG: ---";
        dateText.text = "DATE: ---";
        authorText.text = "AUTHOR: ---";
        messageText.text = "";
    }
}