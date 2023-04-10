// Created by Sérgio Murillo da Costa Faria
// Date: 13/03/2023

#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "RPG/New Unit")]
public class Unit : SerializedScriptableObject
{
    [TitleGroup("Unit Type", Alignment = TitleAlignments.Centered)]
    [SerializeField] private UnitType type;
    
    [TitleGroup("Appearance", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private string unitName;

    [SerializeField]
    private Sprite unitSprite;

    [TitleGroup("Stats", Alignment = TitleAlignments.Centered)]
    [SerializeField, MinValue(1)]
    private int level = 1;
    
    [SerializeField, MinValue(0), ShowIf("type", UnitType.Enemy)]
    private int experienceDrop;

    [SerializeField, MinValue(0), ShowIf("type", UnitType.Player)]
    private int experience;

    [SerializeField, ShowIf("type", UnitType.Player)]
    private readonly Dictionary<int,int> experienceTable = new();

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
    public int ExperienceDrop => experienceDrop;
    public int Experience
    {
        get => experience;
        set => experience = value;
    }

    public Dictionary<int,int> ExperienceTable => experienceTable;
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
    public int Attack => attack;
    public int Defence => defence;
    public int Speed => speed;
    public int Luck => luck;
    public int Dexterity => dexterity;
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
            currentHp = maxHp;
        }
    }
#else
    protected void OnDisable()
    {
        if (resetOnExit)
        {
            currentHp = maxHp;
        }
    }
#endif
    public void CheckLevelUp()
    {
        if (!experienceTable.ContainsKey(level + 1) || experience < experienceTable[level + 1]) return;
        level++;
        experience = 0;
    }
}

public enum UnitType
{
    Player,
    Enemy
}