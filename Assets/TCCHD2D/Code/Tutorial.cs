using System;
using CI.QuickSave;
using UnityEngine;
using UnityEngine.Playables;

public class Tutorial : MonoBehaviour
{
    public bool playedTutorial;
    public PlayableDirector playableDirector;
    public PlayerMovement playerMovement;
    public GameObject tutorialGameObject;
    
    private void Awake()
    {
        if (playableDirector == null) playableDirector = GetComponent<PlayableDirector>();
        var reader = QuickSaveReader.Create("GameSave");
        if (reader.Exists("PlayedTutorial"))
        {
            playedTutorial = reader.Read<bool>("PlayedTutorial");
        }
        else
        {
            playedTutorial = false;
        }

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

    public void SkipTutorial()
    {
        playerMovement.enabled = playedTutorial;
        playableDirector.Stop();
        tutorialGameObject.SetActive(false);
    }
}
