// Created by Sérgio Murillo da Costa Faria
// Date: 13/03/2023

using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "RPG/Unit")]
public class Unit : ScriptableObject
{
    [SerializeField]
    private string unitName;
    [SerializeField]
    private Sprite unitSprite;
    [SerializeField, MinValue(1)]
    private int level = 1;
    [SerializeField, MinValue(0)]
    private int experience;
    [SerializeField, MinValue(1)]
    private int maxHp = 1;
    [SerializeField, MinValue(0), ProgressBar(0,"MaxHp", r : 0, g : 1, b : 0 )]
    private int currentHp;
    [SerializeField, MinValue(1)]
    private int maxTp = 100;
    [SerializeField, MinValue(0), MaxValue(100),ProgressBar(0,"MaxTp", r: 0, g: 0.35f, b: 0.75f)]
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
    [SerializeField]
    private bool isPlayer;
    [SerializeField]
    private bool isDead;
    [SerializeField]
    private bool hasTakenTurn;
    
    public string UnitName => unitName;
    public Sprite UnitSprite => unitSprite;
    public int Level => level;
    public int Experience => experience;
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
}
