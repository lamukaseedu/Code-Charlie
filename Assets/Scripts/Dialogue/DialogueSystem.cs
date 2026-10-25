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
    private float autoAdvanceDelay;
    private WaitForSeconds scrollTime = new WaitForSeconds(0.08f);
    private WaitForSeconds promptWaitTime = new WaitForSeconds(1f);
    private InputAction advanceDialogueAction;
    private bool enabledAdvanceDialogueAction;

    private void Awake()
    {
        advanceDialogueAction = InputSystem.actions.FindAction("UI/AdvanceDialogue", true);
    }

    private void OnEnable()
    {
        enabledAdvanceDialogueAction = !advanceDialogueAction.enabled;
        advanceDialogueAction.Enable();
    }

    private void OnDisable()
    {
        if (enabledAdvanceDialogueAction)
            advanceDialogueAction.Disable();
    }

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

        if (autoAdvanceDelay > 0f)
        {
            float elapsed = 0f;
            while (elapsed < autoAdvanceDelay)
            {
                if (Time.timeScale > 0f && advanceDialogueAction.WasPressedThisFrame())
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            yield return promptWaitTime;
            textBox.text += $"   (Press {advanceDialogueAction.GetBindingDisplayString()} to Continue)";
            while (!advanceDialogueAction.WasPressedThisFrame())
            {
                yield return null;
            }
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
        StartDialogue(script, onCompleted, 0f);
    }

    public void StartDialogue(List<List<string>> script, System.Action onCompleted, float autoAdvanceDelay)
    {
        // Replacing a dialogue cancels its playback and completion callback.
        StopAllCoroutines();
        dialogue = script;
        dialogueCompleted = onCompleted;
        this.autoAdvanceDelay = Mathf.Max(0f, autoAdvanceDelay);
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
