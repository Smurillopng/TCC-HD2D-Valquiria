// Created by Sérgio Murillo da Costa Faria

using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class ChestHelper : MonoBehaviour
{
    #region === Variables ===============================================================
    
    [FoldoutGroup("Chest Helper")]
    [BoxGroup("Chest Helper/Settings")]
    public GameObject chestItem;
    private Animator _animator;
    
    #endregion ==========================================================================

    #region === Unity Methods ===========================================================
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        CheckSave();
    }
    #endregion ==========================================================================

    #region === Methods =================================================================
    public void CheckSave()
    {
        if (QuickSaveReader.Create("GameSave").Exists($"{chestItem.name}"))
        {
            var saveData = QuickSaveReader.Create("GameSave");
            _animator.Play(saveData.Read<bool>($"{chestItem.name}") ? "Chest Opened" : "Chest Closed"); 
        }
        else if (QuickSaveReader.Create("ItemInfo").Exists($"{chestItem.name}"))
        {
            var infoData = QuickSaveReader.Create("ItemInfo");
            _animator.Play(infoData.Read<bool>($"{chestItem.name}") ? "Chest Opened" : "Chest Closed");
        }
    }
    #endregion ==========================================================================
}
