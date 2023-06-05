using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class Tutorial : MonoBehaviour
{
    public DialogueManager dialogueManager;
    public PlayableDirector director;
    public TMP_Text dialogueText;
    public GameControls gameControls;
    public TMP_Text tutorialText;

    private void Awake()
    {
        gameControls = new GameControls();
    }

    public void PlayDialogue(DialogueData dialogueData)
    {
        dialogueManager.StartDialogue(dialogueData);
        StartCoroutine(WaitText(dialogueData));
    }

    private IEnumerator WaitText(DialogueData dialogueData)
    {
        foreach (var text in dialogueData.DialogueLines)
        {
            while (dialogueText.text != text.Text)
            {
                director.Pause();
                yield return null;
            }
            while (dialogueText.text == text.Text)
            {
                yield return null;
            }
        }
        director.Resume();
    }

    public void PlayerDialogueInput(DialogueData dialogueData)
    {
        if (dialogueData.IsTutorial && !dialogueData.HasPlayed)
            StartCoroutine(WaitInput(dialogueData));
    }

    private IEnumerator WaitInput(DialogueData dialogueData)
    {
        var currentLine = dialogueData.DialogueLines[0];
        var currentLineIndex = 0;
        while (true)
        {
            gameControls.Tutorial.Enable();
            if (gameControls.Tutorial.AdvanceDialogue.triggered)
            {
                print(currentLineIndex);
                if (tutorialText.text == currentLine.Text)
                {
                    currentLineIndex++;
                    if (currentLineIndex != dialogueData.DialogueLines.Length)
                        currentLine = dialogueData.DialogueLines[currentLineIndex];
                }
                dialogueManager.StartDialogue(dialogueData);
            }
            if (currentLineIndex == dialogueData.DialogueLines.Length)
            {
                break;
            }
            yield return null;
        }
        gameControls.Tutorial.Disable();
        dialogueData.HasPlayed = true;
    }
}
