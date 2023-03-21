// Created by Sérgio Murillo da Costa Faria
// Date: 13/03/2023

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Responsible for managing the turn order of all units, waiting for player input and selecting actions for the AI.
/// </summary>
public class TurnManager : MonoBehaviour
{
    [TitleGroup("Units in Combat", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private List<UnitController> units = new();
    
    [TitleGroup("Debug Info", Alignment = TitleAlignments.Centered)]
    [SerializeField, ReadOnly]
    private int currentUnitIndex;
    [SerializeField, ReadOnly]
    private bool aiMoved;
    [SerializeField, ReadOnly]
    private UnitController playerUnitController;

    private void Start()
    {
        // Add all player and enemy units to the list
        foreach (var unitObject in GameObject.FindGameObjectsWithTag("Player"))
        {
            units.Add(unitObject.GetComponent<UnitController>());
            playerUnitController = unitObject.GetComponent<UnitController>();
        }
        foreach (var unitObject in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            units.Add(unitObject.GetComponent<UnitController>());
        }

        // Sort the units by speed, so the fastest goes first
        units.Sort((a, b) => b.Unit.Speed.CompareTo(a.Unit.Speed));

        PlayerCombatHUD.TakenAction += TakeAction;
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
        if (currentUnit.Unit.IsPlayer == false && currentUnit.Unit.HasTakenTurn == false && aiMoved == false)
        {
            // Use the AI system to select an action for the enemy
            aiMoved = true;
            currentUnit.SelectAction(playerUnitController);
            PlayerCombatHUD.TakenAction.Invoke();
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

    /// <summary>
    /// Method to be called when the unit has selected an action.
    /// </summary>
    public void TakeAction()
    {
        StartCoroutine(TurnDelay());
    }

    /// <summary>
    /// This is a delay to wait for the unit's animation to finish before setting the HasTakenTurn flag.
    /// </summary>
    private IEnumerator TurnDelay()
    {
        yield return new WaitForSeconds((float)units[currentUnitIndex].Director.duration);
        units[currentUnitIndex].Unit.HasTakenTurn = true;
        if (units[currentUnitIndex].Unit.IsPlayer == false)
            aiMoved = false;
    }

    private void OnDisable()
    {
        foreach (var unit in units)
            unit.Unit.HasTakenTurn = false;
        PlayerCombatHUD.TakenAction -= TakeAction;
    }

    private void OnDestroy()
    {
        PlayerCombatHUD.TakenAction -= TakeAction;
    }
}
