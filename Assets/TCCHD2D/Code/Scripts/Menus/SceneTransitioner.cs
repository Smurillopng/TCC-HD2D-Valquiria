using System;
using System.Collections;
using CI.QuickSave;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using PrimeTween;
using Sirenix.OdinInspector;

public class SceneTransitioner : MonoBehaviour
{
    [SerializeField, BoxGroup("Scene Related")]
    private string goToScene;
    [SerializeField, BoxGroup("Scene Related")]
    private bool spawnStart, spawnEnd;
    [SerializeField, BoxGroup("Transition Related")]
    private RectTransform playerBar, minimap;
    [SerializeField, BoxGroup("Transition Related")]
    private Material mat;
    [SerializeField, BoxGroup("Transition Values")]
    private float min, max, speedTransition, acelerationValue;
    [SerializeField, BoxGroup("Transition Values")]
    private float tweenTime = 1f;

    [SerializeField, ReadOnly, BoxGroup("Debug")]
    private float current;
    [SerializeField, ReadOnly, BoxGroup("Debug")]
    private PlayerMovement _pm;
    [SerializeField, ReadOnly, BoxGroup("Debug")]
    private float _aceleration = 1f;

    public static bool currentlyTransitioning;
    private static readonly int CutoffHeight = Shader.PropertyToID("_Cutoff_Height");

    private void Awake()
    {
        if (playerBar != null)
        {
            playerBar.anchoredPosition = new Vector2(-196.0001f, playerBar.anchoredPosition.y);
            minimap.anchoredPosition = new Vector2(180.0001f, minimap.anchoredPosition.y);
            _pm = FindObjectOfType<PlayerMovement>();
        }

        StartCoroutine(TransitionOut());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        StartCoroutine(TransitionIn(goToScene));
    }

    private IEnumerator TransitionIn(string scene)
    {
        currentlyTransitioning = true;
        _pm.CanMove.Value = false;
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

            if (current >= max)
            {
                current = max;
                _aceleration = 1;
            }
        }

        yield return new WaitUntil(() => Math.Abs(current - max) < 0.01);

        SceneManager.LoadScene(scene);
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
    }

    private IEnumerator TransitionOut()
    {
        currentlyTransitioning = true;
        current = max;
        mat.SetFloat(CutoffHeight, current);

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
    }

    public IEnumerator TransitionTo(string scene)
    {
        currentlyTransitioning = true;
        if (_pm != null) _pm.CanMove.Value = false;
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

            if (current >= max)
            {
                current = max;
                _aceleration = 1;
            }
        }

        yield return new WaitUntil(() => Math.Abs(current - max) < 0.01);

        SceneManager.LoadScene(scene);

        if (_pm != null) _pm.CanMove.Value = true;
        currentlyTransitioning = false;
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
}
