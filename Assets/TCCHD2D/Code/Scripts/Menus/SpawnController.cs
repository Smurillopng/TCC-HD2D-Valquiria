using UnityEngine;

public class SpawnController : MonoBehaviour
{
    [SerializeField] private Transform spawnStart, spawnEnd;
    
    public Transform SpawnStart => spawnStart;
    public Transform SpawnEnd => spawnEnd;
}
