// Created by Sérgio Murillo da Costa Faria
// Date: 01/04/2023

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [ShowInInspector] public List<EquipmentSlot> equipmentSlots = new()
    {
        new EquipmentSlot {slotType = EquipmentSlotType.Head}, 
        new EquipmentSlot {slotType = EquipmentSlotType.Chest}, 
        new EquipmentSlot {slotType = EquipmentSlotType.Legs}, 
        new EquipmentSlot {slotType = EquipmentSlotType.Weapon}, 
        new EquipmentSlot {slotType = EquipmentSlotType.Rune}
    };

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
}

[System.Serializable]
public class EquipmentSlot
{
    public EquipmentSlotType slotType;
    public Equipment equipItem;
}
