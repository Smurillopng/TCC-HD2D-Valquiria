using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New Special", menuName = "RPG/New Special", order = 0), InlineEditor]
public class Special : ScriptableObject
{
    public string specialName;
    public SpecialType specialType;
    [ShowIf("specialType", SpecialType.Debuff)] public AilmentType specialAilment;
    [ShowIf("specialType", SpecialType.Debuff)] public int turnsToLast;
    public int specialCost;
    public int specialDamage;
    public int specialHeal;
    [TextArea]
    public string specialDescription;
}

public enum SpecialType
{
    Heal,
    Damage,
    Buff,
    Debuff
}