// Created by Sérgio Murillo da Costa Faria

using System;
using System.Collections;
using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[HideMonoScript]
public class MenuManager : MonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Menu Manager")]
    [BoxGroup("Menu Manager/Scene Settings", true)]
    [SerializeField, Required, InlineEditor, Tooltip("The previous scene.")]
    private StringVariable previousScene;

    [BoxGroup("Menu Manager/Scene Settings", true)]
    [SerializeField, Tooltip("The confirmation panel.")]
    private GameObject confirmPanel;

    [BoxGroup("Menu Manager/Transition Settings", true)]
    [SerializeField, InlineEditor, PreviewField, Tooltip("The transition material.")]
    private Material mat;

    [BoxGroup("Menu Manager/Transition Settings", true)]
    [SerializeField, Tooltip("The transition slider.")]
    private Slider slider;

    [BoxGroup("Menu Manager/Transition Values", true)]
    [SerializeField, Wrap(-2000f, 2000f), Tooltip("The minimum and maximum transition values.")]
    private float min = -1800, max = 1800;

    [BoxGroup("Menu Manager/Transition Values", true)]
    [SerializeField, Wrap(1f, 5000f), Tooltip("The transition speed.")]
    private float speedTransition = 2500;

    [BoxGroup("Menu Manager/Transition Values", true)]
    [SerializeField, Min(1), Tooltip("The acceleration value.")]
    private float acelerationValue;

    [BoxGroup("Menu Manager/Transition Values", true)]
    [SerializeField, Min(1), Tooltip("The tween time.")]
    private float tweenTime = 1f;

    [BoxGroup("Menu Manager/Debug", true)]
    [SerializeField, ReadOnly, Tooltip("The current value.")]
    private float current;

    [BoxGroup("Menu Manager/Debug", true)]
    [SerializeField, ReadOnly, Tooltip("The acceleration.")]
    private float _aceleration = 1f;

    [BoxGroup("Menu Manager/Debug", true)]
    [SerializeField, ReadOnly, Tooltip("Current cut-off height")]
    private static readonly int CutoffHeight = Shader.PropertyToID("_Cutoff_Height");
    #endregion ==========================================================================

    #region === Unity Methods ===========================================================
    private void Awake()
    {
        StartCoroutine(TransitionOut());
    }
    #endregion ==========================================================================

    #region === Methods =================================================================
    private IEnumerator TransitionOut()
    {
        current = max;
        mat.SetFloat(CutoffHeight, current);
        slider.gameObject.SetActive(false);

        while (current > min)
        {
            if (Math.Abs(current - max) > 0.01 && Math.Abs(current - min) > 0.01)
                _aceleration = acelerationValue;

            current -= speedTransition * Time.deltaTime * _aceleration;
            var progress = Mathf.Clamp01((current - min) / (max - min));
            var targetHeight = Mathf.Lerp(min, max, progress);
            mat.SetFloat(CutoffHeight, targetHeight);

            yield return null;

            if (current <= min)
            {
                current = min;
                _aceleration = 1;
            }
        }
    }

    /// <summary>
    /// Loads the scene with the given name and saves the previous scene name if the scene to be loaded is the options menu.
    /// </summary>
    /// <param name="sceneName"></param>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(TransitionTo(sceneName));
    }

    private IEnumerator TransitionTo(string sceneName)
    {
        if (sceneName == "scn_optionsMenu")
            previousScene.Value = SceneManager.GetActiveScene().name;
        current = min;
        var asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        asyncOperation.allowSceneActivation = false;
        while (current < max)
        {
            if (Math.Abs(current - max) > 0.01 && Math.Abs(current - min) > 0.01)
                _aceleration = acelerationValue;

            current += speedTransition * _aceleration * Time.deltaTime;
            var progress = Mathf.Clamp01((current - min) / (max - min));
            var targetHeight = Mathf.Lerp(min, max, progress);
            mat.SetFloat(CutoffHeight, targetHeight);

            yield return null;

            if (current >= max / 3)
            {
                slider.gameObject.SetActive(true);
            }

            if (current >= max)
            {
                current = max;
                _aceleration = 1;
            }
        }

        while (!asyncOperation.isDone)
        {
            slider.value = asyncOperation.progress;

            if (asyncOperation.progress == 0.9f)
            {
                slider.value = 1f;
                yield return new WaitUntil(() => Math.Abs(current - max) < 0.01);
                asyncOperation.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    public void ConfirmTutorial()
    {
        confirmPanel.SetActive(!confirmPanel.activeSelf);
    }

    public void ConfirmTutorialYes()
    {
        confirmPanel.SetActive(false);
        NewGame();
        var writer = QuickSaveWriter.Create("GameSave");
        writer.Write("FinishedTutorial", true);
        writer.Write("PlayedTutorial", true);
        writer.Commit();
    }

    public void NewGame()
    {
        StartCoroutine(TransitionTo("scn_game"));

        var saveWriter = QuickSaveWriter.Create("GameSave");
        var keys = saveWriter.GetAllKeys();
        foreach (var key in keys)
            saveWriter.Delete(key);
        saveWriter.Commit();
        var infoWriter = QuickSaveWriter.Create("GameInfo");
        var infoKeys = infoWriter.GetAllKeys();
        foreach (var key in infoKeys)
            infoWriter.Delete(key);
        infoWriter.Commit();
        infoWriter = QuickSaveWriter.Create("ItemInfo");
        infoKeys = infoWriter.GetAllKeys();
        foreach (var key in infoKeys)
            infoWriter.Delete(key);
        infoWriter.Commit();
        infoWriter = QuickSaveWriter.Create("InventoryInfo");
        infoKeys = infoWriter.GetAllKeys();
        foreach (var key in infoKeys)
            infoWriter.Delete(key);
        infoWriter.Commit();
    }

    public void ContinueGame()
    {
        var reader = QuickSaveReader.Create("GameSave");

        var infoWriter = QuickSaveWriter.Create("GameInfo");
        var infoKeys = infoWriter.GetAllKeys();
        foreach (var key in infoKeys)
            infoWriter.Delete(key);
        infoWriter.Commit();
        infoWriter = QuickSaveWriter.Create("ItemInfo");
        infoKeys = infoWriter.GetAllKeys();
        foreach (var key in infoKeys)
            infoWriter.Delete(key);
        infoWriter.Commit();
        infoWriter = QuickSaveWriter.Create("InventoryInfo");
        infoKeys = infoWriter.GetAllKeys();
        foreach (var key in infoKeys)
            infoWriter.Delete(key);
        infoWriter.Commit();

        StartCoroutine(TransitionTo(reader.Read<string>("CurrentScene")));
    }

    public void ExitMessage(GameObject exitPanel)
    {
        exitPanel.SetActive(!exitPanel.activeSelf);
    }

    /// <summary>
    /// Closes the game.
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    #endregion ==========================================================================
}