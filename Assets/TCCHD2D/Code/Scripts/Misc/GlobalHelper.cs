// Created by Sérgio Murillo da Costa Faria

using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class GlobalHelper : MonoBehaviour
{
    #region === Variables ===============================================================
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
    #endregion ==========================================================================

    #region === Unity Methods ===========================================================
    private void Awake()
    {
        if (_instance != null && _instance != this)
            Destroy(gameObject);
        else
        {
            _instance = this;
        }
    }
    #endregion ==========================================================================
    
    #region === Methods =================================================================
    public void DestroyObject(GameObject target)
    {
        target.SetActive(false);
    }
    
    public void SaveDestroyObject(GameObject target)
    {
        var saveData = QuickSaveWriter.Create("ItemInfo");
        saveData.Write($"{target.name}", true);
        saveData.Commit();
        target.SetActive(false);
    }
    
    public void CheckSaveData(GameObject target)
    {
        if (QuickSaveReader.Create("ItemInfo").Exists($"{target.name}"))
        {
            var saveData = QuickSaveReader.Create("ItemInfo");
            if (saveData.Read<bool>($"{target.name}"))
            {
                target.SetActive(false);
            }
        }
    }
    #endregion ==========================================================================
}
