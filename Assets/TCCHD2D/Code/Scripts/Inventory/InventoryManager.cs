// Created by Sérgio Murillo da Costa Faria
// Date: 01/04/2023

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class InventoryManager : SerializedMonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [ShowInInspector] private List<IItem> inventory = new();

    [ShowInInspector]
    private List<EquipmentSlot> equipmentSlots = new()
    {
        new EquipmentSlot {slotType = EquipmentSlotType.Head},
        new EquipmentSlot {slotType = EquipmentSlotType.Chest},
        new EquipmentSlot {slotType = EquipmentSlotType.Legs},
        new EquipmentSlot {slotType = EquipmentSlotType.Weapon},
        new EquipmentSlot {slotType = EquipmentSlotType.Rune}
    };

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

    public void AddConsumableItem(Consumable item)
    {
        if (inventory.Contains(item))
        {
            item.CurrentStack++;
        }
        else
        {
            inventory.Add(item);
            item.CurrentStack++;
        }
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
}

[Serializable]
public class EquipmentSlot
{
    public EquipmentSlotType slotType;
    public Equipment equipItem;
}
