// Created by Sérgio Murillo da Costa Faria

using System;
using System.Collections.Generic;
using System.Linq;
using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

[HideMonoScript]
public class InventoryManager : SerializedMonoBehaviour
{
    #region === Variables ===============================================================

    public static InventoryManager Instance { get; private set; } // Singleton

    [FoldoutGroup("Inventory Manager")]
    [BoxGroup("Inventory Manager/Inventory")]
    [ShowInInspector, Tooltip("The inventory list")]
    private List<IItem> _inventory = new();

    [BoxGroup("Inventory Manager/Equipment")]
    [ShowInInspector, Tooltip("The equipment slots list")]
    private List<EquipmentSlot> _equipmentSlots = new()
    {
        new EquipmentSlot {slotType = EquipmentSlotType.Head},
        new EquipmentSlot {slotType = EquipmentSlotType.Chest},
        new EquipmentSlot {slotType = EquipmentSlotType.Legs},
        new EquipmentSlot {slotType = EquipmentSlotType.Weapon},
        new EquipmentSlot {slotType = EquipmentSlotType.Rune}
    };

    /// <summary>
    /// Gets the inventory list.
    /// </summary>
    public List<IItem> Inventory => _inventory;
    /// <summary>
    /// Gets the equipment slots list.
    /// </summary>
    public List<EquipmentSlot> EquipmentSlots => _equipmentSlots;

    [BoxGroup("Inventory Manager/Debug")]
    [SerializeField, Tooltip("If true, the inventory will reset when the game is exited.")]
    private bool resetOnExit;

    private bool _inventoryLoaded;

    #endregion ==========================================================================

    #region === Unity Methods ===========================================================

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// This is where the singleton is created.
    /// </summary>
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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (SceneManager.GetActiveScene().name == "scn_mainMenu" && this != null)
            Destroy(gameObject);
        else
            LoadInventory();
    }

    /// <summary>
    /// Resets the stack count of all items in the inventory to 0 if the resetOnExit flag is set.
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (!resetOnExit) return;
        foreach (var item in _inventory)
            item.CurrentStack = 0;
    }

    #endregion ==========================================================================

    #region === Methods =================================================================

    private void LoadInventory()
    {
        var itemReader = QuickSaveReader.Create("GameSave");
        if (SceneManager.GetActiveScene().name == "scn_mainMenu")
            _inventoryLoaded = false;

        if (!_inventoryLoaded)
        {
            foreach (var key in itemReader.GetAllKeys())
            {
                if (_inventory.Exists(item => item.ItemName == key))
                    _inventory.Find(item => item.ItemName == key).CurrentStack = itemReader.Read<int>(key);
                else
                {
                    var possibleConsumables = Resources.LoadAll<Consumable>("Scriptable Objects/Items/");
                    var possibleEquipment = Resources.LoadAll<Equipment>("Scriptable Objects/Items/");
                    if (possibleConsumables.Any(item => item.ItemName == key))
                    {
                        var item = possibleConsumables.First(item => item.ItemName == key);
                        item.CurrentStack = itemReader.Read<int>(key);
                        if (item.CurrentStack > 0) _inventory.Add(item);
                    }
                    else if (possibleEquipment.Any(item => item.ItemName == key))
                    {
                        var item = possibleEquipment.First(item => item.ItemName == key);
                        item.CurrentStack = itemReader.Read<int>(key);
                        if (item.CurrentStack > 0) _inventory.Add(item);
                    }
                }
            }
            var equipmentReader = QuickSaveReader.Create("GameSave");
            foreach (var key in equipmentReader.GetAllKeys())
            {
                if (_equipmentSlots.Exists(slot => slot.slotType.ToString() == key))
                {
                    var equipSlot = _equipmentSlots.Find(slot => slot.slotType.ToString() == key);
                    var possibleEquipment = Resources.LoadAll<Equipment>("Scriptable Objects/Items/");
                    if (possibleEquipment.Any(item => item.ItemName == equipmentReader.Read<string>(key)))
                    {
                        var item = possibleEquipment.First(item => item.ItemName == equipmentReader.Read<string>(key));
                        equipSlot.equipItem = item;
                    }
                }
            }
            _inventoryLoaded = true;
        }
    }
    /// <summary>
    /// Adds a consumable item to the inventory.
    /// </summary>
    /// <param name="item">The consumable item to be added.</param>
    public void AddConsumableItem(Consumable item)
    {
        var writer = QuickSaveWriter.Create("InventoryInfo");
        if (_inventory.Contains(item) && item.CurrentStack < item.MaxStack)
        {
            item.CurrentStack++;
            writer.Write(item.ItemName, item.CurrentStack);
            writer.Commit();
        }
        else if (item.CurrentStack >= item.MaxStack)
        {
            print("Inventory is full");
        }
        else
        {
            _inventory.Add(item);
            item.CurrentStack++;
            writer.Write(item.ItemName, item.CurrentStack);
            writer.Commit();
        }
    }
    /// <summary>
    /// Adds an equipment item to the inventory.
    /// </summary>
    /// <param name="item">The equipment item to be added.</param>
    public void AddEquipmentItem(Equipment item)
    {
        var writer = QuickSaveWriter.Create("InventoryInfo");
        if (_inventory.Contains(item) && item.CurrentStack < item.MaxStack)
        {
            item.CurrentStack++;
            writer.Write(item.ItemName, item.CurrentStack);
            writer.Commit();
        }
        else if (item.CurrentStack >= item.MaxStack)
        {
            print("Inventory is full");
        }
        else
        {
            _inventory.Add(item);
            item.CurrentStack++;
            writer.Write(item.ItemName, item.CurrentStack);
            writer.Commit();
        }
    }
    /// <summary>
    /// Adds an item to the inventory.
    /// </summary>
    /// <param name="item">The item to be added.</param>
    public void AddItem(IItem item)
    {
        switch (item)
        {
            case Consumable consumable:
                AddConsumableItem(consumable);
                break;
            case Equipment equipment:
                AddEquipmentItem(equipment);
                break;
        }
    }
    /// <summary>
    /// Removes a consumable item from the inventory.
    /// </summary>
    /// <param name="item">The consumable item to be removed.</param>
    public void RemoveConsumableItem(Consumable item)
    {
        _inventory.Remove(item);
    }
    /// <summary>
    /// Removes an equipment item from the inventory.
    /// </summary>
    /// <param name="item">The equipment item to be removed.</param>
    public void RemoveEquipmentItem(Equipment item)
    {
        _inventory.Remove(item);
    }
    /// <summary>
    /// Removes an item from the inventory.
    /// </summary>
    /// <param name="item">The item to be removed.</param>
    public void RemoveItem(IItem item)
    {
        switch (item)
        {
            case Consumable consumable:
                RemoveConsumableItem(consumable);
                break;
            case Equipment equipment:
                RemoveEquipmentItem(equipment);
                break;
        }
    }
    /// <summary>
    /// Equips the provided equipment into the appropriate equipment slot.
    /// </summary>
    /// <param name="equipment">The equipment to be equipped.</param>
    public void Equip(Equipment equipment)
    {
        var writer = QuickSaveWriter.Create("InventoryInfo");
        var slot = _equipmentSlots.Find(x => x.slotType == equipment.SlotType);
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
                writer.Write(slot.slotType.ToString(), equipment.ItemName);
                break;
            case EquipmentSlotType.Chest:
                slot.equipItem = equipment;
                writer.Write(slot.slotType.ToString(), equipment.ItemName);
                break;
            case EquipmentSlotType.Legs:
                slot.equipItem = equipment;
                writer.Write(slot.slotType.ToString(), equipment.ItemName);
                break;
            case EquipmentSlotType.Weapon:
                slot.equipItem = equipment;
                writer.Write(slot.slotType.ToString(), equipment.ItemName);
                break;
            case EquipmentSlotType.Rune:
                slot.equipItem = equipment;
                writer.Write(slot.slotType.ToString(), equipment.ItemName);
                break;
            default:
                print("Invalid Equipment Slot Type");
                break;
        }
        writer.Commit();
    }
    /// <summary>
    /// Unequips the provided equipment from its slot.
    /// </summary>
    /// <param name="equipment">The equipment to be unequipped.</param>
    public void Unequip(Equipment equipment)
    {
        var writer = QuickSaveWriter.Create("InventoryInfo");
        var slot = _equipmentSlots.Find(x => x.slotType == equipment.SlotType);
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
                writer.Write(slot.slotType.ToString(), string.Empty);
                break;
            case EquipmentSlotType.Chest:
                slot.equipItem = null;
                writer.Write(slot.slotType.ToString(), string.Empty);
                break;
            case EquipmentSlotType.Legs:
                slot.equipItem = null;
                writer.Write(slot.slotType.ToString(), string.Empty);
                break;
            case EquipmentSlotType.Weapon:
                slot.equipItem = null;
                writer.Write(slot.slotType.ToString(), string.Empty);
                break;
            case EquipmentSlotType.Rune:
                slot.equipItem = null;
                writer.Write(slot.slotType.ToString(), string.Empty);
                break;
            default:
                print("Invalid Equipment Slot Type");
                break;
        }
        writer.Commit();
    }
    /// <summary>
    /// Uses the provided consumable item and removes it from the inventory if it is completely used.
    /// </summary>
    /// <param name="item">The consumable item to be used.</param>
    public void UseItem(Consumable item)
    {
        item.Use();
        if (item.CurrentStack <= 0)
        {
            _inventory.Remove(item);
        }
        else
        {
            item.CurrentStack--;
            if (item.CurrentStack <= 0)
            {
                _inventory.Remove(item);
            }
        }
    }

    #endregion ==========================================================================
}

[Serializable]
public class EquipmentSlot
{
    public EquipmentSlotType slotType;
    public Equipment equipItem;
}
