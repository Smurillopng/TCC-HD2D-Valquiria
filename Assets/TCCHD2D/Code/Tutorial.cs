using System.Collections;
using CI.QuickSave;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class Tutorial : MonoBehaviour
{
    public bool playedTutorial;
    public Volume postProcessingVolume;
    public PlayerMovement playerMovement;
    public CinemachineBrain cinemachineBrain;
    public CinemachineVirtualCamera cinemachineVirtualCamera;
    private LiftGammaGain _liftGainGamma;
    private Camera _mainCam;
    
    private void Start()
    {
        if (playerMovement == null) FindObjectOfType<PlayerMovement>().GetComponent<PlayerMovement>();
        if (cinemachineBrain == null) cinemachineBrain = FindObjectOfType<CinemachineBrain>();
        if (cinemachineVirtualCamera == null) cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        _mainCam = Camera.main;
        
        var reader = QuickSaveReader.Create("GameSave");
        if (reader.Exists("playedTutorial"))
        {
            playedTutorial = reader.Read<bool>("playedTutorial");
        }
        else
        {
            playedTutorial = false;
        }
        
        if (playedTutorial)
        {
            Destroy(gameObject);
        }
        else
        {
            playedTutorial = true;
            var writer = QuickSaveWriter.Create("GameSave");
            writer.Write("playedTutorial", playedTutorial);
            writer.Commit();
        }    
    }

    public void StartTutorial()
    {
        if (postProcessingVolume != null) postProcessingVolume.profile.TryGet(out _liftGainGamma);
        StartCoroutine(FadeOut(_liftGainGamma));
    }
    
    private IEnumerator FadeOut(LiftGammaGain lgg)
    {
        float time = 0;
        playerMovement.CanMove.Value = false;
        while (time < 1)
        {
            time += Time.deltaTime;
            lgg.gamma.value = new Vector4(-1, -1, -1, -1 + time / 1f);
            yield return null;
        }
    }
    
    public void CameraShake()
    {
        
    }
}
