using System.Collections;
using CI.QuickSave;
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

    private LiftGammaGain _liftGammaGain;
    private bool _isFading;

    private void Awake()
    {
         volume.profile.TryGet(out _liftGammaGain);
         if (!_isFading)
             StartCoroutine(FadeOut(_liftGammaGain, FindObjectOfType<PlayerMovement>()));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerMovement>();
            LoadFade(goToScene, player);
        }
    }

    public void LoadFade(string sceneName, PlayerMovement playerMove)
    {
        if (!_isFading)
        {
            goToScene = sceneName;
            StartCoroutine(FadeIn(_liftGammaGain, playerMove));
        }
    }

    private IEnumerator FadeIn(LiftGammaGain lgg, PlayerMovement pm)
    {
        _isFading = true;
        float time = 0;
        pm.CanMove.Value = false;
        uiController.SetActive(false);
        while (time < fadeTime)
        {
            time += Time.deltaTime;
            lgg.gamma.value = new Vector4(-1, -1, -1, 0 - time / fadeTime);
            yield return null;
        }

        SceneManager.LoadScene(goToScene);
        if (spawnStart)
        {
            var writer = QuickSaveWriter.Create("GameSave");
            writer.Write("SpawnStart", true);
            writer.Write("SpawnEnd", false);
            writer.Write("ChangingScene", true);
            writer.Commit();
        }
        else if (spawnEnd)
        {
            var writer = QuickSaveWriter.Create("GameSave");
            writer.Write("SpawnStart", false);
            writer.Write("SpawnEnd", true);
            writer.Write("ChangingScene", true);
            writer.Commit();
        }
        
        _isFading = false;
    }

    private IEnumerator FadeOut(LiftGammaGain lgg, PlayerMovement pm)
    {
        float time = 0;
        Vector4 defaultGamma = new Vector4(-1, -1, -1, 0);
        lgg.gamma.value = new Vector4(-1, -1, -1, -1);
        pm.CanMove.Value = false;
        while (time < fadeTime)
        {
            time += Time.deltaTime;
            lgg.gamma.value = Vector4.Lerp(new Vector4(-1, -1, -1, -1), defaultGamma, time / fadeTime);
            yield return null;
        }
        pm.CanMove.Value = true;
        uiController.SetActive(true);
    }
}
