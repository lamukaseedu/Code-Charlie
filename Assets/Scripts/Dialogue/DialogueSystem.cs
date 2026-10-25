using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class DialogueSystem : MonoBehaviour
{
    [SerializeField] private string playerName = "Charlie";
    [SerializeField] private TMP_Text textBox;
    [SerializeField] private GameObject player;
    private List<List<string>> dialogue;
    private System.Action dialogueCompleted;
    private WaitForSeconds scrollTime = new WaitForSeconds(0.08f);
    private WaitForSeconds promptWaitTime = new WaitForSeconds(1f);

    IEnumerator ScrollText(string line)
    {
        Debug.Log(line);
        int character = 0;
        while (character < line.Length)
        {
            textBox.text += line[character];
            character++;
            yield return scrollTime;
        }
    }

    IEnumerator ReadLine(int lineNumber)
    {
        string speaker = dialogue[lineNumber][0];
        string line = dialogue[lineNumber][1];
        
        textBox.text = speaker;
        yield return StartCoroutine(ScrollText(line));

        
        yield return promptWaitTime;
        textBox.text += "   (Press E to Continue)";
        while (!Keyboard.current.eKey.wasPressedThisFrame)
        {
            yield return null;
        }

        textBox.text = "";

        if (lineNumber < dialogue.Count - 1)
        {
            StartCoroutine(ReadLine(lineNumber + 1));
        } else
        {
            SetPlayerMovement(true);
            // Clear before invoking so an event can safely start another dialogue.
            System.Action completed = dialogueCompleted;
            dialogueCompleted = null;
            completed?.Invoke();
        }
    }
    public void StartDialogue(List<List<string>> script)
    {
        StartDialogue(script, null);
    }

    public void StartDialogue(List<List<string>> script, System.Action onCompleted)
    {
        // Replacing a dialogue cancels its playback and completion callback.
        StopAllCoroutines();
        dialogue = script;
        dialogueCompleted = onCompleted;
        StartCoroutine(ReadLine(0));
    }
    public string GetPlayerName()
    {
        return playerName;
    }
    public void SetPlayerMovement(bool state)
    {
        player.GetComponent<PlayerMovement>().SetMovementEnabled(state);
    }
}
