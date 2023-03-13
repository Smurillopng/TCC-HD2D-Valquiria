// Created by Sérgio Murillo da Costa Faria
// Date: 08/03/2023

using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New player attribute set", menuName = "RPG/Player Attributes", order = 0)]
public class PlayerAttributesData : ScriptableObject
{
    [SerializeField, MinValue(1)]
    private int level;
    [SerializeField, MinValue(1)]
    private int health;
    [SerializeField, MinValue(1)]
    private int attack;
    [SerializeField, MinValue(1)] 
    private int defence;
    [SerializeField, MinValue(1)] 
    private int speed;
    [SerializeField, MinValue(1)] 
    private int luck;
    [SerializeField, MinValue(1)] 
    private int dexterity;
    [SerializeField, MinValue(0)]
    private int experience;
    
    public int Level => level;
    public int Health => health;
    public int Attack => attack;
    public int Defence => defence;
    public int Speed => speed;
    public int Luck => luck;
    public int Dexterity => dexterity;
    public int Experience => experience;
}