// Created by Sérgio Murillo da Costa Faria
// Date: 13/03/2023

using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField]
    private List<UnitController> units = new();
    [SerializeField, ReadOnly]
    private int currentUnitIndex;

    private void Start()
    {
        // Add all player and enemy units to the list
        foreach (var unitObject in GameObject.FindGameObjectsWithTag("Player"))
        {
            units.Add(unitObject.GetComponent<UnitController>());
        }
        foreach (var unitObject in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            units.Add(unitObject.GetComponent<UnitController>());
        }

        // Sort the units by speed, so the fastest goes first
        units.Sort((a, b) => b.Unit.Speed.CompareTo(a.Unit.Speed));

        PlayerCombatHUD.takenAction += TakeAction;
    }

    private void Update()
    {
        // Check if the current unit is dead or has already taken a turn
        if (units[currentUnitIndex].Unit.IsDead || units[currentUnitIndex].Unit.HasTakenTurn)
        {
            currentUnitIndex++;
            if (currentUnitIndex >= units.Count)
            {
                // If we've reached the end of the list, start over from the beginning
                currentUnitIndex = 0;
            }
        }

        // Set the current unit and wait for input
        var currentUnit = units[currentUnitIndex];
        if (currentUnit.Unit.IsPlayer && currentUnit.Unit.HasTakenTurn == false)
        {
            // Wait for the player to select an action
            // This could be done using Unity's UI system or by using keyboard/mouse input
        }
        else if (currentUnit.Unit.IsPlayer == false && currentUnit.Unit.HasTakenTurn == false)
        {
            // Use the AI system to select an action for the enemy
            currentUnit.SelectAction(units[currentUnitIndex - 1]);

            PlayerCombatHUD.takenAction?.Invoke();
        }

        // Check if all units have taken a turn
        if (units.All(unit => unit.Unit.HasTakenTurn))
        {
            // Reset the turn flags for all units and start over from the beginning
            foreach (var unit in units)
                unit.Unit.HasTakenTurn = false;
            currentUnitIndex = 0;
        }
    }

    public void TakeAction()
    {
        units[currentUnitIndex].Unit.HasTakenTurn = true;
    }

    private void OnDisable()
    {
        foreach (var unit in units)
            unit.Unit.HasTakenTurn = false;
        PlayerCombatHUD.takenAction -= TakeAction;
    }

    private void OnDestroy()
    {
        PlayerCombatHUD.takenAction -= TakeAction;
    }


}
