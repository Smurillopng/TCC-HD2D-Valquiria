// Created by Sérgio Murillo da Costa Faria

using MuriPNG.Audio;
using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class GameManager : MonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Game Manager")]
    [SerializeField, Tooltip("The sound manager.")]

    private SoundManager _soundManager;
    #endregion ==========================================================================

    #region === Unity Methods ===========================================================
    [BoxGroup("Game Manager/Debug", true)]
    [Button("Set Initial Values")]
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
    #endregion ==========================================================================

    #region === Methods =================================================================
    [BoxGroup("Game Manager/Debug", true)]
    [Button("Set Master Volume")]
    public void SetMasterVolume()
    {
        _soundManager.SetMixerVolume("MasterMixer", Mathf.Log10(PlayerPrefs.GetFloat("MusicVolume")) * 20f);
    }

    [BoxGroup("Game Manager/Debug", true)]
    [Button("Set Music Volume")]
    public void SetMusicVolume()
    {
        _soundManager.SetMixerVolume("MusicMixer", Mathf.Log10(PlayerPrefs.GetFloat("MusicVolume")) * 20f);
    }

    [BoxGroup("Game Manager/Debug", true)]
    [Button("Set Sfx Volume")]
    public void SetSfxVolume()
    {
        _soundManager.SetMixerVolume("SfxMixer", Mathf.Log10(PlayerPrefs.GetFloat("SfxVolume")) * 20f);
    }

    [BoxGroup("Game Manager/Debug", true)]
    [Button("Set Quality")]
    public void SetQuality()
    {
        QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("Quality"));
    }

    [BoxGroup("Game Manager/Debug", true)]
    [Button("Set Fps")]
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

    [BoxGroup("Game Manager/Debug", true)]
    [Button("Set Fullscreen")]
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
    #endregion ==========================================================================
}