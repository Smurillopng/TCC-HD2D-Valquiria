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
    [SerializeField]
    private SceneTransitioner sceneTransitioner;
    [FoldoutGroup("Debug Info")]
    [SerializeField, ReadOnly, Tooltip("The index of the current unit in the units list")]
    private int currentUnitIndex;
    [FoldoutGroup("Debug Info")]
    [SerializeField, ReadOnly, Tooltip("Whether the current unit is controlled by the AI and has already moved this turn")]
    private bool aiMoved;
    [FoldoutGroup("Debug Info")]
    [SerializeField, ReadOnly, Tooltip("The UnitController component of the current player unit")]

    public bool isPlayerTurn;

    [SerializeField, ReadOnly] private CombatState combatState; // The current state of the combat
    private UnitController _currentUnit; // The UnitController component of the current unit
    private int _turnCount; // The current turn number

    public UnitController PlayerUnitController { get; private set; }
    public UnitController EnemyUnitController { get; private set; }
    public static UnityAction onDeath;
    private Ailments _playerAilments, _enemyAilments;

    #endregion

    #region === Unity Methods ===========================================================

    /// <summary>Initializes the object when it is loaded into the scene.</summary>
    /// <remarks>
    /// Sets the combat state to CombatStart and calls the ManageTurns method to start the combat.
    /// </remarks>
    private void Awake()
    {
        combatState = CombatState.CombatStart;
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
        switch (combatState)
        {
            case CombatState.CombatStart:
                SetEncounter();
                break;
            case CombatState.TurnCheck:
                CheckTurn();
                break;
            case CombatState.PlayerTurn:
                HandlePlayerTurn();
                break;
            case CombatState.EnemyTurn:
                HandleEnemyTurn();
                break;
            case CombatState.TurnEnd:
                EndTurn();
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

    private void SetEncounter()
    {
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

        var soClone = Resources.Load<Unit>("Scriptable Objects/Enemies/" + reader.Read<string>("EncounteredEnemy"));
        soClone = Instantiate(soClone);
        EnemyUnitController.Unit = soClone;
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
                    enemyDirector.SetGenericBinding(track, enemyObject.GetComponent<AudioSource>());
                    break;
                case "Signals":
                    enemyDirector.SetGenericBinding(track, playerObject.GetComponent<SignalReceiver>());
                    break;
                case "HitVfx":
                    enemyDirector.SetGenericBinding(track, playerObject.GetComponent<UnitController>().HitVfx);
                    break;
            }
        }
        enemyDirector.playableAsset = enemyAttackTimeline;

        _playerAilments = PlayerUnitController.gameObject.GetComponent<Ailments>();
        _enemyAilments = EnemyUnitController.gameObject.GetComponent<Ailments>();
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
        combatState = units[currentUnitIndex].Unit.IsPlayer ? CombatState.PlayerTurn : CombatState.EnemyTurn;
        if (_turnCount == 0)
        {
            StartCoroutine(FirstTurnDelay());
        }
        else
        {
            ManageTurns();
        }
    }

    private void CheckTurn()
    {
        if (units[currentUnitIndex].Unit.IsDead || units[currentUnitIndex].Unit.HasTakenTurn)
        {
            currentUnitIndex++;
            if (currentUnitIndex >= units.Count)
            {
                // If we've reached the end of the list, start over from the beginning
                currentUnitIndex = 0;
            }
        }
        combatState = units[currentUnitIndex].Unit.IsPlayer ? CombatState.PlayerTurn : CombatState.EnemyTurn;
        CheckGameOver();
        ManageTurns();
    }

    private void HandlePlayerTurn()
    {
        _currentUnit = units[currentUnitIndex];
        onTurnStart.Invoke();
        if (_currentUnit.Unit.IsPlayer && !_currentUnit.Unit.HasTakenTurn)
        {
            combatState = CombatState.PlayerTurn;
            isPlayerTurn = true;
            PlayerCombatHUD.ForceDisableButtons.Invoke(false);
            // Wait for player input
        }
        if (units[currentUnitIndex].Unit.HasTakenTurn)
        {
            PlayerCombatHUD.TakenAction.Invoke();
        }
    }

    private void HandleEnemyTurn()
    {
        _currentUnit = units[currentUnitIndex];
        onTurnStart.Invoke();
        if (!_currentUnit.Unit.HasTakenTurn && !aiMoved)
        {
            combatState = CombatState.EnemyTurn;
            isPlayerTurn = false;
            // Use the AI system to select an action for the enemy
            aiMoved = true;
            _currentUnit.SelectAction(PlayerUnitController);
            PlayerCombatHUD.TakenAction.Invoke();
        }
        if (units[currentUnitIndex].Unit.HasTakenTurn)
        {
            PlayerCombatHUD.TakenAction.Invoke();
        }
    }

    private void EndTurn()
    {
        // Check if all units have taken a turn
        if (units.All(unit => unit.Unit.HasTakenTurn))
        {
            // Reset the turn flags for all units and start over from the beginning
            foreach (var unit in units)
                unit.Unit.HasTakenTurn = false;
            currentUnitIndex = 0;
        }
        combatState = CombatState.TurnCheck;
        ManageTurns();
    }

    /// <summary>Delays the first turn by half a second.</summary>
    /// <returns>An IEnumerator that waits for half a second before incrementing the turn count and managing turns.</returns>
    private IEnumerator FirstTurnDelay()
    {
        yield return new WaitUntil(() => SceneTransitioner.currentlyTransitioning == false);
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
        switch (combatState)
        {
            case CombatState.PlayerTurn:
                TriggerAilment(PlayerUnitController, _playerAilments);
                break;
            case CombatState.EnemyTurn:
                TriggerAilment(EnemyUnitController, _enemyAilments);
                break;
        }
    }
    /// <summary>Triggers an ailment on the given target.</summary>
    /// <param name="target">The target to trigger the ailment on.</param>
    /// <remarks>
    /// If the target is on fire, it takes 1 damage and the player's combat HUD is updated. If the target is bleeding or incapacitated, it takes 1 damage and the player's combat HUD is updated. If the target is frozen or stunned, the player's combat HUD is updated.
    /// </remarks>
    private void TriggerAilment(UnitController target, Ailments targetAilments)
    {
        if (targetAilments.HasAilment(AilmentType.OnFire))
        {
            target.TakeRawDamage(Mathf.RoundToInt(target.Unit.MaxHp / 16));
            PlayerCombatHUD.UpdateCombatHUD();
            if (target.Unit.IsDead)
                target.Unit.HasTakenTurn = true;
            CheckGameOver();
        }
        if (targetAilments.HasAilment(AilmentType.Frozen))
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"{target.Unit.UnitName} está congelado!", 3f);
            target.TakeRawDamage(1);
            target.Unit.HasTakenTurn = true;
        }
        if (targetAilments.HasAilment(AilmentType.Stunned))
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"{target.Unit.UnitName} está atordoado!", 3f);
            target.Unit.HasTakenTurn = true;
        }
        if (targetAilments.HasAilment(AilmentType.Bleeding))
        {
            if (target.Unit.CurrentHp > Mathf.RoundToInt(target.Unit.MaxHp / 10))
                target.TakeRawDamage(Mathf.RoundToInt(target.Unit.MaxHp / 10));
            PlayerCombatHUD.UpdateCombatHUD();
        }
        if (targetAilments.HasAilment(AilmentType.Incapacitated))
        {
            // TODO explorar mais possibilidades
            target.TakeRawDamage(1);
            if (target.Unit.IsDead)
                target.Unit.HasTakenTurn = true;
            PlayerCombatHUD.UpdateCombatHUD();
            CheckGameOver();
        }

        targetAilments.DecrementTurnsLeft();
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
            combatState = CombatState.PlayerLost;
        }
        else if (!enemyAlive)
        {
            combatState = CombatState.PlayerWon;
        }
    }
    /// <summary>Loads the "scn_gameOver" scene, indicating that the game is over.</summary>
    private void GameOver()
    {
        sceneTransitioner.StartCoroutine(sceneTransitioner.TransitionTo("scn_gameOver"));
        onDeath.Invoke();
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
        EnemyUnitController.KillUnit();
        yield return new WaitUntil(() => EnemyUnitController.gameObject.activeSelf == false);
        yield return new WaitUntil(() => itemNotification.ItemQueue.Count == 0 && !itemNotification.IsDisplaying);
        yield return new WaitForSeconds(sceneChangeDelay);
        var lastScene = QuickSaveReader.Create("GameInfo").Read<string>("LastScene");
        sceneTransitioner.StartCoroutine(sceneTransitioner.TransitionTo(lastScene));
    }
    /// <summary>Calculates and applies the experience reward for defeating an enemy unit.</summary>
    /// <remarks>
    /// If the player's unit will not level up after receiving the experience reward, the experience is simply added to the unit's total.
    /// If the player's unit will level up after receiving the experience reward, the unit's experience is increased by the reward and then checked for level up.
    /// If the player's unit will level up and have excess experience after receiving the reward, the unit's experience is increased by the reward, checked for level up, and then the excess experience is added.
    /// </remarks>
    private void XpReward()
    {
        PlayerCombatHUD.UpdateExperience.Invoke();
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
        if (combatState == CombatState.PlayerTurn)
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
        combatState = CombatState.TurnEnd;
        onTurnChange.Invoke();
    }

    #endregion
}
