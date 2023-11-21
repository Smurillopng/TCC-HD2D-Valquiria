// Created by Sérgio Murillo da Costa Faria

using System;
using System.Collections;
using CI.QuickSave;
using UnityEngine;
using UnityEngine.SceneManagement;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine.UI;

[HideMonoScript]
public class SceneTransitioner : MonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Scene Transitioner")]
    [BoxGroup("Scene Transitioner/Scene Related")]
    [SerializeField, Required, InlineEditor]
    private StringVariable previousScene;

    [BoxGroup("Scene Transitioner/Scene Related")]
    [SerializeField]
    private string goToScene;

    [BoxGroup("Scene Transitioner/Scene Related")]
    [SerializeField]
    private bool spawnStart, spawnEnd;

    [BoxGroup("Scene Transitioner/Transition Related")]
    [SerializeField]
    private RectTransform playerBar, minimap;

    [BoxGroup("Scene Transitioner/Transition Related")]
    [SerializeField]
    private GameObject transitionCanvas;

    [BoxGroup("Scene Transitioner/Transition Related")]
    [SerializeField, InlineEditor]
    private Material mat;

    [BoxGroup("Scene Transitioner/Transition Related")]
    [SerializeField]
    private Slider slider;

    [BoxGroup("Scene Transitioner/Transition Related")]
    [SerializeField]
    private float min, max, speedTransition, acelerationValue;

    [BoxGroup("Scene Transitioner/Transition Related")]
    [SerializeField]
    private float tweenTime = 1f;

    [BoxGroup("Scene Transitioner/Debug")]
    [SerializeField, ReadOnly]
    private float current;

    [BoxGroup("Scene Transitioner/Debug")]
    [SerializeField, ReadOnly]
    private PlayerMovement _pm;

    [BoxGroup("Scene Transitioner/Debug")]
    [SerializeField, ReadOnly]
    private float _aceleration = 1f;

    public static bool currentlyTransitioning;
    public GameObject TransitionCanvas => transitionCanvas;
    private static readonly int CutoffHeight = Shader.PropertyToID("_Cutoff_Height");
    #endregion ==========================================================================

    #region === Unity Methods ===========================================================
    private void Awake()
    {
        if (playerBar != null)
        {
            playerBar.anchoredPosition = new Vector2(-196.0001f, playerBar.anchoredPosition.y);
            minimap.anchoredPosition = new Vector2(180.0001f, minimap.anchoredPosition.y);
            _pm = FindObjectOfType<PlayerMovement>();
        }
        transitionCanvas = GameObject.FindGameObjectWithTag("Transition");

        StartCoroutine(TransitionOut());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        StartCoroutine(TransitionIn(goToScene));
    }
    #endregion ==========================================================================

    #region === Methods =================================================================
    private IEnumerator TransitionIn(string scene)
    {
        transitionCanvas.SetActive(true);
        if (scene == "scn_optionsMenu")
            previousScene.Value = SceneManager.GetActiveScene().name;
        currentlyTransitioning = true;
        _pm.CanMove.Value = false;
        var asyncOperation = SceneManager.LoadSceneAsync(scene);
        asyncOperation.allowSceneActivation = false;
        current = min;
        TiraUI();
        yield return new WaitUntil(() => Math.Abs(playerBar.anchoredPosition.x - (-196.0001f)) < 0.01);

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
                if (spawnStart)
                {
                    var writer = QuickSaveWriter.Create("GameInfo");
                    writer.Write("SpawnStart", true);
                    writer.Write("SpawnEnd", false);
                    writer.Write("ChangingScene", true);
                    writer.Commit();
                }
                else if (spawnEnd)
                {
                    var writer = QuickSaveWriter.Create("GameInfo");
                    writer.Write("SpawnStart", false);
                    writer.Write("SpawnEnd", true);
                    writer.Write("ChangingScene", true);
                    writer.Commit();
                }

                _pm.CanMove.Value = true;
                currentlyTransitioning = false;
                asyncOperation.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    private IEnumerator TransitionOut()
    {
        transitionCanvas.SetActive(true);
        currentlyTransitioning = true;
        current = max;
        mat.SetFloat(CutoffHeight, current);
        slider.gameObject.SetActive(false);

        while (current > min)
        {
            if (_pm != null) _pm.CanMove.Value = false;
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
        if (playerBar != null) BotaUI();
        yield return new WaitUntil(() => Math.Abs(current - min) < 0.01);
        if (_pm != null) _pm.CanMove.Value = true;
        currentlyTransitioning = false;
        transitionCanvas.SetActive(false);
    }

    public IEnumerator TransitionTo(string scene)
    {
        transitionCanvas.SetActive(true);
        if (scene == "scn_optionsMenu")
            previousScene.Value = SceneManager.GetActiveScene().name;
        currentlyTransitioning = true;
        if (_pm != null) _pm.CanMove.Value = false;
        var asyncOperation = SceneManager.LoadSceneAsync(scene);
        asyncOperation.allowSceneActivation = false;
        current = min;
        if (playerBar != null)
        {
            TiraUI();
            yield return new WaitUntil(() => Math.Abs(playerBar.anchoredPosition.x - (-196.0001f)) < 0.01);
        }

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
            if (asyncOperation.progress >= 0.9f)
            {
                slider.value = 1f;
                yield return new WaitUntil(() => Math.Abs(current - max) < 0.01);
                var writer = QuickSaveWriter.Create("GameInfo");
                writer.Write("SpawnStart", false);
                writer.Write("SpawnEnd", false);
                writer.Write("ChangingScene", true);
                writer.Commit();
                currentlyTransitioning = false;
                asyncOperation.allowSceneActivation = true;
                if (asyncOperation.isDone)
                    _pm.CanMove.Value = true;
            }
            else
            {
                slider.value = asyncOperation.progress;
            }
            yield return null;
        }
    }

    public void TransitionToScene(string scene)
    {
        StartCoroutine(TransitionTo(scene));
    }

    private void TiraUI()
    {
        Tween.UIAnchoredPositionX(playerBar, -196.0001f, tweenTime, Ease.OutQuad);
        Tween.UIAnchoredPositionX(minimap, 180.0001f, tweenTime, Ease.OutQuad);
    }

    private void BotaUI()
    {
        Tween.UIAnchoredPositionX(playerBar, 196.0001f, tweenTime, Ease.OutQuad);
        Tween.UIAnchoredPositionX(minimap, -180.0001f, tweenTime, Ease.OutQuad);
    }
    #endregion ==========================================================================
}