using System.Collections;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

/// <summary>
/// This class is responsible for displaying the dialogue on screen.
/// </summary>
/// <remarks>
/// Created by Sérgio Murillo da Costa Faria on 17/03/2023.
/// </remarks>
[HideMonoScript]
public class DialogueManager : MonoBehaviour
{
    #region === Variables ===============================================================

    [TitleGroup("Dialogue Manager Settings", Alignment = TitleAlignments.Centered)]
    [SerializeField, Required, Tooltip("Dialogue box that will be displayed on screen.")]
    private GameObject dialogueBox;

    [SerializeField, Required, Tooltip("Text that will display the name of the character speaking.")]
    private TextMeshProUGUI speakerName;

    [SerializeField, Required, Tooltip("Text that will display the dialogue of the character speaking.")]
    private TextMeshProUGUI dialogueText;

    [SerializeField, Range(0.01f, 1f), Tooltip("Delay between each character of the dialogue text.")]
    private float charDelay = 0.1f;

    [TitleGroup("Dialogue Data", Alignment = TitleAlignments.Centered)]
    [SerializeField, ReadOnly, Tooltip("Current dialogue data being displayed.")]
    private DialogueData currentDialogueData;

    private int _currentLine;

    public DialogueData CurrentDialogueData { get => currentDialogueData; set => currentDialogueData = value; }

    #endregion

    #region === Methods =================================================================

    /// <summary>
    /// Displays the current dialogue line.
    /// </summary>
    private void DisplayDialogue()
    {
        StopAllCoroutines();
        StartCoroutine(DisplayDialogueCoroutine());
    }
    /// <summary>
    /// Coroutine that displays the current dialogue line character by character.
    /// </summary>
    /// <returns></returns>
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
    /// <summary>
    /// Advances the dialogue to the next line or ends the dialogue if there are no more lines.
    /// </summary>
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
    /// <summary>
    /// Starts the dialogue with the given dialogue data.
    /// </summary>
    /// <param name="dialogueData">ScriptableObject that contains the strings of dialogue</param>
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
    /// <summary>
    /// Ends the current dialogue.
    /// </summary>
    public void EndDialogue()
    {
        speakerName.text = string.Empty;
        dialogueText.text = string.Empty;
        currentDialogueData = null;
        dialogueBox.SetActive(false);
    }

    #endregion
}
