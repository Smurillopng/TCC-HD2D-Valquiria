// Created by Sérgio Murillo da Costa Faria
// Date: 01/04/2023

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InventoryManager : SerializedMonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [ShowInInspector] public List<IItem> inventory = new();
    //
    [SerializeField] private GameObject inventoryPanel;
    //
    [SerializeField] private GameObject bagPanel;
    //
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private Image headSlot;
    [SerializeField] private Image chestSlot;
    [SerializeField] private Image legsSlot;
    [SerializeField] private Image weaponSlot;
    [SerializeField] private Image runeSlot;
    //
    [SerializeField] private Button bagButton;
    [SerializeField] private Button equipmentButton;
    //
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private BoolVariable isInventoryOpen;
    //
    [TitleGroup("Player Status", Alignment = TitleAlignments.Centered)]
    [SerializeField]
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

    [ShowInInspector]
    public List<EquipmentSlot> equipmentSlots = new()
    {
        new EquipmentSlot {slotType = EquipmentSlotType.Head},
        new EquipmentSlot {slotType = EquipmentSlotType.Chest},
        new EquipmentSlot {slotType = EquipmentSlotType.Legs},
        new EquipmentSlot {slotType = EquipmentSlotType.Weapon},
        new EquipmentSlot {slotType = EquipmentSlotType.Rune}
    };

    private bool _updatedStatus = false;
    private SceneType currentScene;

    public List<IItem> Inventory => inventory;
    public List<EquipmentSlot> EquipmentSlots => equipmentSlots;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        var sceneName = SceneManager.GetActiveScene().name;
        PlayerControls.Instance.SceneMap.TryGetValue(sceneName, out currentScene);

        if (currentScene == SceneType.Game)
        {
            inventoryPanel = GameObject.FindWithTag("InventoryPanel");
            bagPanel = GameObject.FindWithTag("BagPanel");
            equipmentPanel = GameObject.FindWithTag("EquipmentPanel");
            headSlot = GameObject.FindWithTag("HeadSlot").GetComponent<Image>();
            chestSlot = GameObject.FindWithTag("ChestSlot").GetComponent<Image>();
            legsSlot = GameObject.FindWithTag("LegsSlot").GetComponent<Image>();
            weaponSlot = GameObject.FindWithTag("WeaponSlot").GetComponent<Image>();
            runeSlot = GameObject.FindWithTag("RuneSlot").GetComponent<Image>();
            playerLvl = GameObject.FindWithTag("PlayerLvlTMP").GetComponent<TMP_Text>();
            playerHelthbarFill = GameObject.FindWithTag("PlayerHealthFill").GetComponent<Image>();
            playerHealthText = GameObject.FindWithTag("PlayerHealthTMP").GetComponent<TMP_Text>();
            playerTpbarFill = GameObject.FindWithTag("PlayerTPFill").GetComponent<Image>();
            playerTpText = GameObject.FindWithTag("PlayerTPTMP").GetComponent<TMP_Text>();
            bagButton = GameObject.FindWithTag("BagButton").GetComponent<Button>();
            equipmentButton = GameObject.FindWithTag("EquipmentButton").GetComponent<Button>();
            bagButton.onClick.AddListener(ShowBagPanel);
            equipmentButton.onClick.AddListener(ShowEquipmentPanel);
        }
        else
        {
            inventoryPanel = null;
            bagPanel = null;
            equipmentPanel = null;
            headSlot = null;
            chestSlot = null;
            legsSlot = null;
            weaponSlot = null;
            runeSlot = null;
            playerLvl = null;
            playerHelthbarFill = null;
            playerHealthText = null;
            playerTpbarFill = null;
            playerTpText = null;
            bagButton = null;
            equipmentButton = null;
        }
    }

    public void AddConsumableItem(Consumable item)
    {
        inventory.Add(item);
    }
    public void AddEquipmentItem(Equipment item)
    {
        inventory.Add(item);
    }

    public void RemoveConsumableItem(Consumable item)
    {
        inventory.Remove(item);
    }
    public void RemoveEquipmentItem(Equipment item)
    {
        inventory.Remove(item);
    }

    [Button]
    public void UpdateBag()
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

    [Button]
    public void UpdateEquipments()
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

    public void Equip(Equipment equipment)
    {
        var slot = equipmentSlots.Find(x => x.slotType == equipment.SlotType);
        if (slot == null) return;
        if (slot.equipItem == equipment)
        {
            print("Item is already equipped");
            return;
        }
        switch (equipment.SlotType)
        {
            case EquipmentSlotType.Head:
                slot.equipItem = equipment;
                equipment.Equip();
                print("Equipped a Head Item");
                break;
            case EquipmentSlotType.Chest:
                slot.equipItem = equipment;
                equipment.Equip();
                print("Equipped a Chest Item");
                break;
            case EquipmentSlotType.Legs:
                slot.equipItem = equipment;
                equipment.Equip();
                print("Equipped a Legs Item");
                break;
            case EquipmentSlotType.Weapon:
                slot.equipItem = equipment;
                equipment.Equip();
                print("Equipped a Weapon Item");
                break;
            case EquipmentSlotType.Rune:
                slot.equipItem = equipment;
                equipment.Equip();
                print("Equipped a Rune Item");
                break;
            default:
                print("Invalid Equipment Slot Type");
                break;
        }
    }

    public void Unequip(Equipment equipment)
    {
        var slot = equipmentSlots.Find(x => x.slotType == equipment.SlotType);
        if (slot == null) return;
        if (slot.equipItem != equipment)
        {
            print("Item is not equipped");
            return;
        }
        switch (equipment.SlotType)
        {
            case EquipmentSlotType.Head:
                slot.equipItem = null;
                equipment.Unequip();
                print("Unequipped a Head Item");
                break;
            case EquipmentSlotType.Chest:
                slot.equipItem = null;
                equipment.Unequip();
                print("Unequipped a Chest Item");
                break;
            case EquipmentSlotType.Legs:
                slot.equipItem = null;
                equipment.Unequip();
                print("Unequipped a Legs Item");
                break;
            case EquipmentSlotType.Weapon:
                slot.equipItem = null;
                equipment.Unequip();
                print("Unequipped a Weapon Item");
                break;
            case EquipmentSlotType.Rune:
                slot.equipItem = null;
                equipment.Unequip();
                print("Unequipped a Rune Item");
                break;
            default:
                print("Invalid Equipment Slot Type");
                break;
        }
    }

    public void UseItem(Consumable item)
    {
        item.Use();
        inventory.Remove(item);
    }

    public void Update()
    {
        if (currentScene != SceneType.Game) return;
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

[System.Serializable]
public class EquipmentSlot
{
    public EquipmentSlotType slotType;
    public Equipment equipItem;
}
