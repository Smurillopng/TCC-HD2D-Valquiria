using System;
using System.Collections;
using System.Collections.Generic;
using CI.QuickSave;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FastTravelManager : MonoBehaviour
{
    public static FastTravelManager Instance { get; private set; }

    [SerializeField] private List<FastTravelPointData> fastTravelLocations;
    private SceneTransitioner _sceneTransitioner;
    public List<FastTravelPointData> FastTravelLocations => fastTravelLocations;

    private string _scene, _name;
    private Vector3 _position;
    private bool _discovered;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        LoadGameSaveData();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void LoadGameSaveData()
    {
        var reader = QuickSaveReader.Create("GameSave");
        var keys = reader.GetAllKeys();
        foreach (var key in keys)
        {
            if (key.Contains("ft:"))
            {
                if (key.Contains("scene"))
                    _scene = reader.Read<string>(key);
                else if (key.Contains("position"))
                    _position = reader.Read<Vector3>(key);
                else if (key.Contains("discovered"))
                    _discovered = reader.Read<bool>(key);
                else
                    _name = reader.Read<string>(key);

                if (!fastTravelLocations.Exists(x => x.fastTravelName == _name))
                    AddPoint(AddPointData(_name, _scene, _position, _discovered));
                else
                    UpdatePoint(AddPointData(_name, _scene, _position, _discovered));
            }
        }
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        StartManager();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void StartManager()
    {
        _sceneTransitioner = FindObjectOfType<SceneTransitioner>();
        var fastTravelPoints = FindObjectsOfType<FastTravelPoint>();
        foreach (var point in fastTravelPoints)
        {
            if (!fastTravelLocations.Exists(x => x.fastTravelName == point.FastTravelName))
            {
                AddPoint(point.PointData());
            }
            if (QuickSaveReader.Create("GameInfo").Exists("IsFastTravel") && QuickSaveReader.Create("GameInfo").Read<bool>("IsFastTravel"))
            {
                QuickSaveWriter.Create("GameInfo").Write("IsFastTravel", false).Commit();
                var player = GameObject.FindGameObjectWithTag("Player");
                player.transform.position = fastTravelLocations.Find(x => x.fastTravelName == point.FastTravelName).position;
            }
        }
    }

    public void TravelTo(FastTravelPointData point)
    {
        QuickSaveWriter.Create("GameInfo").Write("IsFastTravel", true).Commit();
        StartCoroutine(_sceneTransitioner.TransitionTo(point.sceneName));
    }

    public void AddPoint(FastTravelPointData point)
    {
        fastTravelLocations.Add(point);
    }

    public FastTravelPointData AddPointData(string name, string scene, Vector3 position, bool discovered)
    {
        return new FastTravelPointData
        {
            fastTravelName = name,
            sceneName = scene,
            position = position,
            discovered = discovered
        };
    }
    public void UpdatePoint(FastTravelPointData point)
    {
        var index = fastTravelLocations.FindIndex(x => x.fastTravelName == point.fastTravelName);
        fastTravelLocations[index] = point;
        var writer = QuickSaveWriter.Create("GameInfo");
        writer.Write($"ft:{point.fastTravelName}", point.fastTravelName);
        writer.Write($"ft:{point.fastTravelName}_scene", point.sceneName);
        writer.Write($"ft:{point.fastTravelName}_position", point.position);
        writer.Write($"ft:{point.fastTravelName}_discovered", point.discovered);
        writer.Commit();
    }
}
