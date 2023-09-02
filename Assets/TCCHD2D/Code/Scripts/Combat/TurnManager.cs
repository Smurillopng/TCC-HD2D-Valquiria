using System.Linq;
using System.Collections;
using System.Collections.Generic;
using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Random = UnityEngine.Random;

/// <summary>
/// This enum represents the current state of the combat.
/// </summary>
/// <remarks>
/// It contains the following states: CombatStart, TurnCheck, TurnEnd, PlayerTurn, EnemyTurn, PlayerWon, and PlayerLost.
/// </remarks>
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
/// This class manages the turns of the units in combat.
/// </summary>
/// <remarks>
/// It contains a list of all units in combat, the current state of the combat, the current unit, the current unit index, the turn count, and the delay for changing scenes.
/// </remarks>
public class TurnManager : MonoBehaviour
{
    #region === Variables ===============================================================

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
    private int _turnCount; // The current turn number

    public UnitController PlayerUnitController { get; private set; }
    public UnitController EnemyUnitController { get; private set; }

    #endregion

    #region === Unity Methods ===========================================================

    /// <summary>Initializes the object when it is loaded into the scene.</summary>
    /// <remarks>
    /// Sets the combat state to CombatStart and calls the ManageTurns method to start the combat.
    /// </remarks>
    private void Awake()
    {
        _combatState = CombatState.CombatStart;
        ManageTurns();
    }
    /// <summary>Enables the event handlers for taking player actions.</summary>
    /// <remarks>
    /// This method subscribes to the <see cref="PlayerCombatHUD.TakenAction"/> event, which is raised when the player takes an action.
    /// It registers two event handlers: <see cref="TakeAction"/> and <see cref="PlayerAction"/>.
    /// </remarks>
    private void OnEnable()
    {
        PlayerCombatHUD.TakenAction += TakeAction;
        PlayerCombatHUD.TakenAction += PlayerAction;
    }
    /// <summary>Disables the component.</summary>
    /// <remarks>
    /// This method sets the HasTakenTurn property of each unit in the units collection to false.
    /// It also unsubscribes the TakeAction and PlayerAction methods from the TakenAction event of the PlayerCombatHUD.
    /// </remarks>
    private void OnDisable()
    {
        foreach (var unit in units)
            unit.Unit.HasTakenTurn = false;
        PlayerCombatHUD.TakenAction -= TakeAction;
        PlayerCombatHUD.TakenAction -= PlayerAction;
    }
    /// <summary>Unsubscribes from the TakenAction event of the PlayerCombatHUD.</summary>
    private void OnDestroy()
    {
        PlayerCombatHUD.TakenAction -= TakeAction;
    }

    #endregion

    #region === Methods =================================================================

    /// <summary>Manages the turns in combat.</summary>
    /// <remarks>
    /// This method is responsible for managing the order of turns, checking if a unit is dead or has taken a turn, and
    /// determining whether it is the player's or enemy's turn. It also invokes events when a turn starts and when a player
    /// takes an action. If all units have taken a turn, it resets the HasTakenTurn flag and sets the current unit index to 0.
    /// </remarks>
    private void ManageTurns()
    {
        switch (_combatState)
        {
            case CombatState.CombatStart:
                var reader = QuickSaveReader.Create("GameInfo");
                // Add all (player and enemy) units to the list
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

                EnemyUnitController.Unit = Resources.Load<Unit>("Scriptable Objects/Enemies/" + reader.Read<string>("EncounteredEnemy"));
                var enemyObject = EnemyUnitController.gameObject;
                var playerObject = PlayerUnitController.gameObject;
                enemyObject.GetComponent<SpriteRenderer>().sprite = EnemyUnitController.Unit.UnitSprite;
                var enemyAttackTimeline = EnemyUnitController.Unit.AttackAnimation;
                var enemyDirector = enemyObject.GetComponent<PlayableDirector>();
                foreach (var track in enemyAttackTimeline.GetOutputTracks())
                {
                    switch (track.name)
                    {
                        case "AttackAnimation":
                            enemyDirector.SetGenericBinding(track, enemyObject.GetComponent<Animator>());
                            break;
                        case "MovementAnimation":
                            enemyDirector.SetGenericBinding(track, enemyObject.GetComponent<Animator>());
                            break;
                        case "Audio Track":
                            enemyDirector.SetGenericBinding(track,enemyObject.GetComponent<AudioSource>());
                            break;
                        case "Signals":
                            enemyDirector.SetGenericBinding(track, playerObject.GetComponent<SignalReceiver>());
                            break;
                    }
                }
                enemyDirector.playableAsset = enemyAttackTimeline;

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
                if (_turnCount == 0)
                {
                    StartCoroutine(FirstTurnDelay());
                }
                else
                {
                    ManageTurns();
                }
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
                    PlayerCombatHUD.ForceDisableButtons.Invoke(false);
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
                    isPlayerTurn = false;
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
                StartCoroutine(Victory());
                break;

            case CombatState.PlayerLost:
                GameOver();
                break;
        }
        // Set the current unit and wait for input
        _currentUnit = units[currentUnitIndex];
    }
    /// <summary>Delays the first turn by half a second.</summary>
    /// <returns>An IEnumerator that waits for half a second before incrementing the turn count and managing turns.</returns>
    private IEnumerator FirstTurnDelay()
    {
        yield return new WaitForSeconds(0.5f);
        _turnCount++;
        ManageTurns();
    }
    /// <summary>Sets the ailments for the current turn's unit.</summary>
    /// <remarks>
    /// If it's the player's turn, the ailments for the player unit will be set.
    /// If it's the enemy's turn, the ailments for the enemy unit will be set.
    /// </remarks>
    public void SetAilments()
    {
        switch (_combatState)
        {
            case CombatState.PlayerTurn:
                TriggerAilment(PlayerUnitController);
                break;
            case CombatState.EnemyTurn:
                TriggerAilment(EnemyUnitController);
                break;
        }
    }
    /// <summary>Triggers an ailment on the given target.</summary>
    /// <param name="target">The target to trigger the ailment on.</param>
    /// <remarks>
    /// If the target is on fire, it takes 1 damage and the player's combat HUD is updated. If the target is bleeding or incapacitated, it takes 1 damage and the player's combat HUD is updated. If the target is frozen or stunned, the player's combat HUD is updated.
    /// </remarks>
    private void TriggerAilment(UnitController target)
    {
        var targetAilments = target.gameObject.GetComponent<Ailments>();
        if (targetAilments.OnFire)
        {
            target.TakeDamage(1);
            PlayerCombatHUD.UpdateCombatHUD();
            CheckGameOver();
        }
        if (targetAilments.Frozen)
        {
            PlayerCombatHUD.TakenAction.Invoke();
        }
        if (targetAilments.Stunned)
        {
            PlayerCombatHUD.TakenAction.Invoke();
        }
        if (targetAilments.Bleeding)
        {
            target.TakeDamage(1);
            PlayerCombatHUD.UpdateCombatHUD();
            CheckGameOver();
        }
        if (targetAilments.Incapacitated)
        {
            target.TakeDamage(1);
            PlayerCombatHUD.UpdateCombatHUD();
            CheckGameOver();
        }
    }
    /// <summary>Checks if the game is over and updates the combat state accordingly.</summary>
    /// <remarks>
    /// The game is considered over if either all player units are dead or all enemy units are dead.
    /// </remarks>
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
    /// <summary>Loads the "scn_gameOver" scene, indicating that the game is over.</summary>
    private void GameOver()
    {
        SceneManager.LoadScene("scn_gameOver");
    }
    /// <summary>Performs the actions necessary for a victory.</summary>
    /// <returns>An IEnumerator that can be used to wait for the victory actions to complete.</returns>
    /// <remarks>
    /// This method performs the following actions:
    /// - Rewards the player with experience points.
    /// - Rewards the player with items.
    /// - Displays victory text.
    /// - Waits until all item notifications have been displayed.
    /// - Waits for a delay before changing the scene.
    /// - Saves the current scene, experience points, and other relevant data.
    /// - Loads the last scene.
    /// </remarks>
    private IEnumerator Victory()
    {
        XpReward();
        ItemReward();
        DisplayVictoryText();
        yield return new WaitUntil(() => itemNotification.ItemQueue.Count == 0 && !itemNotification.IsDisplaying);
        yield return new WaitForSeconds(sceneChangeDelay);
        var lastScene = QuickSaveReader.Create("GameInfo").Read<string>("LastScene");
        SceneManager.LoadScene(lastScene);
    }
    /// <summary>Calculates and applies the experience reward for defeating an enemy unit.</summary>
    /// <remarks>
    /// If the player's unit will not level up after receiving the experience reward, the experience is simply added to the unit's total.
    /// If the player's unit will level up after receiving the experience reward, the unit's experience is increased by the reward and then checked for level up.
    /// If the player's unit will level up and have excess experience after receiving the reward, the unit's experience is increased by the reward, checked for level up, and then the excess experience is added.
    /// </remarks>
    private void XpReward()
    {
        if (PlayerUnitController.Unit.Experience + EnemyUnitController.Unit.ExperienceDrop <= PlayerUnitController.Unit
                .StatsTables.First(statGroup => statGroup.Level == PlayerUnitController.Unit.Level + 1).Experience)
        {
            PlayerUnitController.Unit.Experience += EnemyUnitController.Unit.ExperienceDrop;
            PlayerUnitController.Unit.CheckLevelUp();
        }
        else if (PlayerUnitController.Unit.Experience + EnemyUnitController.Unit.ExperienceDrop > PlayerUnitController
                     .Unit.StatsTables.First(statGroup => statGroup.Level == PlayerUnitController.Unit.Level + 1)
                     .Experience)
        {
            var xpLeft = PlayerUnitController.Unit.Experience + EnemyUnitController.Unit.ExperienceDrop - PlayerUnitController
                             .Unit.StatsTables.First(statGroup => statGroup.Level == PlayerUnitController.Unit.Level + 1)
                             .Experience;
            PlayerUnitController.Unit.Experience += EnemyUnitController.Unit.ExperienceDrop;
            PlayerUnitController.Unit.CheckLevelUp();
            PlayerUnitController.Unit.Experience = 0;
            PlayerUnitController.Unit.Experience += xpLeft;
        }
    }
    /// <summary>Randomly rewards the player with items dropped by the enemy unit.</summary>
    /// <remarks>The chance of receiving an item is determined by the value associated with each item in the ItemDrops dictionary of the enemy unit.</remarks>
    private void ItemReward()
    {
        foreach (var item in EnemyUnitController.Unit.ItemDrops.Where(item => item.Value > Random.Range(0, 100)))
        {
            itemNotification.AddItemWithNotification(item.Key);
        }
    }
    /// <summary>Displays the victory text on the player's combat HUD.</summary>
    /// <remarks>The victory text includes the name of the defeated enemy unit and the amount of experience points gained by the player.</remarks>
    private void DisplayVictoryText()
    {
        var victoryText =
            $"{EnemyUnitController.Unit.UnitName} foi derrotado!\n" +
            $"Você ganhou {EnemyUnitController.Unit.ExperienceDrop} pontos de experiência!";
        PlayerCombatHUD.CombatTextEvent.Invoke(victoryText);
    }
    /// <summary>Advances the game to the next turn.</summary>
    /// <remarks>This method calls the ManageTurns() method to handle the logic for advancing to the next turn.</remarks>
    public void NextTurn()
    {
        ManageTurns();
    }
    /// <summary>Initiates a coroutine to delay the turn.</summary>
    private void TakeAction()
    {
        StartCoroutine(TurnDelay());
    }
    /// <summary>Ends the player's turn in combat.</summary>
    /// <remarks>
    /// If it is currently the player's turn, this method sets the isPlayerTurn flag to false,
    /// indicating that the player's turn has ended.
    /// </remarks>
    private void PlayerAction()
    {
        if (_combatState == CombatState.PlayerTurn)
            isPlayerTurn = false;
    }
    /// <summary>Delays the turn for a specified duration and updates the game state.</summary>
    /// <returns>An IEnumerator that waits for the specified duration before updating the game state.</returns>
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

    #endregion
}
