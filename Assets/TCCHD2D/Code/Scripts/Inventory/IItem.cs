// Created by Sérgio Murillo da Costa Faria

using UnityEngine;

public interface IItem
{
    ItemTyping ItemType { get; set; }
    int ItemID { get; set; }
    string ItemName { get; set; }
    Sprite ItemIcon { get; set; }
    string ItemDescription { get; set; }
    int MaxStack { get; set; }
    int CurrentStack { get; set; }
    int ItemValue { get; set; }
}

public enum ItemTyping
{
    Consumable,
    Equipment
}