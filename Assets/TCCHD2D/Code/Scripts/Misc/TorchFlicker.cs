using UnityEngine;

public class TorchFlicker : MonoBehaviour
{
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private Light _pointLight;
    
    private void Update()
    {
        if (_playerMovement.MovementValue.x == 0 && _playerMovement.MovementValue.y == 0) return;
        _pointLight.intensity = Random.Range(0.5f, 1.0f);
        _pointLight.transform.localScale = new Vector3(Random.Range(0.9f, 1.1f), Random.Range(0.9f, 1.1f), 1);
    }
}
