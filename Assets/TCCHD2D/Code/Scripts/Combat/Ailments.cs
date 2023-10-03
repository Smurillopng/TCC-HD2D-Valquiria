using UnityEngine;

public enum AilmentType
{
    OnFire,
    Frozen,
    Bleeding,
    Stunned,
    Incapacitated
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

    public bool OnFire { get; private set; }
    public bool Frozen { get; private set; }
    public bool Bleeding { get; private set; }
    public bool Stunned { get; private set; }
    public bool Incapacitated { get; private set; }

    public int turnsLeft;

    #endregion

    #region === Methods =================================================================

    public void SetAilment(AilmentType ailmentType, bool value, int duration)
    {
        switch (ailmentType)
        {
            case AilmentType.OnFire:
                OnFire = value;
                turnsLeft = duration;
                break;
            case AilmentType.Frozen:
                Frozen = value;
                turnsLeft = duration;
                break;
            case AilmentType.Bleeding:
                Bleeding = value;
                turnsLeft = duration;
                break;
            case AilmentType.Stunned:
                Stunned = value;
                turnsLeft = duration;
                break;
            case AilmentType.Incapacitated:
                Incapacitated = value;
                turnsLeft = duration;
                break;
        }
    }

    #endregion
}