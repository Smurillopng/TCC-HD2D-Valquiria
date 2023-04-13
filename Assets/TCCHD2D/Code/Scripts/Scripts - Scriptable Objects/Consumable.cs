// Created by Sérgio Murillo da Costa Faria
// Date: 01/04/2023

using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable Item", menuName = "RPG/New Consumable Item", order = 0)]
public class Consumable : ScriptableObject, IItem
{
    [SerializeField] private int itemID;
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private ConsumableTypes effectType;
    [SerializeField] private string itemDescription;
    [SerializeField] private int maxStack;
    [SerializeField] private int currentStack;
    [SerializeField] private int itemValue;
    [SerializeField] private int effectValue;

    public int ItemID
    {
        get => itemID;
        set => itemID = value;
    }

    public string ItemName
    {
        get => itemName;
        set => itemName = value;
    }

    public Sprite ItemIcon
    {
        get => itemIcon;
        set => itemIcon = value;
    }

    public ConsumableTypes EffectType
    {
        get => effectType;
        set => effectType = value;
    }

    public string ItemDescription
    {
        get => itemDescription;
        set => itemDescription = value;
    }

    public int MaxStack
    {
        get => maxStack;
        set => maxStack = value;
    }
    
    public int CurrentStack
    {
        get => currentStack;
        set => currentStack = value;
    }

    public int ItemValue
    {
        get => itemValue;
        set => itemValue = value;
    }

    public int EffectValue
    {
        get => effectValue;
        set => effectValue = value;
    }

    public void Use()
    {
        switch (effectType)
        {
            case ConsumableTypes.Heal:
                Heal();
                break;
            case ConsumableTypes.Damage:
                Damage();
                break;
            case ConsumableTypes.IncreaseTp:
                IncreaseTp();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Discard()
    {
        Debug.Log("Discarded");
    }

    //In Combat Effects

    public void Heal()
    {
        var target = FindObjectOfType<TurnManager>().PlayerUnitController;
        target.Unit.CurrentHp += EffectValue;
    }

    public void Damage()
    {
        var target = FindObjectOfType<TurnManager>().EnemyUnitController;
        target.Unit.CurrentHp -= EffectValue;
    }

    public void IncreaseTp()
    {
        var target = FindObjectOfType<TurnManager>().PlayerUnitController;
        target.Unit.CurrentTp += EffectValue;
    }
}