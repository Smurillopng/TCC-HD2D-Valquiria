// Created by Sérgio Murillo da Costa Faria
// Date: 28/08/2023

using CI.QuickSave;
using UnityEngine;

public class ChestHelper : MonoBehaviour
{
    private Animator _animator;
    public GameObject chestItem;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        CheckSave();
    }

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
}
