using UnityEngine;

[CreateAssetMenu(fileName = "New Special", menuName = "RPG/New Special", order = 0)]
public class Special : ScriptableObject
{
    public string specialName;
    public SpecialType specialType;
    public int specialCost;
    public int specialDamage;
    public int specialHeal;
}

public enum SpecialType
{
    Heal,
    Damage,
    Buff,
    Debuff
}