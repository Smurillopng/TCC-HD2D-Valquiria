// Created by Sérgio Murillo da Costa Faria
// Date: 13/03/2023

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

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
    [BoxGroup("Units")]
    [SerializeField, Tooltip("List of all units in combat")]
    private List<UnitController> units = new();

    [FoldoutGroup("Events")]
    [SerializeField, Tooltip("Event called at the start of a unit's turn")]
    private UnityEvent onTurnStart;
    [FoldoutGroup("Events")]
    [SerializeField, Tooltip("Event called when a unit's turn ends or is skipped")]
    private UnityEvent onTurnChange;

    [FoldoutGroup("Debug Info")]
    [SerializeField]
    private ItemNotification itemNotification;
    [FoldoutGroup("Debug Info")]
    [SerializeField]
    private float sceneChangeDelay;
    [FoldoutGroup("Debug Info")]
    [SerializeField, ReadOnly, Tooltip("The index of the current unit in the units list")]
    private int currentUnitIndex;
    [FoldoutGroup("Debug Info")]
    [SerializeField, ReadOnly, Tooltip("Whether the current unit is controlled by the AI and has already moved this turn")]
    private bool aiMoved;
    [FoldoutGroup("Debug Info")]
    [SerializeField, ReadOnly, Tooltip("The UnitController component of the current player unit")]

    public bool isPlayerTurn;

    [SerializeField, ReadOnly] private CombatState _combatState; // The current state of the combat
    private UnitController _currentUnit; // The UnitController component of the current unit

    public UnitController PlayerUnitController { get; private set; }
    public UnitController EnemyUnitController { get; private set; }

    private void Awake()
    {
        _combatState = CombatState.CombatStart;
        ManageTurns();
    }

    private void OnEnable()
    {
        PlayerCombatHUD.TakenAction += TakeAction;
        PlayerCombatHUD.TakenAction += PlayerAction;
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
                    PlayerUnitController = unitObject.GetComponent<UnitController>();
                }
                foreach (var unitObject in GameObject.FindGameObjectsWithTag("Enemy"))
                {
                    units.Add(unitObject.GetComponent<UnitController>());
                    EnemyUnitController = unitObject.GetComponent<UnitController>();
                }

                // Sort the units by speed, so the fastest goes first
                foreach (var unit in units)
                {
                    if (unit.Unit.IsPlayer)
                    {
                        if (InventoryManager.Instance.EquipmentSlots[2].equipItem != null)
                            unit.speedCalculated = unit.Unit.Speed + InventoryManager.Instance.EquipmentSlots[2].equipItem.StatusValue;
                        else
                            unit.speedCalculated = unit.Unit.Speed;
                    }
                    else
                    {
                        unit.speedCalculated = unit.Unit.Speed;
                    }
                }
                units = units.OrderByDescending(unit => unit.speedCalculated).ToList();
                _combatState = units[currentUnitIndex].Unit.IsPlayer ? CombatState.PlayerTurn : CombatState.EnemyTurn;
                ManageTurns();
                break;

            case CombatState.TurnCheck:
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
                CheckGameOver();
                ManageTurns();
                break;

            case CombatState.PlayerTurn:
                _currentUnit = units[currentUnitIndex];
                if (_currentUnit.Unit.IsPlayer && !_currentUnit.Unit.HasTakenTurn)
                {
                    _combatState = CombatState.PlayerTurn;
                    onTurnStart.Invoke();
                    isPlayerTurn = true;
                    // Wait for player input
                }
                if (units[currentUnitIndex].Unit.HasTakenTurn)
                {
                    PlayerCombatHUD.TakenAction.Invoke();
                }
                break;

            case CombatState.EnemyTurn:
                _currentUnit = units[currentUnitIndex];
                if (!_currentUnit.Unit.HasTakenTurn && !aiMoved)
                {
                    _combatState = CombatState.EnemyTurn;
                    onTurnStart.Invoke();
                    // Use the AI system to select an action for the enemy
                    aiMoved = true;
                    _currentUnit.SelectAction(PlayerUnitController);
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
                StartCoroutine(Victory());
                break;

            case CombatState.PlayerLost:
                Debug.Log("Player has been defeated!");
                GameOver();
                break;
        }

        // Set the current unit and wait for input
        _currentUnit = units[currentUnitIndex];
    }

    private void CheckGameOver()
    {
        var playerAlive = units.Any(unit => unit.Unit.IsPlayer && !unit.Unit.IsDead);
        var enemyAlive = units.Any(unit => !unit.Unit.IsPlayer && !unit.Unit.IsDead);

        if (!playerAlive)
        {
            _combatState = CombatState.PlayerLost;
        }
        else if (!enemyAlive)
        {
            _combatState = CombatState.PlayerWon;
        }
    }

    private void GameOver()
    {
        SceneManager.LoadScene("scn_gameOver");
    }

    private IEnumerator Victory()
    {
        XpReward();
        ItemReward();
        DisplayVictoryText();
        yield return new WaitUntil(() => itemNotification.ItemQueue.Count == 0 && !itemNotification.IsDisplaying);
        yield return new WaitForSeconds(sceneChangeDelay);
        SceneManager.LoadScene("scn_game");
    }

    private void XpReward()
    {
        PlayerUnitController.Unit.Experience += EnemyUnitController.Unit.ExperienceDrop;
        PlayerUnitController.Unit.CheckLevelUp();
    }

    private void ItemReward()
    {
        foreach (var item in EnemyUnitController.Unit.ItemDrops.Where(item => item.Value > Random.Range(0, 100)))
        {
            itemNotification.AddItemWithNotification(item.Key);
        }
    }
    
    private void DisplayVictoryText()
    {
        var victoryText =
            $"{EnemyUnitController.Unit.UnitName} has been defeated!\n" +
            $"You have gained {EnemyUnitController.Unit.ExperienceDrop} experience!";
        PlayerCombatHUD.CombatTextEvent.Invoke(victoryText);
    }

    public void NextTurn()
    {
        ManageTurns();
    }

    /// <summary>
    /// Method to be called when the unit has selected an action.
    /// </summary>
    private void TakeAction()
    {
        StartCoroutine(TurnDelay());
    }

    private void PlayerAction()
    {
        if (_combatState == CombatState.PlayerTurn)
            isPlayerTurn = false;
    }

    /// <summary>
    /// This is a delay to wait for the unit's animation to finish before setting the HasTakenTurn flag.
    /// </summary>
    private IEnumerator TurnDelay()
    {
        yield return new WaitForSeconds((float)units[currentUnitIndex].Director.duration);
        units[currentUnitIndex].Unit.HasTakenTurn = true;
        if (!units[currentUnitIndex].Unit.IsPlayer)
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
        PlayerCombatHUD.TakenAction -= PlayerAction;
    }

    private void OnDestroy()
    {
        PlayerCombatHUD.TakenAction -= TakeAction;
    }
}
