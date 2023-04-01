// Created by Sérgio Murillo da Costa Faria
// Date: 01/04/2023

using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable Item", menuName = "RPG/New Consumable Item", order = 0)]
public class Consumable : ScriptableObject, IItem
{
    [SerializeField] private int itemID;
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private string itemDescription;
    [SerializeField] private int maxStack;
    [SerializeField] private int itemValue;

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

    public int ItemValue
    {
        get => itemValue;
        set => itemValue = value;
    }

    public void Use()
    {
        Debug.Log("Used");
    }
    
    public void Discard()
    {
        Debug.Log("Discarded");
    }
}