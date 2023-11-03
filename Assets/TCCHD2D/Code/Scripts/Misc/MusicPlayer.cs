// Created by Sérgio Murillo da Costa Faria

using MuriPNG.Audio;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

[HideMonoScript]
public class MusicPlayer : MonoBehaviour
{
    private void Start()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        var sceneType = PlayerControls.Instance.SceneMap[SceneManager.GetActiveScene().name];
        SoundManager.Instance.StopAllSounds();
        switch (sceneType)
        {
            case SceneType.Game:
                SoundManager.Instance.PlaySound("gameMusic");
                break;
            case SceneType.Menu:
                SoundManager.Instance.PlaySound("menuMusic");
                break;
            case SceneType.Combat:
                SoundManager.Instance.PlaySound("combatMusic");
                break;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}