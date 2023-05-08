// Created by Sérgio Murillo da Costa Faria
// Date: 01/04/2023

using UnityEngine;

[CreateAssetMenu(fileName = "New Equipment Item", menuName = "RPG/New Equipment Item", order = 0)]
public class Equipment : ScriptableObject, IItem
{
    [SerializeField] private ItemTyping itemType;
    [SerializeField] private int itemID;
    [SerializeField] private EquipmentSlotType slotType;
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private string itemDescription;
    [SerializeField] private int maxStack;
    [SerializeField] private int currentStack;
    [SerializeField] private int itemValue;
    [SerializeField] private int statusValue;

    public ItemTyping ItemType
    {
        get => itemType;
        set => itemType = value;
    }
    public int ItemID
    {
        get => itemID;
        set => itemID = value;
    }

    public EquipmentSlotType SlotType => slotType;

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

    public int StatusValue => statusValue;
}