// Created by Sérgio Murillo da Costa Faria

using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;

[HideMonoScript]
public class TutorialPrologue : MonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Tutorial Prologue")]
    [BoxGroup("Tutorial Prologue/Settings")]
    public static bool playedTutorial;
    
    [BoxGroup("Tutorial Prologue/Settings")]
    public PlayableDirector playableDirector;
    
    [BoxGroup("Tutorial Prologue/Settings")]
    public PlayerMovement playerMovement;
    
    [BoxGroup("Tutorial Prologue/Settings")]
    public GameObject tutorialGameObject;
    
    [BoxGroup("Tutorial Prologue/Settings")]
    public Tutorial tutorial;
    #endregion ==========================================================================

    #region === Unity Methods ===========================================================
    private void Awake()
    {
        if (playableDirector == null) playableDirector = GetComponent<PlayableDirector>();
        var reader = QuickSaveReader.Create("GameSave");
        playedTutorial = reader.Exists("PlayedTutorial") && reader.Read<bool>("PlayedTutorial");

        playerMovement.enabled = playedTutorial;
        
        if (!playedTutorial)
        {
            playableDirector.Play();
            playedTutorial = true;
            var writer = QuickSaveWriter.Create("GameSave");
            writer.Write("PlayedTutorial", playedTutorial);
            writer.Commit();
        }
        else
        {
            playableDirector.Stop();
            tutorialGameObject.SetActive(false);
        }
    }
    #endregion ==========================================================================

    #region === Methods =================================================================
    public void SkipTutorial()
    {
        playableDirector.Stop();
        tutorialGameObject.SetActive(false);
        tutorial.StartTutorial();
    }
    #endregion ==========================================================================
}
