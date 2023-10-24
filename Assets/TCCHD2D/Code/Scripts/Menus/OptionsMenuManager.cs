// Created by Sérgio Murillo da Costa Faria
// Date: 09/03/2023

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Responsible for handling everything related to the options menu.
/// </summary>
public class OptionsMenuManager : MonoBehaviour
{
    [BoxGroup("Audio Settings")]
    [SerializeField]
    private AudioMixerGroup masterMixer;

    [BoxGroup("Audio Settings")]
    [SerializeField]
    private Slider masterVolumeSlider;

    [BoxGroup("Audio Settings")]
    [SerializeField]
    private AudioMixerGroup musicMixer;

    [BoxGroup("Audio Settings")]
    [SerializeField]
    private Slider musicVolumeSlider;

    [BoxGroup("Audio Settings")]
    [SerializeField]
    private AudioMixerGroup sfxMixer;

    [BoxGroup("Audio Settings")]
    [SerializeField]
    private Slider sfxVolumeSlider;

    [BoxGroup("Graphics Settings")]
    [SerializeField]
    private TMP_Dropdown qualityDropdown;

    [BoxGroup("Graphics Settings")]
    [SerializeField]
    private TMP_Dropdown screenDropdown;

    [BoxGroup("Graphics Settings")]
    [SerializeField]
    private TMP_Dropdown fpsDropdown;

    [BoxGroup("Graphics Settings")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [SerializeField] private StringVariable previousScene;

    private Resolution[] _resolutions;
    private List<Resolution> _filteredResolutions;

    private void Start()
    {
        qualityDropdown.ClearOptions();

        // Populate quality dropdown with available quality levels
        var qualityLevels = QualitySettings.names;
        qualityDropdown.AddOptions(new List<string>(qualityLevels));

        // Populate resolution dropdown with available resolutions
        _resolutions = Screen.resolutions;
        _filteredResolutions = new List<Resolution>();
        resolutionDropdown.ClearOptions();
        var currentRefreshRate = Screen.currentResolution.refreshRate;
        var currentResolutionIndex = 0;

        for (var i = 0; i < _resolutions.Length; i++)
        {
            if (_resolutions[i].refreshRate == currentRefreshRate)
            {
                _filteredResolutions.Add(_resolutions[i]);
            }
        }
        var resolutionOptions = new List<string>();
        for (var i = 0; i < _filteredResolutions.Count; i++)
        {
            var option = $"{_filteredResolutions[i].width} x {_filteredResolutions[i].height} @ {_filteredResolutions[i].refreshRate}Hz";
            resolutionOptions.Add(option);
            if (_filteredResolutions[i].width == Screen.width &&
                _filteredResolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(resolutionOptions);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // Set initial values for quality, fullscreen, volume, and resolution
        qualityDropdown.value = PlayerPrefs.GetInt("Quality", 3);
        screenDropdown.value = PlayerPrefs.GetInt("Fullscreen", 0);
        fpsDropdown.value = PlayerPrefs.GetInt("Fps", 0);
        masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SfxVolume", 1f);
        masterMixer.audioMixer.SetFloat("MasterVolume", Mathf.Log10(PlayerPrefs.GetFloat("MasterVolume", 0.75f)) * 20f);
        musicMixer.audioMixer.SetFloat("MusicVolume", Mathf.Log10(PlayerPrefs.GetFloat("MusicVolume", 0.75f)) * 20f);
        sfxMixer.audioMixer.SetFloat("SfxVolume", Mathf.Log10(PlayerPrefs.GetFloat("SfxVolume", 0.75f)) * 20f);
        resolutionDropdown.value = PlayerPrefs.GetInt("Resolution", currentResolutionIndex);
    }

    /// <summary>
    /// Sets the master volume of the game and saves it to PlayerPrefs.
    /// </summary>
    public void SetMasterVolume()
    {
        masterMixer.audioMixer.SetFloat("MasterVolume", Mathf.Log10(masterVolumeSlider.value) * 20f);
        PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
    }

    /// <summary>
    /// Sets the music volume of the game and saves it to PlayerPrefs.
    /// </summary>
    public void SetMusicVolume()
    {
        musicMixer.audioMixer.SetFloat("MusicVolume", Mathf.Log10(musicVolumeSlider.value) * 20f);
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
    }

    /// <summary>
    /// Sets the sfx volume of the game and saves it to PlayerPrefs.
    /// </summary>
    public void SetSfxVolume()
    {
        sfxMixer.audioMixer.SetFloat("SfxVolume", Mathf.Log10(sfxVolumeSlider.value) * 20f);
        PlayerPrefs.SetFloat("SfxVolume", sfxVolumeSlider.value);
    }

    /// <summary>
    /// Sets the quality of the game and saves it to PlayerPrefs.
    /// </summary>
    public void SetQuality()
    {
        QualitySettings.SetQualityLevel(qualityDropdown.value);
        PlayerPrefs.SetInt("Quality", qualityDropdown.value);
    }

    public void SetFps()
    {
        QualitySettings.vSyncCount = 0;
        switch (fpsDropdown.value)
        {
            case 0:
                Application.targetFrameRate = 60;
                PlayerPrefs.SetInt("Fps", 0);
                break;
            case 1:
                Application.targetFrameRate = 30;
                PlayerPrefs.SetInt("Fps", 1);
                break;
            case 2:
                Application.targetFrameRate = -1;
                PlayerPrefs.SetInt("Fps", 2);
                break;
        }
    }

    /// <summary>
    /// Sets the game to fullscreen or windowed and saves it to PlayerPrefs.
    /// </summary>
    public void SetFullscreen()
    {
        switch (screenDropdown.value)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                PlayerPrefs.SetInt("Fullscreen", 0);
                break;
            case 1:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                PlayerPrefs.SetInt("Fullscreen", 1);
                break;
            case 2:
                Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
                PlayerPrefs.SetInt("Fullscreen", 2);
                break;
        }
    }

    /// <summary>
    /// Sets the resolution of the game and saves it to PlayerPrefs.
    /// </summary>
    public void SetResolution()
    {
        var resolution = _filteredResolutions[resolutionDropdown.value];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("Resolution", resolutionDropdown.value);
    }

    /// <summary>
    /// Returns to the last scene.
    /// </summary>
    public void ReturnToLastScene()
    {
        SceneManager.LoadScene(previousScene.Value);
    }
}