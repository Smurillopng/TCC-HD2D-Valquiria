using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// This class represents a set of combat scenarios.
/// </summary>
/// <remarks>
/// It contains a dictionary of combat scenarios.
/// </remarks>
public class SetScenario : SerializedMonoBehaviour
{
    public Dictionary<CombatScenarios, GameObject> scenarios;
}
