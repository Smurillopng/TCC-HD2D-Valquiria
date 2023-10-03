using UnityEngine;

public class TorchFlicker : MonoBehaviour
{
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Light _pointLight;
    [SerializeField] private float baseIntensity = 0.8f;
    [SerializeField] private float intensityFluctuation = 0.2f;
    [SerializeField] private float flickerSpeed = 0.1f;
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField] private float colorIntensity = 0.1f;
    [SerializeField] private float playerColorIntensity = 0.1f;

    private float _timeOffset;

    private void Start()
    {
        _timeOffset = Random.Range(0f, 10f);
    }

    private void Update()
    {
        float flickerValue = Mathf.PerlinNoise(Time.time * flickerSpeed + _timeOffset, 0);
        float flickerIntensity = baseIntensity + flickerValue * intensityFluctuation;

        _pointLight.intensity = flickerIntensity;
        _pointLight.color = baseColor + new Color(
            Random.Range(-colorIntensity, colorIntensity),
            Random.Range(-colorIntensity, colorIntensity),
            Random.Range(-colorIntensity, colorIntensity)
        );

        _pointLight.transform.localScale = new Vector3(
            Random.Range(0.98f, 1.02f),
            Random.Range(0.98f, 1.02f),
            1
        );
        _spriteRenderer.material.SetFloat("_Intensity", flickerIntensity / playerColorIntensity);
        _spriteRenderer.material.SetColor("_Color", _pointLight.color);
    }
}
