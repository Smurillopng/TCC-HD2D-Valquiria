// Created by Sérgio Murillo da Costa Faria

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[HideMonoScript]
public class VolumeEffectsController : MonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Volume Effects Controller")]
    [BoxGroup("Volume Effects Controller/References")]
    [SerializeField, ReadOnly] private Volume _volume;
    
    [BoxGroup("Volume Effects Controller/References")]
    [SerializeField, ReadOnly] private ChromaticAberration chromaticAberration;
    
    [BoxGroup("Volume Effects Controller/References")]
    [SerializeField] private float _intensity;
    #endregion ==========================================================================

    #region === Unity Methods ===========================================================
    void Start()
    {
        _volume = GetComponent<Volume>();
        _volume.profile.TryGet<ChromaticAberration>(out chromaticAberration);
    }

    void Update()
    {
        chromaticAberration.intensity.value = _intensity;
    }
    #endregion ==========================================================================
}
