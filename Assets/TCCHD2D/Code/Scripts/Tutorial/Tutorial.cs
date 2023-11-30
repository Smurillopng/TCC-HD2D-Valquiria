// Created by Sérgio Murillo da Costa Faria

using System.Collections;
using CI.QuickSave;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

[HideMonoScript]
public class Tutorial : MonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Tutorial")]
    [BoxGroup("Tutorial/Settings")]
    public DialogueManager dialogueManager;
    
    [BoxGroup("Tutorial/Settings")]
    public PlayableDirector director;
    
    [BoxGroup("Tutorial/Settings")]
    public PlayableDirector director2;
    
    [BoxGroup("Tutorial/Settings")]
    public TMP_Text dialogueText;
    
    [BoxGroup("Tutorial/Settings")]
    public GameControls gameControls;
    
    [BoxGroup("Tutorial/Settings")]
    public PlayerMovement playerMovement;
    
    [BoxGroup("Tutorial/Settings")]
    public GameObject[] tutorialObjects;
    
    [BoxGroup("Tutorial/Settings")]
    public Transform startPosition;
    
    [BoxGroup("Tutorial/Settings")]
    public GameObject bjorn, player;
    
    [BoxGroup("Tutorial/Settings")]
    public Unit _player;

    public static bool finishedTutorial;
    #endregion ==========================================================================

    #region === Unity Methods ===========================================================
    private void Start()
    {
        var reader = QuickSaveReader.Create("GameSave");
        finishedTutorial = reader.Exists("FinishedTutorial") && reader.Read<bool>("FinishedTutorial");
        if (finishedTutorial.Equals(true))
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
    #endregion ==========================================================================

    #region === Methods =================================================================
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
        finishedTutorial = true;
        var writer = QuickSaveWriter.Create("GameSave");
        writer.Write("FinishedTutorial", finishedTutorial);
        writer.Commit();
        foreach (var tutorialObject in tutorialObjects)
            tutorialObject.SetActive(false);
        _player.Experience = 1;
        _player.CurrentHp = _player.MaxHp;
        _player.CurrentTp = 0;
    }
    #endregion ==========================================================================
}
