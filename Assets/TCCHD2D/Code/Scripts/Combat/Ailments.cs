using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public enum AilmentType
{
    OnFire,
    Frozen,
    Bleeding,
    Stunned,
    Incapacitated,
    None
}

public class Ailment
{
    public bool isActive;
    public int turnsLeft;
}

/// <summary>
/// This class represents the ailments of an object.
/// </summary>
/// <remarks>
/// It contains the OnFire, Frozen, Bleeding, Stunned, and Incapacitated properties.
/// </remarks>
public class Ailments : MonoBehaviour
{
    #region === Properties ==============================================================

    [ShowInInspector, ReadOnly]
    private Dictionary<AilmentType, Ailment> _ailments = new()
    {
        { AilmentType.OnFire, new Ailment() },
        { AilmentType.Frozen, new Ailment() },
        { AilmentType.Bleeding, new Ailment() },
        { AilmentType.Stunned, new Ailment() },
        { AilmentType.Incapacitated, new Ailment() }
    };
    public Dictionary<AilmentType, Ailment> AilmentsDictionary => _ailments;

    #endregion

    #region === Methods =================================================================

    public void SetAilment(AilmentType ailmentType, bool value, int duration)
    {
        _ailments[ailmentType].isActive = value;
        _ailments[ailmentType].turnsLeft = duration;
    }

    public bool HasAilment(AilmentType ailmentType)
    {
        return _ailments[ailmentType].isActive;
    }

    public int GetTurnsLeft(AilmentType ailmentType)
    {
        return _ailments[ailmentType].turnsLeft;
    }

    public void DecrementTurnsLeft()
    {
        foreach (var ailment in _ailments)
        {
            if (ailment.Value.isActive)
            {
                ailment.Value.turnsLeft--;
            }
            if (ailment.Value.turnsLeft <= 0)
            {
                ailment.Value.turnsLeft = 0;
                ailment.Value.isActive = false;
            }
        }
    }

    #endregion
}