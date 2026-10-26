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
    private sealed class DialogueRequest
    {
        public List<List<string>> Lines;
        public System.Action OnCompleted;
        public float AutoAdvanceDelay;
        public bool MovementEnabled;
    }

    private readonly Queue<DialogueRequest> dialogueQueue = new Queue<DialogueRequest>();
    private bool isPlayingDialogue;
    private const float ScrollDelay = 0.08f;
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
        StartQueuedDialogue();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        dialogueQueue.Clear();
        if (isPlayingDialogue)
        {
            isPlayingDialogue = false;
            textBox.text = "";
            SetPlayerMovement(true);
        }

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

            // Check input every frame, including between character reveals.
            float elapsed = 0f;
            while (elapsed < ScrollDelay)
            {
                if (Time.timeScale > 0f && advanceDialogueAction.WasPressedThisFrame())
                {
                    textBox.text += line.Substring(character);
                    // Consume this frame so revealing the text cannot also advance it.
                    yield return null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    IEnumerator ReadLine(List<string> dialogueLine, float autoAdvanceDelay)
    {
        string speaker = dialogueLine[0];
        string line = dialogueLine[1];
        
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
    }

    private void StartQueuedDialogue()
    {
        if (isPlayingDialogue || dialogueQueue.Count == 0 || !isActiveAndEnabled)
            return;
        
        isPlayingDialogue = true;
        StartCoroutine(ProcessDialogueQueue());
    }

    private IEnumerator ProcessDialogueQueue()
    {
        while (dialogueQueue.Count > 0)
        {
            DialogueRequest request = dialogueQueue.Dequeue();
            SetPlayerMovement(request.MovementEnabled);
            foreach (List<string> line in request.Lines)
                yield return ReadLine(line, request.AutoAdvanceDelay);

            SetPlayerMovement(true);
            try
            {
                request.OnCompleted?.Invoke();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }

            if (!isActiveAndEnabled)
                yield break;
        }

        isPlayingDialogue = false;
    }

    // Select this method in a Unity Event and enter the player's dialogue text.
    public void StartPlayerDialogue(string text)
    {
        StartDialogue(new List<List<string>>
        {
            new List<string> { playerName + ": ", text }
        }, null, 2f);
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
        StartDialogue(script, onCompleted, autoAdvanceDelay, true);
    }

    public void StartDialogue(List<List<string>> script, System.Action onCompleted, float autoAdvanceDelay, bool movementEnabled)
    {
        if (script == null || script.Count == 0)
        {
            Debug.LogWarning("Cannot queue an empty dialogue script.", this);
            return;
        }
        
        List<List<string>> lines = new List<List<string>>(script.Count);
        foreach (List<string> line in script)
        {
            if (line == null || line.Count < 2 || line[0] == null || line[1] == null)
            {
                Debug.LogWarning("Each dialogue line must contain a speaker and text.", this);
                return;
            }

            lines.Add(new List<string> { line[0], line[1] });
        }

        dialogueQueue.Enqueue(new DialogueRequest
        {
            Lines = lines,
            OnCompleted = onCompleted,
            AutoAdvanceDelay = Mathf.Max(0f, autoAdvanceDelay),
            MovementEnabled = movementEnabled
        });
        StartQueuedDialogue();
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
