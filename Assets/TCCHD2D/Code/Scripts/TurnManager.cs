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
        if (currentUnit.Unit.IsPlayer)
        {
            // Wait for the player to select an action
            // This could be done using Unity's UI system or by using keyboard/mouse input
        }
        else
        {
            // Use the AI system to select an action for the enemy
            currentUnit.SelectAction();
        }

        // Set the current unit's turn flag
        currentUnit.Unit.HasTakenTurn = true;

        // Check if all units have taken a turn
        if (units.All(unit => unit.Unit.HasTakenTurn))
        {
            // Reset the turn flags for all units and start over from the beginning
            foreach (var unit in units)
            {
                unit.Unit.HasTakenTurn = false;
            }
            currentUnitIndex = 0;
        }
    }
}
