using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeEffectsController : MonoBehaviour
{
    [SerializeField, ReadOnly] private Volume _volume;
    [SerializeField, ReadOnly] private ChromaticAberration chromaticAberration;
    [SerializeField] private float _intensity;

    void Start()
    {
        _volume = GetComponent<Volume>();
        _volume.profile.TryGet<ChromaticAberration>(out chromaticAberration);
    }

    void Update()
    {
        chromaticAberration.intensity.value = _intensity;
    }
}
