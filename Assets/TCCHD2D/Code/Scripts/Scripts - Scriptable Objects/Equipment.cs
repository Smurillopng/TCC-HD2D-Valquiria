// Created by Sérgio Murillo da Costa Faria
// Date: 01/04/2023

using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New Equipment Item", menuName = "RPG/New Equipment Item", order = 0)]
public class Equipment : ScriptableObject, IItem
{
    [BoxGroup("!", showLabel: false)]
    [SerializeField, InfoBox("File name to be assign on creation")] private string filename;
    [BoxGroup("Type", centerLabel: true, showLabel: true)]
    [SerializeField, GUIColor("green")] private ItemTyping itemType;
    [BoxGroup("Type", centerLabel: true, showLabel: true)]
    [SerializeField, GUIColor("green")] private EquipmentSlotType slotType;
    [ShowIf("slotType", EquipmentSlotType.Weapon)]
    [BoxGroup("Info", centerLabel: true, showLabel: true)]
    [SerializeField, GUIColor("cyan")] private AttackType attackType;
    [BoxGroup("Info", centerLabel: true, showLabel: true)]
    [SerializeField, GUIColor("cyan")] private string itemName;
    [BoxGroup("Info", centerLabel: true, showLabel: true)]
    [SerializeField, GUIColor("cyan"), PreviewField(32, ObjectFieldAlignment.Center, FilterMode = FilterMode.Point), HideLabel]  private Sprite itemIcon;
    [BoxGroup("Info", centerLabel: true, showLabel: true)]
    [SerializeField, GUIColor("cyan"), TextArea] private string itemDescription;
    [BoxGroup("Values", centerLabel: true, showLabel: true)]
    [SerializeField, GUIColor("yellow")] private int maxStack;
    [BoxGroup("Values", centerLabel: true, showLabel: true)]
    [SerializeField, GUIColor("yellow")] private int currentStack;
    [BoxGroup("Values", centerLabel: true, showLabel: true)]
    [SerializeField, GUIColor("yellow")] private int statusValue;
    [SerializeField, HideInInspector] private int itemValue; // TODO: Implementar quando der para comprar e vender
    [SerializeField, HideInInspector] private int itemID; // TODO: Implementar caso seja útil

    public string Filename
    {
        get => filename;
        set => filename = value;
    }
    public ItemTyping ItemType
    {
        get => itemType;
        set => itemType = value;
    }
    public int ItemID
    {
        get => itemID;
        set => itemID = value;
    }

    public EquipmentSlotType SlotType => slotType;
    
    public AttackType AttackType => attackType;

    public string ItemName
    {
        get => itemName;
        set => itemName = value;
    }

    public Sprite ItemIcon
    {
        get => itemIcon;
        set => itemIcon = value;
    }

    public string ItemDescription
    {
        get => itemDescription;
        set => itemDescription = value;
    }

    public int MaxStack
    {
        get => maxStack;
        set => maxStack = value;
    }
    
    public int CurrentStack
    {
        get => currentStack;
        set => currentStack = value;
    }

    public int ItemValue
    {
        get => itemValue;
        set => itemValue = value;
    }

    public int StatusValue => statusValue;
    
    public void OnEnable()
    {
        filename = name;
    }
}