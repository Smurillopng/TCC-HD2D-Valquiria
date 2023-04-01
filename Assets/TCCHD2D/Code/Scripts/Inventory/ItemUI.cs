// Created by Sérgio Murillo da Costa Faria
// Date: 01/04/2023

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public IItem Item;
    [Required] public Image icon;
    [Required] public TMP_Text nameText;
    [Required] public TMP_Text quantityText;
    
    public void SetItem(IItem item)
    {
        Item = item;
        icon.sprite = item.ItemIcon;
        nameText.text = item.ItemName;
        quantityText.text = $"x{Item.MaxStack}"; //TODO: trocar para quantidade atual ao invés da máxima
    }
}
