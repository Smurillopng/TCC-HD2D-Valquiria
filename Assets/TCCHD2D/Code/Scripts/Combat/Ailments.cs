using UnityEngine;

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

    #endregion

    #region === Methods =================================================================

    /// <summary>Sets the OnFire property to the specified value.</summary>
    /// <param name="value">The value to set the OnFire property to.</param>
    public void SetOnFire(bool value)
    {
        OnFire = value;
    }
    /// <summary>Sets the frozen state of an object.</summary>
    /// <param name="value">The value to set the frozen state to.</param>
    public void SetFrozen(bool value)
    {
        Frozen = value;
    }
    /// <summary>Sets the bleeding property of an object.</summary>
    /// <param name="value">The value to set the bleeding property to.</param>
    public void SetBleeding(bool value)
    {
        Bleeding = value;
    }
    /// <summary>Sets the stunned state of an object.</summary>
    /// <param name="value">The value to set the stunned state to.</param>
    public void SetStunned(bool value)
    {
        Stunned = value;
    }
    /// <summary>Sets the incapacitated status of an object.</summary>
    /// <param name="value">The new incapacitated status.</param>
    public void SetIncapacitated(bool value)
    {
        Incapacitated = value;
    }

    #endregion
}