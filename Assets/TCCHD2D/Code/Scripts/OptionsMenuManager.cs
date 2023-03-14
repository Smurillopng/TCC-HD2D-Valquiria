// Created by Sérgio Murillo da Costa Faria
// Date: 09/03/2023

using System;
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
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    
    [SerializeField] private StringVariable previousScene;
    
    private Resolution[] _resolutions;

    private void Start()
    {
        qualityDropdown.ClearOptions();
        resolutionDropdown.ClearOptions();

        // Populate quality dropdown with available quality levels
        var qualityLevels = QualitySettings.names;
        qualityDropdown.AddOptions(new List<string>(qualityLevels));

        // Populate resolution dropdown with available resolutions
        _resolutions = Screen.resolutions;
        var resolutionOptions = new List<string>();
        var currentResolutionIndex = 0;
        
        // Supported aspect ratios and refresh rates
        float[] aspectRatios = { 16f / 9f, 16f / 10f, 4f / 3f };
        int[] refreshRates = { 60, 144, 240};
        
        foreach (var resolution in _resolutions)
        {
            if (Array.Exists(refreshRates, rate => rate == resolution.refreshRate) &&
                Array.Exists(aspectRatios, ratio => Mathf.Approximately(ratio, (float)resolution.width / resolution.height)))
            {
                var option = $"{resolution.width}x{resolution.height} ({resolution.refreshRate}Hz)";
                resolutionOptions.Add(option);

                if (resolution.width == Screen.currentResolution.width &&
                    resolution.height == Screen.currentResolution.height)
                    currentResolutionIndex = resolutionDropdown.options.Count;
            }
        }
        
        resolutionDropdown.AddOptions(resolutionOptions);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // Set initial values for quality, fullscreen, volume, and resolution
        qualityDropdown.value = PlayerPrefs.GetInt("Quality", 3);
        screenDropdown.value = PlayerPrefs.GetInt("Fullscreen", 0);
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
        var resolution = _resolutions[resolutionDropdown.value];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen, resolution.refreshRate);
        PlayerPrefs.SetInt("ResolutionWidth", resolution.width);
        PlayerPrefs.SetInt("ResolutionHeight", resolution.height);
        PlayerPrefs.SetInt("ResolutionRefreshRate", resolution.refreshRate);
    }
    
    /// <summary>
    /// Returns to the last scene.
    /// </summary>
    public void ReturnToLastScene()
    {
        SceneManager.LoadScene(previousScene.Value);
    }
}