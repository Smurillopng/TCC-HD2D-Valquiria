// Created by Sérgio Murillo da Costa Faria
// Date: 13/03/2023

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum CombatState
{
    CombatStart,
    TurnCheck,
    TurnEnd,
    PlayerTurn,
    EnemyTurn,
    PlayerWon,
    PlayerLost
}

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

    [TitleGroup("Events", Alignment = TitleAlignments.Centered)]
    [SerializeField] 
    private UnityEvent onTurnStart;
    [SerializeField]
    private UnityEvent onTurnChange;
    private UnitController _currentUnit;
    private CombatState _combatState;

    private void Start()
    {
        _combatState = CombatState.CombatStart;
        PlayerCombatHUD.TakenAction += TakeAction;
        ManageTurns();
    }

    private void ManageTurns()
    {
        switch (_combatState)
        {
            case CombatState.CombatStart:
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
                _combatState = units[currentUnitIndex].Unit.IsPlayer ? CombatState.PlayerTurn : CombatState.EnemyTurn;
                break;
            
            case CombatState.TurnCheck:
                CheckGameOver();
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
                _combatState = units[currentUnitIndex].Unit.IsPlayer ? CombatState.PlayerTurn : CombatState.EnemyTurn;
                ManageTurns();
                break;

            case CombatState.PlayerTurn:
                _currentUnit = units[currentUnitIndex];
                if (_currentUnit.Unit.IsPlayer && _currentUnit.Unit.HasTakenTurn == false)
                {
                    _combatState = CombatState.PlayerTurn;
                    onTurnStart.Invoke();
                    // Wait for player input
                }
                if (units[currentUnitIndex].Unit.HasTakenTurn)
                {
                    PlayerCombatHUD.TakenAction.Invoke();
                }
                break;

            case CombatState.EnemyTurn:
                _currentUnit = units[currentUnitIndex];
                if (_currentUnit.Unit.HasTakenTurn == false && aiMoved == false)
                {
                    _combatState = CombatState.EnemyTurn;
                    onTurnStart.Invoke();
                    // Use the AI system to select an action for the enemy
                    aiMoved = true;
                    _currentUnit.SelectAction(playerUnitController);
                    PlayerCombatHUD.TakenAction.Invoke();
                }
                if (units[currentUnitIndex].Unit.HasTakenTurn)
                {
                    PlayerCombatHUD.TakenAction.Invoke();
                }
                break;

            case CombatState.TurnEnd:
                // Check if all units have taken a turn
                if (units.All(unit => unit.Unit.HasTakenTurn))
                {
                    // Reset the turn flags for all units and start over from the beginning
                    foreach (var unit in units)
                        unit.Unit.HasTakenTurn = false;
                    currentUnitIndex = 0;
                }
                _combatState = CombatState.TurnCheck;
                ManageTurns();
                break;

            case CombatState.PlayerWon:
                Debug.Log("Enemy has been defeated!");
                Victory();
                break;

            case CombatState.PlayerLost:
                Debug.Log("Player has been defeated!");
                GameOver();
                break;
        }

        // Set the current unit and wait for input
        _currentUnit = units[currentUnitIndex];
    }
    
    public void CheckGameOver()
    {
        var playerAlive = units.Any(unit => unit.Unit.IsPlayer && !unit.Unit.IsDead);
        var enemyAlive = units.Any(unit => !unit.Unit.IsPlayer && !unit.Unit.IsDead);
        
        if (!playerAlive)
        {
            _combatState = CombatState.PlayerLost;
            ManageTurns();
        }
        else if (!enemyAlive)
        {
            _combatState = CombatState.PlayerWon;
            ManageTurns();
        }
    }
    
    public void GameOver()
    {
        SceneManager.LoadScene("scn_gameOver");
    }
    
    public void Victory()
    {
        SceneManager.LoadScene("scn_game");
    }
    
    public void NextTurn()
    {
        ManageTurns();
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
        yield return new WaitForSeconds(0.5f);
        _combatState = CombatState.TurnEnd;
        onTurnChange.Invoke();
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
