// Created by Sérgio Murillo da Costa Faria
// Date: 03/04/2023

using System.Collections;
using System.Collections.Generic;
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
            DontDestroyOnLoad(gameObject);
        }
    }
    public void DestroyObject(GameObject target)
    {
        Destroy(target);
    }
    
    public string SaveScene()
    {
        return SavedScene = SceneManager.GetActiveScene().name;
    }
}
