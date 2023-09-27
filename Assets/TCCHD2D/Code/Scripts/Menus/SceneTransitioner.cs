using System;
using System.Collections;
using CI.QuickSave;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class SceneTransitioner : MonoBehaviour
{
    [SerializeField]
    private string goToScene;
    [SerializeField]
    private Volume volume;
    [SerializeField]
    private float fadeTime;
    [SerializeField] 
    private bool spawnStart, spawnEnd;
    [SerializeField]
    private GameObject uiController;
    
    private static bool _isFading;
    private PlayerMovement _pm;
    // ---
    public RectTransform playerBar, minimap;
    public Material mat;
    public float min, max, current, speedTransition, acelerationValue;
    
    private float _aceleration = 1f;
    private static readonly int CutoffHeight = Shader.PropertyToID("_Cutoff_Height");

    private void Awake()
    {
        playerBar.anchoredPosition = new Vector2(-196.0001f, playerBar.anchoredPosition.y);
        minimap.anchoredPosition = new Vector2(180.0001f, minimap.anchoredPosition.y);

        _pm = FindObjectOfType<PlayerMovement>();
        StartCoroutine(TransitionOut());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var player = other.GetComponent<PlayerMovement>();
        LoadFade(goToScene, player);
    }

    private void LoadFade(string sceneName, PlayerMovement playerMove)
    {
        if (_isFading) return;
        goToScene = sceneName;
        StartCoroutine(TransitionIn());
    }

    private IEnumerator TransitionIn()
    {
        current = min;
        _isFading = true;
        _pm.CanMove.Value = false;
        TiraUI();
        yield return new WaitUntil(() => Math.Abs(playerBar.anchoredPosition.x - (-196.0001f)) < 0.01);
        
        while (current < max)
        {
            if(Math.Abs(current - max) > 0.01 && Math.Abs(current - min) > 0.01)
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
        
        SceneManager.LoadScene(goToScene);
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
        
        _isFading = false;
    }

    private IEnumerator TransitionOut()
    {
        current = max;
        mat.SetFloat(CutoffHeight, current);
        _isFading = true;
        _pm.CanMove.Value = false;
        
        while (current > min)
        {
            if(Math.Abs(current - max) > 0.01 && Math.Abs(current - min) > 0.01)
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
        BotaUI();
        yield return new WaitUntil(() => Math.Abs(playerBar.anchoredPosition.x - 196.0001f) < 0.01);
        _pm.CanMove.Value = true;
        _isFading = false;
    }
    
    private void TiraUI()
    {
        playerBar.DOAnchorPosX(-196.0001f, 1).SetEase(Ease.OutQuad);
        minimap.DOAnchorPosX(180.0001f, 1).SetEase(Ease.OutQuad);
    }

    private void BotaUI()
    {
        playerBar.DOAnchorPosX(196.0001f, 1).SetEase(Ease.OutQuad);
        minimap.DOAnchorPosX(-180.0001f, 1).SetEase(Ease.OutQuad);
    }
}
