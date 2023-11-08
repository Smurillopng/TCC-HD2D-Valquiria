// Created by Sérgio Murillo da Costa Faria
// Date: 01/04/2023

using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New Equipment Item", menuName = "RPG/New Equipment Item", order = 0), InlineEditor]
public class Equipment : ScriptableObject, IItem
{
    [BoxGroup("!", showLabel: false)]
    [SerializeField, InfoBox("File name to be assign on creation")]
    private string filename;

    [TitleGroup("Type", Alignment = TitleAlignments.Centered)]
    [SerializeField, GUIColor("green")]
    private ItemTyping itemType;

    [SerializeField, GUIColor("green")]
    private EquipmentSlotType slotType;

    [ShowIf("slotType", EquipmentSlotType.Weapon)]
    [TitleGroup("Info", Alignment = TitleAlignments.Centered)]
    [SerializeField, GUIColor("cyan")]
    private AttackType attackType;

    [TitleGroup("Info", Alignment = TitleAlignments.Centered)]
    [SerializeField, GUIColor("cyan")]
    private string itemName;

    [SerializeField, GUIColor("cyan"), PreviewField(160, ObjectFieldAlignment.Center, FilterMode = FilterMode.Point), HideLabel]
    private Sprite itemIcon;

    [SerializeField, GUIColor("cyan"), TextArea]
    private string itemDescription;

    [TitleGroup("Values", Alignment = TitleAlignments.Centered)]
    [SerializeField, GUIColor("yellow")]
    private int maxStack;

    [SerializeField, GUIColor("yellow")]
    private int currentStack;

    [SerializeField, GUIColor("yellow")]
    private int statusValue;

    [SerializeField, HideInInspector]
    private int itemValue; // TODO: Implementar quando der para comprar e vender

    [SerializeField, HideInInspector]
    private int itemID; // TODO: Implementar caso seja útil

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