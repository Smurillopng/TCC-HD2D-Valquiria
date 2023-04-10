// Created by Sérgio Murillo da Costa Faria
// Date: 04/04/2023

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [BoxGroup("Panels")]
    [SerializeField] private GameObject inventoryPanel;
    [BoxGroup("Panels")]
    [SerializeField] private GameObject bagPanel;
    [BoxGroup("Panels")]
    [SerializeField] private GameObject equipmentPanel;

    [BoxGroup("Equipment Slots")]
    [SerializeField] private Image headSlot;
    [BoxGroup("Equipment Slots")]
    [SerializeField] private Image chestSlot;
    [BoxGroup("Equipment Slots")]
    [SerializeField] private Image legsSlot;
    [BoxGroup("Equipment Slots")]
    [SerializeField] private Image weaponSlot;
    [BoxGroup("Equipment Slots")]
    [SerializeField] private Image runeSlot;

    [BoxGroup("Status")]
    [SerializeField] private TMP_Text playerLvl;
    [BoxGroup("Status")]
    [SerializeField] private Image playerHealthBarFill;
    [BoxGroup("Status")]
    [SerializeField] private TMP_Text playerHealthText;
    [BoxGroup("Status")]
    [SerializeField] private Image playerXpBarFill;
    [BoxGroup("Status")]
    [SerializeField] private TMP_Text playerXpText;

    [BoxGroup("External References")] 
    [SerializeField]
    private Unit playerUnit;
    [SerializeField] private GameObject itemPrefab;
    [BoxGroup("External References")]
    [SerializeField] private BoolVariable isInventoryOpen;

    [BoxGroup("Debug")]
    [SerializeField, ReadOnly] private bool updatedStatus;
    [BoxGroup("Debug")]
    [SerializeField, ReadOnly] private InventoryManager inventoryManager;

    private void Start()
    {
        inventoryManager = InventoryManager.Instance;
    }

    public void ShowBagPanel()
    {
        inventoryPanel.SetActive(true);
        bagPanel.SetActive(true);
        UpdateBag(inventoryManager.Inventory);
        equipmentPanel.SetActive(false);
    }

    public void ShowEquipmentPanel()
    {
        inventoryPanel.SetActive(true);
        bagPanel.SetActive(false);
        UpdateEquipments(inventoryManager.EquipmentSlots);
        equipmentPanel.SetActive(true);
    }

    public void UpdateBag(List<IItem> inventory)
    {
        foreach (Transform child in bagPanel.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (var item in inventory)
        {
            var itemObject = Instantiate(itemPrefab, bagPanel.transform);
            itemObject.GetComponent<ItemUI>().SetItem(item);
        }
    }

    public void UpdateEquipments(List<EquipmentSlot> equipmentSlots)
    {
        if (equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Head).equipItem != null)
            headSlot.sprite = equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Head).equipItem.ItemIcon;
        if (equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Chest).equipItem != null)
            chestSlot.sprite = equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Chest).equipItem.ItemIcon;
        if (equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Legs).equipItem != null)
            legsSlot.sprite = equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Legs).equipItem.ItemIcon;
        if (equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Weapon).equipItem != null)
            weaponSlot.sprite = equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Weapon).equipItem.ItemIcon;
        if (equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Rune).equipItem != null)
            runeSlot.sprite = equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Rune).equipItem.ItemIcon;
    }

    public void UpdatePlayerStatus(Unit unit)
    {
        playerLvl.text = $"Lv. {unit.Level}";
        playerHealthText.text = $"HP: {unit.CurrentHp} / {unit.MaxHp}";
        playerHealthBarFill.fillAmount = (float)unit.CurrentHp / unit.MaxHp;
        playerXpText.text = $"XP: {unit.Experience} / {unit.ExperienceTable[unit.Level+1]}";
        playerXpBarFill.fillAmount = (float)unit.Experience / unit.ExperienceTable[unit.Level+1];
    }

    private void ResetPanels()
    {
        bagPanel.SetActive(false);
        equipmentPanel.SetActive(false);
    }

    public void Update()
    {
        inventoryPanel.SetActive(isInventoryOpen.Value);
        if (isInventoryOpen.Value)
        {
            PlayerControls.Instance.ToggleDefaultControls(false);
            if (!updatedStatus)
            {
                ResetPanels();
                UpdatePlayerStatus(playerUnit);
                updatedStatus = true;
            }
        }
        else
        {
            PlayerControls.Instance.ToggleDefaultControls(true);
            updatedStatus = false;
        }
    }
}