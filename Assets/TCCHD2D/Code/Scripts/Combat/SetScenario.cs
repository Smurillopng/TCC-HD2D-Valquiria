// Created by Sérgio Murillo da Costa Faria

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class SetScenario : SerializedMonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Set Scenario")]
    public Dictionary<CombatScenarios, GameObject> scenarios;
    #endregion ==========================================================================
}
