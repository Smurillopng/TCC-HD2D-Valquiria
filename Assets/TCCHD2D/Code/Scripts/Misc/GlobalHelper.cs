// Created by Sérgio Murillo da Costa Faria
// Date: 03/04/2023

using CI.QuickSave;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalHelper : MonoBehaviour
{
    private static GlobalHelper _instance;
    public static GlobalHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GlobalHelper>();
                if (_instance == null)
                {
                    GameObject globalHelper = new GameObject("GlobalHelper");
                    _instance = globalHelper.AddComponent<GlobalHelper>();
                }
            }
            return _instance;
        }
    }

    public string SavedScene { get; private set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
            Destroy(gameObject);
        else
        {
            _instance = this;
        }
    }
    public void DestroyObject(GameObject target)
    {
        target.SetActive(false);
    }
    
    public void SaveDestroyObject(GameObject target)
    {
        var saveData = QuickSaveWriter.Create("GameSave");
        saveData.Write($"{target.name}", true);
        saveData.Commit();
        target.SetActive(false);
    }
    
    public void CheckSaveData(GameObject target)
    {
        if (QuickSaveReader.Create("GameSave").Exists($"{target.name}"))
        {
            var saveData = QuickSaveReader.Create("GameSave");
            if (saveData.Read<bool>($"{target.name}"))
            {
                target.SetActive(false);
            }
        }
    }
    
    public string SaveScene()
    {
        return SavedScene = SceneManager.GetActiveScene().name;
    }
}
