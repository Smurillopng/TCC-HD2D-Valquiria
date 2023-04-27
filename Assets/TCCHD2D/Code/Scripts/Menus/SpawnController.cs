using UnityEngine;

/// <summary>
/// Class that holds the start and end points of the spawn area
/// </summary>
/// <remarks>
/// Created by Sérgio Murillo da Costa Faria on 23/04/2023.
/// </remarks>
public class SpawnController : MonoBehaviour
{
    [Tooltip("The start and end points of the spawn area")]
    [SerializeField] private Transform spawnStart, spawnEnd;
    
    public Transform SpawnStart => spawnStart;
    public Transform SpawnEnd => spawnEnd;
}
