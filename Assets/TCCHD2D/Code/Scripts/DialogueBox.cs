// Created by Sérgio Murillo da Costa Faria
// Date: 17/03/2023

using TMPro;
using UnityEngine;

public class DialogueBox : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMeshPro;
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private DialogueSystem dialogueSystem;

    private int currentLine = 0;
    
    public DialogueData DialogueData { get => dialogueData; set => dialogueData = value; }

    private void Start()
    {
        //DisplayDialogue();
    }

    private void DisplayDialogue()
    {
        textMeshPro.text = $"{dialogueData.CharacterName}: {dialogueData.DialogueLines[currentLine]}";
    }

    public void AdvanceDialogue()
    {
        currentLine++;
        if (currentLine >= dialogueData.DialogueLines.Length)
        {
            // End of dialogue
            EndDialogue();
            return;
        }
        DisplayDialogue();
    }

    public void EndDialogue()
    {
        dialogueSystem.EndDialogue();
        Destroy(gameObject);
    }
}
