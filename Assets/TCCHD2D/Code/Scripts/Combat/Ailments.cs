using UnityEngine;

public class Ailments : MonoBehaviour
{
    public bool OnFire { get; private set; }
    public bool Frozen { get; private set; }
    public bool Bleeding { get; private set; }
    public bool Stunned { get; private set; }
    public bool Incapacitated { get; private set; }

    public void SetOnFire(bool value)
    {
        OnFire = value;
    }
    public void SetFrozen(bool value)
    {
        Frozen = value;
    }
    public void SetBleeding(bool value)
    {
        Bleeding = value;
    }
    public void SetStunned(bool value)
    {
        Stunned = value;
    }
    public void SetIncapacitated(bool value)
    {
        Incapacitated = value;
    }
}