using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// This class manages the inventory of the player.
/// </summary>
/// <remarks>
/// Created by Sérgio Murillo da Costa Faria on 01/04/2023.
/// </remarks>
[HideMonoScript]
public class InventoryManager : SerializedMonoBehaviour
{
    #region === Variables ===============================================================

    public static InventoryManager Instance { get; private set; } // Singleton

    [ShowInInspector]
    [Tooltip("The inventory list")]
    private List<IItem> inventory = new();

    [ShowInInspector]
    [Tooltip("The equipment slots list")]
    private List<EquipmentSlot> equipmentSlots = new()
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
    public List<IItem> Inventory => inventory;
    /// <summary>
    /// Gets the equipment slots list.
    /// </summary>
    public List<EquipmentSlot> EquipmentSlots => equipmentSlots;

    [TitleGroup("Debug", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    [Tooltip("If true, the inventory will reset when the game is exited.")]
    private bool resetOnExit;

    #endregion

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
    }
    /// <summary>
    /// Resets the stack count of all items in the inventory to 0 if the resetOnExit flag is set.
    /// </summary>
    private void OnDisable()
    {
        if (!resetOnExit) return;
        foreach (var item in inventory)
            item.CurrentStack = 0;
    }

    #endregion

    #region === Methods =================================================================

    /// <summary>
    /// Adds a consumable item to the inventory.
    /// </summary>
    /// <param name="item">The consumable item to be added.</param>
    public void AddConsumableItem(Consumable item)
    {
        if (inventory.Contains(item) && item.CurrentStack < item.MaxStack)
        {
            item.CurrentStack++;
        }
        else if (item.CurrentStack >= item.MaxStack)
        {
            print("Inventory is full");
        }
        else
        {
            inventory.Add(item);
            item.CurrentStack++;
        }
    }
    /// <summary>
    /// Adds an equipment item to the inventory.
    /// </summary>
    /// <param name="item">The equipment item to be added.</param>
    public void AddEquipmentItem(Equipment item)
    {
        if (inventory.Contains(item) && item.CurrentStack < item.MaxStack)
        {
            item.CurrentStack++;
        }
        else if (item.CurrentStack >= item.MaxStack)
        {
            print("Inventory is full");
        }
        else
        {
            inventory.Add(item);
            item.CurrentStack++;
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
        inventory.Remove(item);
    }
    /// <summary>
    /// Removes an equipment item from the inventory.
    /// </summary>
    /// <param name="item">The equipment item to be removed.</param>
    public void RemoveEquipmentItem(Equipment item)
    {
        inventory.Remove(item);
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
                print("Equipped a Head Item");
                break;
            case EquipmentSlotType.Chest:
                slot.equipItem = equipment;
                print("Equipped a Chest Item");
                break;
            case EquipmentSlotType.Legs:
                slot.equipItem = equipment;
                print("Equipped a Legs Item");
                break;
            case EquipmentSlotType.Weapon:
                slot.equipItem = equipment;
                print("Equipped a Weapon Item");
                break;
            case EquipmentSlotType.Rune:
                slot.equipItem = equipment;
                print("Equipped a Rune Item");
                break;
            default:
                print("Invalid Equipment Slot Type");
                break;
        }
    }
    /// <summary>
    /// Unequips the provided equipment from its slot.
    /// </summary>
    /// <param name="equipment">The equipment to be unequipped.</param>
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
                print("Unequipped a Head Item");
                break;
            case EquipmentSlotType.Chest:
                slot.equipItem = null;
                print("Unequipped a Chest Item");
                break;
            case EquipmentSlotType.Legs:
                slot.equipItem = null;
                print("Unequipped a Legs Item");
                break;
            case EquipmentSlotType.Weapon:
                slot.equipItem = null;
                print("Unequipped a Weapon Item");
                break;
            case EquipmentSlotType.Rune:
                slot.equipItem = null;
                print("Unequipped a Rune Item");
                break;
            default:
                print("Invalid Equipment Slot Type");
                break;
        }
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
            inventory.Remove(item);
        }
        else
        {
            item.CurrentStack--;
            if (item.CurrentStack <= 0)
            {
                inventory.Remove(item);
            }
        }
    }

    #endregion
}

[Serializable]
public class EquipmentSlot
{
    public EquipmentSlotType slotType;
    public Equipment equipItem;
}
