// Created by Sérgio Murillo da Costa Faria
// Date: 01/04/2023

using UnityEngine;

public interface IItem
{
    int ItemID { get; set; }
    string ItemName { get; set; }
    Sprite ItemIcon { get; set; }
    string ItemDescription { get; set; }
    int MaxStack { get; set; }
    int CurrentStack { get; set; }
    int ItemValue { get; set; }
}