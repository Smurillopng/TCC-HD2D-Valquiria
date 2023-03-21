// Created by Sérgio Murillo da Costa Faria
// Date: 17/03/2023

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [TitleGroup("Dialogue Manager Settings", Alignment = TitleAlignments.Centered)]
    [SerializeField, Required]
    private GameObject dialogueBox;
    
    [SerializeField, Required] 
    private TextMeshProUGUI speakerName;
    
    [SerializeField, Required] 
    private TextMeshProUGUI dialogueText;
    
    [SerializeField] 
    private AudioSource audioSource;
    
    [TitleGroup("Dialogue Data", Alignment = TitleAlignments.Centered)]
    [SerializeField, ReadOnly] 
    private DialogueData currentDialogueData;

    private int _currentLine;

    public DialogueData CurrentDialogueData { get => currentDialogueData; set => currentDialogueData = value; }

    private void DisplayDialogue()
    {
        var currentLine = CurrentDialogueData.DialogueLines[_currentLine];
        speakerName.text = CurrentDialogueData.CharacterName;
        dialogueText.text = currentLine.Text;
        if (!currentLine.PlayAudio) return;
        var audioClip = currentLine.AudioClip;
        if (audioClip != null) audioSource.PlayOneShot(audioClip);
        else Debug.LogWarning($"Audio clip {currentLine.AudioClip} not found.");
    }

    public void AdvanceDialogue()
    {
        _currentLine++;
        if (_currentLine >= CurrentDialogueData.DialogueLines.Length)
        {
            EndDialogue();
            return;
        }
        DisplayDialogue();
    }
    
    public void StartDialogue(DialogueData dialogueData)
    {
        if (dialogueBox.activeSelf == false)
            dialogueBox.SetActive(true);
        if (currentDialogueData != dialogueData)
        {
            CurrentDialogueData = dialogueData;
            _currentLine = 0;
            DisplayDialogue();
        }
        else AdvanceDialogue();
    }

    public void EndDialogue()
    {
        speakerName.text = string.Empty;
        dialogueText.text = string.Empty;
        currentDialogueData = null;
        dialogueBox.SetActive(false);
    }
}
