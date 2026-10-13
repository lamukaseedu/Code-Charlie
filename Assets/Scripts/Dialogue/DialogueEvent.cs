/*
 * Author: Brian Delaney
 * Created: 9/3/2026
 */
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Playables;

public class DialogueEvent : MonoBehaviour
{
    [SerializeField] List<string> speakerNames = new();
    [SerializeField] TextAsset dialogueFile;
    [SerializeField] DialogueSystem dialogueSystem;
    [SerializeField] private string triggerTag = "Player";
    [SerializeField] private bool eventTriggered = false;
    private string playerName;
    private readonly List<List<string>> script = new();

    void Start()
    {
        playerName = dialogueSystem.GetPlayerName();
    }

    void OnTriggerEnter(Collider other)
    {

        if (!eventTriggered && other.CompareTag(triggerTag))
        {
            eventTriggered = true;
            ProcessText();
        }
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

        dialogueSystem.StartDialogue(script);
    }
}
