using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.InputSystem; 

public class DialogueSystem : MonoBehaviour
{
    [SerializeField] private string playerName = "Charli";
    [SerializeField] private TMP_Text textBox;
    private List<List<string>> dialogue;
    private WaitForSeconds scrollTime = new WaitForSeconds(0.08f);
    private WaitForSeconds readTime = new WaitForSeconds(1.5f);

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

        if (lineNumber < dialogue.Count - 1)
        {
            yield return null;

            while (!Keyboard.current.eKey.wasPressedThisFrame)
            {
                yield return null;
            }

            StartCoroutine(ReadLine(lineNumber + 1));
        }
    }
    public void StartDialogue(List<List<string>> script)
    {
        dialogue = script;
        StartCoroutine(ReadLine(0));
    }
    public string GetPlayerName()
    {
        return playerName;
    }
}
