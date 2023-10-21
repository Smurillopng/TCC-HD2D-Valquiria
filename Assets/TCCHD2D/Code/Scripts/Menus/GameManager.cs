using System.Collections;
using System.Collections.Generic;
using MuriPNG.Audio;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private SoundManager _soundManager;
    void Awake()
    {
        if (_soundManager == null) _soundManager = FindObjectOfType<SoundManager>();
        SetMasterVolume();
        SetMusicVolume();
        SetSfxVolume();
        SetQuality();
        SetFps();
        SetFullscreen();
    }

    public void SetMasterVolume()
    {
        _soundManager.SetMixerVolume("MasterMixer", Mathf.Log10(PlayerPrefs.GetFloat("MusicVolume")) * 20f);
    }

    /// <summary>
    /// Sets the music volume of the game and saves it to PlayerPrefs.
    /// </summary>
    public void SetMusicVolume()
    {
        _soundManager.SetMixerVolume("MusicMixer", Mathf.Log10(PlayerPrefs.GetFloat("MusicVolume")) * 20f);
    }

    /// <summary>
    /// Sets the sfx volume of the game and saves it to PlayerPrefs.
    /// </summary>
    public void SetSfxVolume()
    {
        _soundManager.SetMixerVolume("SfxMixer", Mathf.Log10(PlayerPrefs.GetFloat("SfxVolume")) * 20f);
    }

    /// <summary>
    /// Sets the quality of the game and saves it to PlayerPrefs.
    /// </summary>
    public void SetQuality()
    {
        QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("Quality"));
    }

    public void SetFps()
    {
        var fps = PlayerPrefs.GetInt("Fps");
        QualitySettings.vSyncCount = 0;
        switch (fps)
        {
            case 0:
                Application.targetFrameRate = 60;
                break;
            case 1:
                Application.targetFrameRate = 30;
                break;
            case 2:
                Application.targetFrameRate = -1;
                break;
        }
    }

    /// <summary>
    /// Sets the game to fullscreen or windowed and saves it to PlayerPrefs.
    /// </summary>
    public void SetFullscreen()
    {
        var screenType = PlayerPrefs.GetInt("Fullscreen");
        switch (screenType)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
            case 2:
                Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
                break;
        }
    }
}