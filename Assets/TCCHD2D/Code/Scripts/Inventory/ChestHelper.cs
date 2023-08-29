// Created by Sérgio Murillo da Costa Faria
// Date: 28/08/2023

using CI.QuickSave;
using UnityEngine;

public class ChestHelper : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponentInParent<Animator>();
    }

    public void CheckSave()
    {
        if (!QuickSaveReader.Create("GameSave").Exists($"{name}")) return;
        var saveData = QuickSaveReader.Create("GameSave");
        _animator.Play(saveData.Read<bool>($"{name}") ? "Chest Opened" : "Chest Closed");
    }
}
