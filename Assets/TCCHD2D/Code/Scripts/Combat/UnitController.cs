using CI.QuickSave;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Random = UnityEngine.Random;

/// <summary>
/// Controls the behaviour of a unit.
/// </summary>
/// <remarks>
/// This class controls the behaviour of a unit. It contains the unit data, the playable director that controls the animations of the unit, and the playable assets that represent the unit's actions.
/// </remarks>
public class UnitController : MonoBehaviour
{
    #region === Variables ===============================================================

    [BoxGroup("Unit Info")]
    [SerializeField, InlineEditor, Tooltip("The unit data that this controller controls.")]
    private Unit unit;

    [FoldoutGroup("Action Timelines")]
    [SerializeField, Tooltip("The playable director that controls the animations of this unit.")]
    private PlayableDirector director;

    [FoldoutGroup("Action Timelines")]
    [SerializeField, Tooltip("The PlayableAsset representing the unit's basic attack.")]
    private TimelineAsset basicAttack;

    [FoldoutGroup("Action Timelines")]
    [SerializeField, Tooltip("The PlayableAsset representing the unit's basic attack.")]
    private TimelineAsset basicCutAttack;

    [FoldoutGroup("Action Timelines")]
    [SerializeField, Tooltip("The PlayableAsset representing the unit's basic attack.")]
    private TimelineAsset basicBluntAttack;

    [FoldoutGroup("Action Timelines")]
    [SerializeField, Tooltip("The PlayableAsset representing the unit's basic attack.")]
    private TimelineAsset basicRangedAttack;

    [FoldoutGroup("Action Timelines")]
    [SerializeField, Tooltip("The PlayableAsset representing the unit's use item action.")]
    private TimelineAsset useItem;

    [FoldoutGroup("Action Timelines")]
    [SerializeField, Tooltip("The PlayableAsset representing the unit's run action.")]
    private TimelineAsset run;

    [FoldoutGroup("Unit Floating Numbers")]
    [SerializeField, Tooltip("The animator that controls the damage text animation.")]
    private Animator damageTextAnimator;

    [FoldoutGroup("Unit Floating Numbers")]
    [SerializeField, Tooltip("The text that displays the damage taken by the unit.")]
    private TMP_Text damageText;

    public int damageTakenThisTurn;
    public int attackDamageCalculated;
    public int defenceCalculated;
    public int speedCalculated;
    public int _charges;

    #endregion

    #region === Properties ==============================================================

    public Unit Unit
    {
        get => unit;
        set => unit = value;
    }
    public PlayableDirector Director => director;
    public TimelineAsset BasicAttack => basicAttack;
    public TimelineAsset BasicCutAttack => basicCutAttack;
    public TimelineAsset BasicBluntAttack => basicBluntAttack;
    public TimelineAsset BasicRangedAttack => basicRangedAttack;
    public TimelineAsset UseItem => useItem;
    public TimelineAsset Run => run;
    public int Charges
    {
        get => _charges;
        set => _charges = value;
    }

    #endregion

    #region === Unity Methods ===========================================================

    /// <summary>Initializes the unit's state when it wakes up.</summary>
    /// <remarks>
    /// If the unit is not a player, it sets its state to alive with full health and the basic attack animation.
    /// If the unit is a player, it checks its current health and death status and updates them accordingly.
    /// </remarks>
    public void Awake()
    {
        if (!unit.IsPlayer)
        {
            unit.IsDead = false;
            unit.CurrentHp = unit.MaxHp;
            basicAttack = unit.AttackAnimation;
        }
        else
        {
            if (unit.CurrentHp == unit.MaxHp && unit.IsDead)
                unit.IsDead = false;
            if (!unit.IsDead && unit.CurrentHp == 0)
                unit.CurrentHp = unit.MaxHp;
            if (unit.IsDead && unit.CurrentHp == 0)
            {
                unit.CurrentHp = unit.MaxHp;
                unit.IsDead = false;
            }
        }
    }
    /// <summary>Updates the current health and TP of a unit if they exceed their maximum values.</summary>
    /// <remarks>
    /// If the unit is a player and their current TP exceeds their maximum TP, their current TP is set to their maximum TP.
    /// If the unit is a player and their current health exceeds their maximum health, their current health is set to their maximum health.
    /// If the unit is not a player and their current health exceeds their maximum health, their current health is set to their maximum health.
    /// </remarks>
    private void Update()
    {
        if (unit.IsPlayer && unit.CurrentTp > unit.MaxTp)
            unit.CurrentTp = unit.MaxTp;
        if (unit.IsPlayer && unit.CurrentHp > unit.MaxHp)
            unit.CurrentHp = unit.MaxHp;
        if (!unit.IsPlayer && unit.CurrentHp > unit.MaxHp)
            unit.CurrentHp = unit.MaxHp;
    }

    #endregion

    #region === Methods =================================================================

    /// <summary>Performs an attack action on a target unit.</summary>
    /// <param name="target">The unit controller representing the target of the attack.</param>
    /// <remarks>Updates the combat HUD to reflect the attack action.</remarks>
    public void AttackAction(UnitController target)
    {
        AttackLogic(target);
        PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
        PlayerCombatHUD.CombatTextEvent.Invoke($"<b>Atacou <color=blue>{target.Unit.UnitName}</color> causando <color=red>{target.damageTakenThisTurn}</color> de dano</b>");
        PlayerCombatHUD.TakenAction.Invoke();
    }
    /// <summary>Executes the attack logic on a target unit.</summary>
    /// <param name="target">The target unit to attack.</param>
    /// <remarks>
    /// This method calculates the attack damage based on the unit's attack stat and any equipped weapon.
    /// If the unit has charges, the attack damage is increased by the number of charges and the charges are reset to 0.
    /// If the unit is a player, it sets up the animation and signal bindings for the basic attack.
    /// If the player has a weapon equipped, it plays the appropriate attack animation based on the weapon's attack type.
    /// If the player's TP is less than the maximum TP, it increases the TP by 10 and updates the player's TP
    private void AttackLogic(UnitController target)
    {
        attackDamageCalculated = unit.Attack;
        if (unit.IsPlayer && InventoryManager.Instance.EquipmentSlots[3].equipItem != null)
            attackDamageCalculated += InventoryManager.Instance.EquipmentSlots[3].equipItem.StatusValue;
        if (_charges > 0)
        {
            _charges++;
            attackDamageCalculated += _charges;
            _charges -= _charges;
        }
        if (unit.IsPlayer)
        {
            var enemyObject = GameObject.FindWithTag("Enemy");
            foreach (var track in basicAttack.GetOutputTracks())
            {
                switch (track.name)
                {
                    case "AttackAnimation":
                        director.SetGenericBinding(track, gameObject.GetComponentInChildren<Animator>());
                        break;
                    case "MovementAnimation":
                        director.SetGenericBinding(track, gameObject.GetComponentInChildren<Animator>());
                        break;
                    case "Signals":
                        director.SetGenericBinding(track, enemyObject.GetComponentInChildren<SignalReceiver>());
                        break;
                }
            }
        }
        if (unit.IsPlayer && InventoryManager.Instance.EquipmentSlots[3].equipItem != null)
            switch (InventoryManager.Instance.EquipmentSlots[3].equipItem.AttackType)
            {
                case AttackType.Blunt:
                    Director.Play(basicBluntAttack);
                    break;
                case AttackType.Cut:
                    Director.Play(basicCutAttack);
                    break;
                case AttackType.Ranged:
                    Director.Play(basicRangedAttack);
                    break;
                default:
                    Director.Play(basicAttack);
                    break;
            }
        else
            Director.Play(basicAttack);

        if (unit.IsPlayer && unit.CurrentTp < unit.MaxTp)
        {
            unit.CurrentTp += 10;
            if (unit.CurrentTp > unit.MaxTp)
                unit.CurrentTp = unit.MaxTp;
            PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
        }
        target.TakeDamage(attackDamageCalculated);
    }
    /// <summary>Reduces the unit's health by the given amount of damage, taking into account the unit's defense and equipment.</summary>
    /// <param name="damage">The amount of damage to be taken.</param>
    /// <returns>The amount of damage taken after defense and equipment have been taken into account.</returns>
    public int TakeDamage(int damage)
    {
        defenceCalculated = unit.Defence;
        if (unit.IsPlayer && InventoryManager.Instance.EquipmentSlots[0].equipItem != null)
            defenceCalculated += InventoryManager.Instance.EquipmentSlots[0].equipItem.StatusValue;
        if (unit.IsPlayer && InventoryManager.Instance.EquipmentSlots[1].equipItem != null)
            defenceCalculated += InventoryManager.Instance.EquipmentSlots[1].equipItem.StatusValue;
        // Calculate damage taken based on defense
        damageTakenThisTurn = Mathf.Max(1, damage - defenceCalculated);
        
        if (damage == 0)
            damageTakenThisTurn = 0;

        // Subtract damage from health
        unit.CurrentHp -= damageTakenThisTurn;

        if (unit.IsPlayer)
        {
            unit.CurrentTp += 5;
            if (unit.CurrentTp > unit.MaxTp)
                unit.CurrentTp = unit.MaxTp;
            PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
        }
        // Check if the unit has died
        if (unit.CurrentHp <= 0)
        {
            unit.IsDead = true;
            unit.CurrentHp = 0;
            // TODO: Play death animation
        }
        return damageTakenThisTurn;
    }
    /// <summary>Displays the damage taken by a unit as text.</summary>
    /// <remarks>
    /// The damage taken is displayed using an animator that triggers a specific animation depending on whether the unit is a player or an enemy.
    /// </remarks>
    public void DisplayDamageText()
    {
        damageText.text = damageTakenThisTurn.ToString();
        damageTextAnimator.SetTrigger(unit.IsPlayer ? "PlayerTookDamage" : "EnemyTookDamage");
    }
    /// <summary>Runs the logic for a player's escape attempt and updates the game state accordingly.</summary>
    /// <remarks>
    /// If the player successfully escapes, the game will load the last scene visited and update the game save file with the name of the current scene.
    /// If the player fails to escape, the game will display a message indicating the failure.
    /// </remarks>
    public void RunAction()
    {
        var gotAway = RunLogic();

        if (gotAway)
        {
            var reader = QuickSaveReader.Create("GameInfo");
            SceneManager.LoadScene(reader.Read<string>("LastScene"));
            PlayerCombatHUD.CombatTextEvent.Invoke($"Você <color=green>fugiu com sucesso</color>");
            PlayerCombatHUD.TakenAction.Invoke();
        }
        else
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"Você <color=red>falhou em fugir</color>");
            PlayerCombatHUD.TakenAction.Invoke();
        }
    }
    /// <summary>Runs a logic that involves a random chance and a unit's luck.</summary>
    /// <returns>True if the random chance plus the unit's luck is greater than 50, false otherwise.</returns>
    public bool RunLogic()
    {
        var randomChance = Random.Range(0, 100);
        randomChance += Unit.Luck + Unit.Dexterity;
        randomChance = randomChance > 50 ? 1 : 0;
        if (randomChance == 1)
        {
            //TODO: play run animation
            return true;
        }
        return false;
    }
    /// <summary>Selects an action for the unit to perform on the target.</summary>
    /// <param name="target">The target unit.</param>
    /// <remarks>If the unit is a player, no action is taken. Otherwise, the unit attacks the target and displays the damage dealt in the player combat HUD.</remarks>
    public void SelectAction(UnitController target)
    {
        if (unit.IsPlayer) return;
        // AI logic for selecting an action goes here
        AttackLogic(target);
        PlayerCombatHUD.CombatTextEvent.Invoke($"<color=blue>{unit.UnitName}</color> atacou <color=red>{target.Unit.UnitName}</color> causando <color=red>{target.damageTakenThisTurn}</color> de dano!");

        // IF ENEMY CAN RUN AWAY
        // else
        // {
        //     var enemyRan = RunLogic();
        //     if (enemyRan)
        //     {
        //         //TODO: give player exp reward
        //         //TODO: play run animation
        //         //TODO: change scenes
        //         PlayerCombatHUD.CombatTextEvent.Invoke(
        //             $"<color=blue>{unit.UnitName}</color> ran away!");
        //     }
        //     else
        //     {
        //         //Enemy lost a turn
        //         PlayerCombatHUD.CombatTextEvent.Invoke(
        //             $"<color=blue>{unit.UnitName}</color> tried to run away but <color=red>failed</color>");
        //     }
        // }
        
    }
    // tutorial
    public void RunActionTutorial()
    {
        var reader = QuickSaveReader.Create("GameInfo");
        SceneManager.LoadScene(reader.Read<string>("LastScene"));
        PlayerCombatHUD.CombatTextEvent.Invoke($"Você terminou seu treino");
        PlayerCombatHUD.TakenAction.Invoke();
        unit.Experience = 1;
    }

    #endregion
}
