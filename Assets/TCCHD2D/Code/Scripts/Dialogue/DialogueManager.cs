// Created by Sérgio Murillo da Costa Faria

using System.Collections;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[HideMonoScript]
public class DialogueManager : MonoBehaviour
{
    #region === Variables ===============================================================

    [FoldoutGroup("Dialogue Manager")]
    [BoxGroup("Dialogue Manager/Settings")]
    [SerializeField, Required, Tooltip("Dialogue box that will be displayed on screen.")]
    private GameObject dialogueBox;

    [BoxGroup("Dialogue Manager/Settings")]
    [SerializeField, Required, Tooltip("Dialogue image that will be displayed on screen.")]
    private GameObject dialogueImage;

    [BoxGroup("Dialogue Manager/Settings")]
    [SerializeField, Required, Tooltip("Text that will display the name of the character speaking.")]
    private TextMeshProUGUI speakerName;

    [BoxGroup("Dialogue Manager/Settings")]
    [SerializeField, Required, Tooltip("Text that will display the dialogue of the character speaking.")]
    private TextMeshProUGUI dialogueText;

    [BoxGroup("Dialogue Manager/Settings")]
    [SerializeField, Range(0.01f, 1f), Tooltip("Delay between each character of the dialogue text.")]
    private float charDelay = 0.1f;

    [BoxGroup("Dialogue Manager/Dialogue Data")]
    [SerializeField, ReadOnly, Tooltip("Current dialogue data being displayed.")]
    private DialogueData currentDialogueData;

    private int _currentLine;
    public DialogueData CurrentDialogueData { get => currentDialogueData; set => currentDialogueData = value; }

    #endregion ==========================================================================

    #region === Methods =================================================================

    /// <summary>Displays a dialogue.</summary>
    /// <remarks>
    /// This method stops all coroutines and starts a new coroutine to display the dialogue.
    /// </remarks>
    private void DisplayDialogue()
    {
        StopAllCoroutines();
        StartCoroutine(DisplayDialogueCoroutine());
    }
    /// <summary>Displays a dialogue line character by character.</summary>
    /// <returns>An IEnumerator that can be used to iterate through the characters of the dialogue line.</returns>
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
    }
    /// <summary>Advances the dialogue to the next line.</summary>
    /// <remarks>
    /// If the current line's text is not yet fully displayed, this method stops all coroutines and immediately displays the full text.
    /// Otherwise, this method increments the current line index and displays the next line, or ends the dialogue if there are no more lines.
    /// </remarks>
    private void AdvanceDialogue()
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
    private void AdvanceTip()
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
                EndTip();
                return;
            }
            DisplayDialogue();
        }
    }
    /// <summary>Starts a new dialogue.</summary>
    /// <param name="dialogueData">The data for the dialogue to start.</param>
    /// <remarks>If the dialogue box is not active, it will be activated. If the dialogue data is different from the current dialogue data, the current line will be set to 0 and the dialogue will be displayed. Otherwise, the dialogue will be advanced.</remarks>
    public void StartDialogue(DialogueData dialogueData)
    {
        if (!dialogueBox.activeSelf)
            dialogueBox.SetActive(true);
        if (currentDialogueData != dialogueData)
        {
            CurrentDialogueData = dialogueData;
            _currentLine = 0;
            DisplayDialogue();
        }
        else AdvanceDialogue();
    }
    public void StartTip(DialogueData dialogueData)
    {
        if (!dialogueBox.activeSelf)
            dialogueBox.SetActive(true);
        if (currentDialogueData != dialogueData)
        {
            CurrentDialogueData = dialogueData;
            _currentLine = 0;
            DisplayDialogue();
        }
        else AdvanceTip();
    }
    /// <summary>Ends the current dialogue by clearing the speaker name and dialogue text, setting the current dialogue data to null, and hiding the dialogue box.</summary>
    public void EndDialogue()
    {
        speakerName.text = string.Empty;
        dialogueText.text = string.Empty;
        currentDialogueData = null;
        dialogueBox.SetActive(false);
    }
    public void EndTip()
    {
        speakerName.text = string.Empty;
        dialogueText.text = string.Empty;
        currentDialogueData = null;
        dialogueBox.SetActive(false);
        HideImage();
    }

    public void ShowImage(Sprite sprite)
    {
        dialogueImage.SetActive(true);
        dialogueImage.GetComponent<Image>().sprite = sprite;
    }

    public void HideImage()
    {
        dialogueImage.SetActive(false);
        dialogueImage.GetComponent<Image>().sprite = null;
    }

    public void TutorialDialoguePlayer()
    {
        if (currentDialogueData != null)
        {
            AdvanceDialogue();
        }
    }

    #endregion ==========================================================================
}
