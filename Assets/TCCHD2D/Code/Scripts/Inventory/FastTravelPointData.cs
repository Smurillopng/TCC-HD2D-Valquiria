using UnityEngine;

[System.Serializable]
public struct FastTravelPointData
{
    public string fastTravelName;
    public string sceneName;
    public Vector3 position;
    public bool discovered;

    public FastTravelPointData(string fastTravelName, string sceneName, Vector3 position, bool discovered)
    {
        this.fastTravelName = fastTravelName;
        this.sceneName = sceneName;
        this.position = position;
        this.discovered = discovered;
    }
}