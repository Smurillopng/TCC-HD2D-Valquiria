// Created by Sérgio Murillo da Costa Faria

using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class SpawnController : MonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Spawn Controller")]
    [BoxGroup("Spawn Controller/Settings")]
    [Tooltip("The start and end points of the spawn area")]
    [SerializeField] private Transform spawnStart, spawnEnd;
    
    public Transform SpawnStart => spawnStart;
    public Transform SpawnEnd => spawnEnd;
    #endregion ==========================================================================
}
