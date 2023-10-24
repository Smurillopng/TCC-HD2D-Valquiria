using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FastTravelPoint : MonoBehaviour
{
    [SerializeField] private string fastTravelName;
    [SerializeField] private Transform playerTransform;
    private string _sceneName;
    private bool _discovered;

    public string FastTravelName => fastTravelName;

    private void Start()
    {
        _sceneName = gameObject.scene.name;
    }

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
}