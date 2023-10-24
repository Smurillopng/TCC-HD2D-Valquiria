using System.Collections;
using CI.QuickSave;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class Tutorial : MonoBehaviour
{
    public DialogueManager dialogueManager;
    public PlayableDirector director;
    public PlayableDirector director2;
    public TMP_Text dialogueText;
    public GameControls gameControls;
    public PlayerMovement playerMovement;
    public GameObject[] tutorialObjects;
    public Transform startPosition;
    public GameObject bjorn, player;

    private bool _finishedTutorial;

    private void Start()
    {
        var reader = QuickSaveReader.Create("GameSave");
        _finishedTutorial = reader.Exists("FinishedTutorial") && reader.Read<bool>("FinishedTutorial");
        if (_finishedTutorial.Equals(true))
        {
            foreach (var tutorialObject in tutorialObjects)
                tutorialObject.SetActive(false);
            bjorn.SetActive(false);
            return;
        }
        gameControls = new GameControls();
        var playerUnit = player.GetComponent<RandomEncounterManager>();
        if (playerUnit.player.Experience == 1)
        {
            director2.Play();
        }
        playerMovement.enabled = false;
    }

    public void StartTutorial()
    {
        director.Play();
        playerMovement.enabled = false;
    }

    public void PlayDialogue(DialogueData dialogueData)
    {
        dialogueManager.StartDialogue(dialogueData);
        StartCoroutine(WaitText(dialogueData));
    }

    public void PlayDialogue2(DialogueData dialogueData)
    {
        dialogueManager.StartDialogue(dialogueData);
        StartCoroutine(WaitText2(dialogueData));
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

    private IEnumerator WaitText2(DialogueData dialogueData)
    {
        foreach (var text in dialogueData.DialogueLines)
        {
            while (dialogueText.text != text.Text)
            {
                director2.Pause();
                yield return null;
            }
            while (dialogueText.text == text.Text)
            {
                yield return null;
            }
        }
        director2.Resume();
    }

    public void PlayerDialogueInput(DialogueData dialogueData)
    {
        if (dialogueData.IsTutorial && !dialogueData.HasPlayed)
            StartCoroutine(WaitInput(dialogueData));
    }

    public void PlayerDialogueInput2(DialogueData dialogueData)
    {
        if (dialogueData.IsTutorial && !dialogueData.HasPlayed)
            StartCoroutine(WaitInput2(dialogueData));
    }

    private IEnumerator WaitInput(DialogueData dialogueData)
    {
        dialogueManager.StartDialogue(dialogueData);
        var currentLine = dialogueData.DialogueLines[0];
        var currentLineIndex = 0;
        director.Pause();
        while (true)
        {
            gameControls.Tutorial.Enable();
            if (gameControls.Tutorial.AdvanceDialogue.triggered)
            {
                if (dialogueText.text == currentLine.Text)
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
        director.Resume();
    }

    private IEnumerator WaitInput2(DialogueData dialogueData)
    {
        dialogueManager.StartDialogue(dialogueData);
        var currentLine = dialogueData.DialogueLines[0];
        var currentLineIndex = 0;
        director2.Pause();
        while (true)
        {
            gameControls.Tutorial.Enable();
            if (gameControls.Tutorial.AdvanceDialogue.triggered)
            {
                if (dialogueText.text == currentLine.Text)
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
        director2.Resume();
    }

    public void SkipTutorial()
    {
        gameControls.Tutorial.Disable();
        player.transform.position = startPosition.position;
        Destroy(bjorn);
        director.Stop();
        director2.Stop();
        playerMovement.enabled = true;
        _finishedTutorial = true;
        var writer = QuickSaveWriter.Create("GameSave");
        writer.Write("FinishedTutorial", _finishedTutorial);
        writer.Commit();
        foreach (var tutorialObject in tutorialObjects)
            tutorialObject.SetActive(false);
    }
}
