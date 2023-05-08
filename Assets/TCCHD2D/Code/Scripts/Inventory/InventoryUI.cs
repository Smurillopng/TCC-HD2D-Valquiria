// Created by Sérgio Murillo da Costa Faria
// Date: 04/04/2023

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [BoxGroup("Panels")][SerializeField] private GameObject inventoryPanel;
    [BoxGroup("Panels")][SerializeField] private GameObject bagPanel;
    [BoxGroup("Panels")][SerializeField] private GameObject equipmentPanel;
    [BoxGroup("Panels")][SerializeField] private GameObject itemDisplayPanel;
    [BoxGroup("Panels")][SerializeField] private GameObject statusPanel;

    [BoxGroup("Equipment Slots")]
    [SerializeField]
    private Image headSlot;

    [BoxGroup("Equipment Slots")]
    [SerializeField]
    private Image chestSlot;

    [BoxGroup("Equipment Slots")]
    [SerializeField]
    private Image legsSlot;

    [BoxGroup("Equipment Slots")]
    [SerializeField]
    private Image weaponSlot;

    [BoxGroup("Equipment Slots")]
    [SerializeField]
    private Image runeSlot;

    [BoxGroup("Status")][SerializeField] private TMP_Text playerLvl;
    [BoxGroup("Status")][SerializeField] private Image playerHealthBarFill;
    [BoxGroup("Status")][SerializeField] private TMP_Text playerHealthText;
    [BoxGroup("Status")][SerializeField] private Image playerXpBarFill;
    [BoxGroup("Status")][SerializeField] private TMP_Text playerXpText;
    
    [BoxGroup("Item Display")][SerializeField] private TMP_Text itemName;
    [BoxGroup("Item Display")][SerializeField] private TMP_Text itemDescription;
    [BoxGroup("Item Display")][SerializeField] private Image itemIcon;
    [BoxGroup("Item Display")][SerializeField] private TMP_Text itemQuantity;
    [BoxGroup("Item Display")][SerializeField] private Button itemUseButton;

    [BoxGroup("External References")]
    [SerializeField]
    private Unit playerUnit;

    [SerializeField] private GameObject itemPrefab;

    [BoxGroup("External References")]
    [SerializeField]
    private BoolVariable isInventoryOpen;

    [BoxGroup("Debug")]
    [SerializeField, ReadOnly]
    private bool updatedStatus;

    [BoxGroup("Debug")]
    [SerializeField, ReadOnly]
    private InventoryManager inventoryManager;
    
    public Unit PlayerUnit => playerUnit;

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
        itemDisplayPanel.SetActive(false);
        statusPanel.SetActive(false);
    }

    public void ShowEquipmentPanel()
    {
        inventoryPanel.SetActive(true);
        bagPanel.SetActive(false);
        itemDisplayPanel.SetActive(false);
        statusPanel.SetActive(false);
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
            var uiScript = itemObject.GetComponent<ItemUI>();
            uiScript.SetItem(item);
            var useButton = uiScript.useButton;
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(() => uiScript.DisplayItem(itemName, itemDescription, itemIcon, itemQuantity, itemDisplayPanel, item));
            if (item.ItemType == ItemTyping.Consumable)
            {
                var consumable = item as Consumable;
                itemUseButton.onClick.RemoveAllListeners();
                itemUseButton.onClick.AddListener(() => consumable.Use());
                itemUseButton.onClick.AddListener(() => ShowBagPanel());
            }
            else if (item.ItemType == ItemTyping.Equipment)
            {
                var equipment = item as Equipment;
                if (inventoryManager.EquipmentSlots.Find(x => equipment != null && x.slotType == equipment.SlotType).equipItem != equipment)
                {
                    itemUseButton.onClick.RemoveAllListeners();
                    itemUseButton.onClick.AddListener(() => inventoryManager.Equip(equipment));
                    itemUseButton.onClick.AddListener(() => ShowBagPanel());
                }
                else
                {
                    itemUseButton.onClick.RemoveAllListeners();
                    itemUseButton.onClick.AddListener(() => inventoryManager.Unequip(equipment));
                    itemUseButton.onClick.AddListener(() => ShowBagPanel());
                }
            }
        }
    }

    public void UpdateEquipments(List<EquipmentSlot> equipmentSlots)
    {
        if (equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Head).equipItem != null)
        {
            headSlot.sprite = equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Head).equipItem.ItemIcon;
            headSlot.type = Image.Type.Simple;
            headSlot.color = new Color(1, 1, 1, 1);
            headSlot.preserveAspect = true;
        }

        if (equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Chest).equipItem != null)
        {
            chestSlot.sprite = equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Chest).equipItem.ItemIcon;
            chestSlot.type = Image.Type.Simple;
            chestSlot.color = new Color(1, 1, 1, 1);
            chestSlot.preserveAspect = true;
        }

        if (equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Legs).equipItem != null)
        {
            legsSlot.sprite = equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Legs).equipItem.ItemIcon;
            legsSlot.type = Image.Type.Simple;
            legsSlot.color = new Color(1, 1, 1, 1);
            legsSlot.preserveAspect = true;
        }

        if (equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Weapon).equipItem != null)
        {
            weaponSlot.sprite = equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Weapon).equipItem.ItemIcon;
            weaponSlot.type = Image.Type.Simple;
            weaponSlot.color = new Color(1, 1, 1, 1);
            weaponSlot.preserveAspect = true;
        }

        if (equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Rune).equipItem != null)
        {
            runeSlot.sprite = equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Rune).equipItem.ItemIcon;
            runeSlot.type = Image.Type.Simple;
            runeSlot.color = new Color(1, 1, 1, 1);
            runeSlot.preserveAspect = true;
        }
    }

    public void UpdatePlayerStatus(Unit unit)
    {
        playerLvl.text = $"Lv. {unit.Level}";
        playerHealthText.text = $"HP: {unit.CurrentHp} / {unit.MaxHp}";
        playerHealthBarFill.fillAmount = (float)unit.CurrentHp / unit.MaxHp;
        playerXpText.text = $"XP: {unit.Experience} / {unit.StatsTables.Find(x => x.Level == unit.Level + 1).Experience}";
        playerXpBarFill.fillAmount = (float)unit.Experience / unit.StatsTables.Find(x => x.Level == unit.Level + 1).Experience;
    }

    private void ResetPanels()
    {
        bagPanel.SetActive(false);
        equipmentPanel.SetActive(false);
        itemDisplayPanel.SetActive(false);
        statusPanel.SetActive(true);
    }

    public void ToggleInventory()
    {
        isInventoryOpen.Value = !isInventoryOpen.Value;
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