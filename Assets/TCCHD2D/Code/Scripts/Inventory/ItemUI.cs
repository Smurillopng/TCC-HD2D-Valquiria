// Created by Sérgio Murillo da Costa Faria

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[HideMonoScript]
public class ItemUI : MonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Item UI")]
    [BoxGroup("Item UI/References")]
    [Required] public Image icon;
    
    [BoxGroup("Item UI/References")]
    [Required] public TMP_Text nameText;
    
    [BoxGroup("Item UI/References")]
    [Required] public TMP_Text quantityText;
    
    [BoxGroup("Item UI/References")]
    [Required] public Button useButton;
    
    [BoxGroup("Item UI/References")]
    [Required] public Image div;
    
    public IItem DisplayedItem;
    #endregion ==========================================================================

    #region === Unity Methods ===========================================================
    private void Awake()
    {
        icon.maskable = true;
        nameText.maskable = true;
        quantityText.maskable = true;
        div.maskable = true;
    }
    #endregion ==========================================================================

    #region === Methods =================================================================
    public void SetItem(IItem item)
    {
        icon.sprite = item.ItemIcon;
        nameText.text = item.ItemName;
        quantityText.text = $"{item.CurrentStack}x";
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
        itemQuantity.text = $"{itemToDisplay.CurrentStack}x";
        DisplayedItem = itemToDisplay;
    }
    #endregion ==========================================================================
}
