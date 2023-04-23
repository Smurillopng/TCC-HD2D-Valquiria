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

    private LiftGammaGain liftGammaGain;
    private bool isFading;

    private void Awake()
    {
         volume.profile.TryGet(out liftGammaGain);
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
        if (!isFading)
        {
            goToScene = sceneName;
            StartCoroutine(FadeIn(liftGammaGain, playerMove));
        }
    }

    private IEnumerator FadeIn(LiftGammaGain lgg, PlayerMovement pm)
    {
        isFading = true;
        float time = 0;
        var defaultLgg = lgg.gamma.value;
        pm.CanMove.Value = false;
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
        pm.CanMove.Value = true;
        lgg.gamma.value = defaultLgg;
        isFading = false;
    }
}
