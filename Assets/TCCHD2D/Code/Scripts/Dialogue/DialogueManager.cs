// Created by Sérgio Murillo da Costa Faria
// Date: 17/03/2023

using System.Collections;
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
    
    [SerializeField, Range(0.01f, 1f)]
    private float charDelay = 0.1f;
    
    [SerializeField] 
    private AudioSource audioSource;
    
    [TitleGroup("Dialogue Data", Alignment = TitleAlignments.Centered)]
    [SerializeField, ReadOnly] 
    private DialogueData currentDialogueData;

    private int _currentLine;

    public DialogueData CurrentDialogueData { get => currentDialogueData; set => currentDialogueData = value; }
    
    public void DisplayDialogue()
    {
        StopAllCoroutines();
        StartCoroutine(DisplayDialogueCoroutine());
    }

    private IEnumerator DisplayDialogueCoroutine()
    {
        var currentLine = CurrentDialogueData.DialogueLines[_currentLine];
        speakerName.text = CurrentDialogueData.CharacterName;
        dialogueText.text = "";
        foreach (var character in currentLine.Text)
        {
            dialogueText.text += character;
            yield return new WaitForSeconds(charDelay);
        }

        if (!currentLine.PlayAudio) yield break;
        var audioClip = currentLine.AudioClip;
        if (audioClip != null) audioSource.PlayOneShot(audioClip);
        else Debug.LogWarning($"Audio clip {currentLine.AudioClip} not found.");
    }

    public void AdvanceDialogue()
    {
        if (dialogueText.text != CurrentDialogueData.DialogueLines[_currentLine].Text)
        {
            StopAllCoroutines();
            dialogueText.text = CurrentDialogueData.DialogueLines[_currentLine].Text;
        }
        else
        {
            _currentLine++;
            if (_currentLine >= CurrentDialogueData.DialogueLines.Length)
            {
                EndDialogue();
                return;
            }
            DisplayDialogue();
        }
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
