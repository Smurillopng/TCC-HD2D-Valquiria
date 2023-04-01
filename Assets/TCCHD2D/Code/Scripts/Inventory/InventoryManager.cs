// Created by Sérgio Murillo da Costa Faria
// Date: 01/04/2023

using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : SerializedMonoBehaviour
{
    [ShowInInspector] public List<IItem> Inventory = new();
    //
    [SerializeField, Required] private GameObject inventoryPanel;
    //
    [SerializeField, Required] private GameObject bagPanel;
    //
    [SerializeField, Required] private GameObject equipmentPanel;
    [SerializeField, Required] private Image headSlot;
    [SerializeField, Required] private Image chestSlot;
    [SerializeField, Required] private Image legsSlot;
    [SerializeField, Required] private Image weaponSlot;
    [SerializeField, Required] private Image runeSlot;
    //
    [SerializeField, Required] private GameObject itemPrefab;
    [SerializeField, Required] private PlayerEquipment playerEquipmentInfo;
    [SerializeField, Required] private BoolVariable isInventoryOpen;
    //
    [TitleGroup("Player Status", Alignment = TitleAlignments.Centered)] [SerializeField]
    private Unit playerUnit;
    [SerializeField]
    private TMP_Text playerLvl;
    [SerializeField]
    private Image playerHelthbarFill;
    [SerializeField]
    private TMP_Text playerHealthText;
    [SerializeField]
    private Image playerTpbarFill;
    [SerializeField]
    private TMP_Text playerTpText;
    private bool _updatedStatus = false;
    
    public void AddItem(IItem item)
    {
        Inventory.Add(item);
    }
    
    public void RemoveItem(IItem item)
    {
        Inventory.Remove(item);
    }

    [Button]
    public void UpdateBag()
    {
        foreach (Transform child in bagPanel.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (var item in Inventory)
        {
            var itemObject = Instantiate(itemPrefab, bagPanel.transform);
            itemObject.GetComponent<ItemUI>().SetItem(item);
        }
    }

    [Button]
    public void UpdateEquipments()
    {
        if (playerEquipmentInfo.equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Head).equipItem != null)
            headSlot.sprite = playerEquipmentInfo.equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Head).equipItem.ItemIcon;
        if (playerEquipmentInfo.equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Chest).equipItem != null)
            chestSlot.sprite = playerEquipmentInfo.equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Chest).equipItem.ItemIcon;
        if (playerEquipmentInfo.equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Legs).equipItem != null)
            legsSlot.sprite = playerEquipmentInfo.equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Legs).equipItem.ItemIcon;
        if (playerEquipmentInfo.equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Weapon).equipItem != null)
            weaponSlot.sprite = playerEquipmentInfo.equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Weapon).equipItem.ItemIcon;
        if (playerEquipmentInfo.equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Rune).equipItem != null)
            runeSlot.sprite = playerEquipmentInfo.equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Rune).equipItem.ItemIcon;
    }
    
    public void UpdateStatus()
    {
        playerLvl.text = $"Lv. {playerUnit.Level}";
        playerHealthText.text = $"HP: {playerUnit.CurrentHp} / {playerUnit.MaxHp}";
        playerHelthbarFill.fillAmount = (float)playerUnit.CurrentHp / playerUnit.MaxHp;
        playerTpText.text = $"TP: {playerUnit.CurrentTp}%";
        playerTpbarFill.fillAmount = (float)playerUnit.CurrentTp / playerUnit.MaxTp;
    }
    
    public void ShowBagPanel()
    {
        bagPanel.SetActive(true);
        UpdateBag();
        equipmentPanel.SetActive(false);
    }
    
    public void ShowEquipmentPanel()
    {
        equipmentPanel.SetActive(true);
        UpdateEquipments();
        bagPanel.SetActive(false);
    }

    public void Update()
    {
        inventoryPanel.SetActive(isInventoryOpen.Value);
        if (isInventoryOpen.Value)
        {
            PlayerControls.Instance.ToggleDefaultControls(false);
            if (!_updatedStatus)
            {
                UpdateStatus();
                _updatedStatus = true;
            }
        }
        else
        {
            PlayerControls.Instance.ToggleDefaultControls(true);
            _updatedStatus = false;
        }
    }
}
