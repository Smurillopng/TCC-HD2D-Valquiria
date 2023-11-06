// Created by Sérgio Murillo da Costa Faria

using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class FastTravelPoint : MonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Fast Travel Point")]
    [BoxGroup("Fast Travel Point/Settings")]
    [SerializeField] private string fastTravelName;
    [SerializeField] private Transform playerTransform;
    
    private string _sceneName;
    private bool _discovered;
    public string FastTravelName => fastTravelName;
    #endregion ==========================================================================
    
    #region === Unity Methods ===========================================================
    private void Start()
    {
        _sceneName = gameObject.scene.name;
    }
    #endregion ==========================================================================

    #region === Methods =================================================================
    public void Discover()
    {
        _discovered = true;
    }

    public void UpdatePoint()
    {
        FastTravelManager.Instance.UpdatePoint(PointData());
    }

    public FastTravelPointData PointData()
    {
        return new FastTravelPointData
        {
            fastTravelName = fastTravelName,
            sceneName = _sceneName,
            position = playerTransform.position,
            discovered = _discovered
        };
    }
    #endregion ==========================================================================
}