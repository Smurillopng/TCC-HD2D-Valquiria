// Created by Sérgio Murillo da Costa Faria
// Date: 01/04/2023

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    [Required] public Image icon;
    [Required] public TMP_Text nameText;
    [Required] public TMP_Text quantityText;
    [Required] public Button useButton;

    public void SetItem(IItem item)
    {
        icon.sprite = item.ItemIcon;
        nameText.text = item.ItemName;
        quantityText.text = $"x{item.CurrentStack}";
    }
    public void DisplayItem(TMP_Text itemName, TMP_Text itemDescription, Image itemIcon, TMP_Text itemQuantity, GameObject displayPanel, IItem itemToDisplay)
    {
        if (itemToDisplay.CurrentStack == 0)
        {
            displayPanel.SetActive(false);
            return;
        }
        displayPanel.SetActive(true);
        itemName.text = itemToDisplay.ItemName;
        itemDescription.text = itemToDisplay.ItemDescription;
        itemIcon.sprite = itemToDisplay.ItemIcon;
        itemQuantity.text = $"x{itemToDisplay.CurrentStack}";
    }
}
