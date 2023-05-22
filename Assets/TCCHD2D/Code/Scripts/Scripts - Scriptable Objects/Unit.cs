// Created by Sérgio Murillo da Costa Faria
// Date: 13/03/2023

#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Timeline;
using System.Linq;

[CreateAssetMenu(fileName = "New Unit", menuName = "RPG/New Unit")]
public class Unit : SerializedScriptableObject
{
    [TitleGroup("Unit Type", Alignment = TitleAlignments.Centered)]
    [SerializeField] private UnitType type;

    [TitleGroup("Appearance", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private string unitName;

    [SerializeField, PreviewField]
    private Sprite unitSprite;

    [TitleGroup("Stats", Alignment = TitleAlignments.Centered)]
    [SerializeField, MinValue(1)]
    private int level = 1;

    [SerializeField, ShowIf("type", UnitType.Enemy)]
    private Dictionary<IItem, int> itemDrops;

    [SerializeField, MinValue(0), ShowIf("type", UnitType.Enemy)]
    private int experienceDrop;

    [SerializeField, MinValue(0), ShowIf("type", UnitType.Player)]
    private int experience;

    [SerializeField, ShowIf("type", UnitType.Player)]
    private readonly List<StatsTable> statsTable = new();

    [SerializeField, ShowIf("type", UnitType.Enemy)]
    private TimelineAsset attackAnimation;

    [SerializeField, MinValue(1)]
    private int maxHp = 1;

    [SerializeField, MinValue(0), ProgressBar(0, "MaxHp", r: 0, g: 1, b: 0)]
    private int currentHp;

    [SerializeField, MinValue(1), ShowIf("type", UnitType.Player)]
    private int maxTp = 100;

    [SerializeField, MinValue(0), MaxValue(100), ProgressBar(0, "MaxTp", r: 0, g: 0.35f, b: 0.75f), ShowIf("type", UnitType.Player)]
    private int currentTp;

    [SerializeField, MinValue(1)]
    private int attack = 1;

    [SerializeField, MinValue(1)]
    private int defence = 1;

    [SerializeField, MinValue(1)]
    private int speed = 1;

    [SerializeField, MinValue(1)]
    private int luck = 1;

    [SerializeField, MinValue(1)]
    private int dexterity = 1;
    
    [SerializeField, ShowIf("type", UnitType.Player)]
    private int attributesPoints;

    [TitleGroup("Conditions", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private bool isPlayer;

    [TitleGroup("Save Settings", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private bool resetOnExit;

    [TitleGroup("Debug Info", Alignment = TitleAlignments.Centered)]
    [SerializeField, ReadOnly]
    private bool isDead;

    [SerializeField, ReadOnly]
    private bool hasTakenTurn;

    public UnitType Type => type;
    public string UnitName => unitName;
    public Sprite UnitSprite => unitSprite;
    public int Level => level;
    public Dictionary<IItem, int> ItemDrops => itemDrops;
    public int ExperienceDrop => experienceDrop;
    public int Experience
    {
        get => experience;
        set => experience = value;
    }
    public List<StatsTable> StatsTables => statsTable;
    public TimelineAsset AttackAnimation => attackAnimation;
    public int MaxHp => maxHp;
    public int CurrentHp
    {
        get => currentHp;
        set => currentHp = value;
    }
    public int MaxTp => maxTp;
    public int CurrentTp
    {
        get => currentTp;
        set => currentTp = value;
    }
    public int Attack
    {
        get => attack;
        set => attack = value;
    }
    public int Defence
    {
        get => defence;
        set => defence = value;
    }
    public int Speed
    {
        get => speed;
        set => speed = value;
    }

    public int Luck
    {
        get => luck;
        set => luck = value;
    }

    public int Dexterity
    {
        get => dexterity;
        set => dexterity = value;
    }
    public int AttributesPoints
    {
        get => attributesPoints;
        set => attributesPoints = value;
    }
    public bool IsPlayer => isPlayer;
    public bool IsDead
    {
        get => isDead;
        set => isDead = value;
    }
    public bool HasTakenTurn
    {
        get => hasTakenTurn;
        set => hasTakenTurn = value;
    }

#if UNITY_EDITOR
    protected void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    protected void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }
    protected void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && resetOnExit)
        {
            level = 1;
            experience = 0;
            maxHp = 20;
            currentHp = maxHp;
            maxTp = 100;
            currentTp = 0;
            attack = 10;
            defence = 1;
            speed = 1;
            luck = 1;
            dexterity = 1;
        }
    }
#else
    protected void OnDisable()
    {
        if (resetOnExit)
        {
            level = 1;
            experience = 0;
            maxHp = 20;
            currentHp = maxHp;
            maxTp = 100;
            currentTp = 0;
            attack = 10;
            defence = 1;
            speed = 1;
            luck = 1;
            dexterity = 1;
        }
    }
#endif
    public void CheckLevelUp()
    {
        if (!statsTable.Any(statGroup => statGroup.Level == level + 1) || experience < statsTable.First(statGroup => statGroup.Level == level + 1).Experience) return;
        level++;
        experience = 0;
        attributesPoints++;
    }

    public struct StatsTable
    {
        public int Level;
        public int Experience;
    }
}

public enum UnitType
{
    Player,
    Enemy
}
public enum StatType
{
    MaxHp,
    CurrentHp,
    MaxTp,
    Attack,
    Defence,
    Speed,
    Luck,
    Dexterity
}