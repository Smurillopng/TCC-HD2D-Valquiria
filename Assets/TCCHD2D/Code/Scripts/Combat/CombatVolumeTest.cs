using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CombatVolumeTest : MonoBehaviour
{
    [SerializeField, Required] private Volume volume;
    [SerializeField] private float initialExposure = -8f;
    [SerializeField, ReadOnly] private ColorAdjustments colorAdjustments;

    // Start is called before the first frame update
    void Start()
    {
        volume.profile.TryGet(out colorAdjustments);
        colorAdjustments.postExposure.value = initialExposure;
    }

    // Update is called once per frame
    void Update()
    {
        colorAdjustments.postExposure.value = Mathf.Lerp(colorAdjustments.postExposure.value, 0f, Time.deltaTime * 5f);
    }
}
