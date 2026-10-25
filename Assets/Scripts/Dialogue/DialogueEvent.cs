/*
 * Author: Brian Delaney
 * Created: 9/3/2026
 */
using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.Playables;

public class DialogueEvent : MonoBehaviour
{
    public enum TriggerMode { Collider, UnityEvent }

    [Tooltip("Choose whether dialogue starts when the tagged object enters the trigger collider or when a Unity Event calls TriggerDialogue.")]
    [SerializeField] private TriggerMode triggerMode = TriggerMode.Collider;
    [SerializeField] List<string> speakerNames = new();
    [SerializeField] TextAsset dialogueFile;
    [SerializeField] DialogueSystem dialogueSystem;
    [SerializeField] private string triggerTag = "Player";
    [SerializeField] private bool eventTriggered = false;
    [SerializeField] private bool movementEnabled = true;
    [Tooltip("Invoked after the player advances past the final line of this dialogue and player movement is restored.")]
    [SerializeField] private UnityEvent onDialogueEnded = new UnityEvent();
    private string playerName;
    private readonly List<List<string>> script = new();

    void OnTriggerEnter(Collider other)
    {
        if (triggerMode == TriggerMode.Collider && !eventTriggered && other.CompareTag(triggerTag))
            BeginDialogue();
    }

    // Select this method in a Unity Event when Trigger Mode is UnityEvent.
    public void TriggerDialogue()
    {
        if (triggerMode == TriggerMode.UnityEvent)
            BeginDialogue();
    }

    private void BeginDialogue()
    {
        if (eventTriggered || !isActiveAndEnabled) return;
        if (dialogueSystem == null || dialogueFile == null)
        {
            Debug.LogWarning("Assign a Dialogue System and Dialogue File before triggering dialogue.", this);
            return;
        }

        // Resolve here so scene-loaded Unity Events can trigger dialogue before Start.
        playerName = dialogueSystem.GetPlayerName();
        dialogueSystem.SetPlayerMovement(movementEnabled);
        eventTriggered = true;
        ProcessText();
    }

    void ProcessText()
    {
        string[] lines = dialogueFile.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string line in lines)
        {
            string speaker;
            char speakerIndicator = line[0];
            if (speakerIndicator == 'P')
            {
                speaker = playerName;
            }
            else if (char.IsDigit(speakerIndicator) && (int)char.GetNumericValue(speakerIndicator) < speakerNames.Count)
            {
                speaker = speakerNames[(int)char.GetNumericValue(speakerIndicator)];
            }
            else speaker = "ERROR";
            List<string> attributedLine = new() { speaker + ": ", line.Substring(2)};
            script.Add(attributedLine);
        }

        dialogueSystem.StartDialogue(script, HandleDialogueEnded);
    }

    private void HandleDialogueEnded()
    {
        if (this != null)
            onDialogueEnded?.Invoke();
    }
}
