// Created by Sérgio Murillo da Costa Faria
// Date: 13/03/2023

using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the behaviour of a unit.
/// </summary>
public class UnitController : MonoBehaviour
{
    [BoxGroup("Unit Info")]
    [SerializeField, InlineEditor, Tooltip("The unit data that this controller controls.")]
    private Unit unit;

    [FoldoutGroup("Action Timelines")]
    [SerializeField, Tooltip("The playable director that controls the animations of this unit.")]
    private PlayableDirector director;

    [FoldoutGroup("Action Timelines")]
    [SerializeField, Tooltip("The PlayableAsset representing the unit's basic attack.")]
    private PlayableAsset basicAttack;

    [FoldoutGroup("Action Timelines")]
    [SerializeField, Tooltip("The list of PlayableAssets representing the unit's special attacks.")]
    private List<PlayableAsset> specialAttacks = new();

    [FoldoutGroup("Action Timelines")]
    [SerializeField, Tooltip("The PlayableAsset representing the unit's use item action.")]
    private PlayableAsset useItem;

    [FoldoutGroup("Action Timelines")]
    [SerializeField, Tooltip("The PlayableAsset representing the unit's run action.")]
    private PlayableAsset run;

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

    /// <summary>
    /// The <see cref="Unit"/> that this controller controls.
    /// </summary>
    public Unit Unit
    {
        get => unit;
        set => unit = value;
    }

    /// <summary>
    /// The <see cref="PlayableDirector"/> that controls the animations of this unit.
    /// </summary>
    public PlayableDirector Director => director;

    /// <summary>
    /// The <see cref="PlayableAsset"/> representing the unit's basic attack.
    /// </summary>
    public PlayableAsset BasicAttack => basicAttack;

    /// <summary>
    /// The <see cref="PlayableAsset"/> representing the unit's special attacks.
    /// </summary>
    public List<PlayableAsset> SpecialAttacks => specialAttacks;

    /// <summary>
    /// The <see cref="PlayableAsset"/> representing the unit's use item action.
    /// </summary>
    public PlayableAsset UseItem => useItem;

    /// <summary>
    /// The <see cref="PlayableAsset"/> representing the unit's run action.
    /// </summary>
    public PlayableAsset Run => run;

    public void Awake()
    {
        // Set the current HP to the maximum
        if (!unit.IsPlayer)
        {
            unit.IsDead = false;
            unit.CurrentHp = unit.MaxHp;
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
    
    /// <summary>
    /// Adds damage text to the combat text box and calls the player unit attack method [<see cref="AttackLogic"/>].
    /// </summary>
    public void AttackAction(UnitController target)
    {
        AttackLogic(target);
        PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
        PlayerCombatHUD.CombatTextEvent.Invoke($"<b>Attacked <color=blue>{target.Unit.UnitName}</color> for <color=red>{target.damageTakenThisTurn}</color> damage</b>");
        PlayerCombatHUD.TakenAction.Invoke();
    }

    /// <summary>
    /// The attack action.
    /// </summary>
    /// <param name="target"></param>
    private void AttackLogic(UnitController target)
    {
        // Calculate damage based on strength
        attackDamageCalculated = unit.Attack;
        if (unit.IsPlayer && InventoryManager.Instance.EquipmentSlots[3].equipItem != null)
            attackDamageCalculated += InventoryManager.Instance.EquipmentSlots[3].equipItem.StatusValue;
        Director.Play(basicAttack);

        if (unit.IsPlayer && unit.CurrentTp < unit.MaxTp)
            unit.CurrentTp += 10;

        // Apply damage to target
        target.TakeDamage(attackDamageCalculated);
    }

    /// <summary>
    /// Responsible for handling the damage taken by the unit.
    /// </summary>
    /// <param name="damage"></param>
    public int TakeDamage(int damage)
    {
        defenceCalculated = unit.Defence;
        if (unit.IsPlayer && InventoryManager.Instance.EquipmentSlots[0].equipItem != null)
            defenceCalculated += InventoryManager.Instance.EquipmentSlots[0].equipItem.StatusValue;
        if (unit.IsPlayer && InventoryManager.Instance.EquipmentSlots[1].equipItem != null)
            defenceCalculated += InventoryManager.Instance.EquipmentSlots[1].equipItem.StatusValue;
        // Calculate damage taken based on defense
        damageTakenThisTurn = Mathf.Max(1, damage - defenceCalculated);

        // Subtract damage from health
        unit.CurrentHp -= damageTakenThisTurn;

        if (unit.IsPlayer)
        {
            unit.CurrentTp += 10;
            PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
        }

        // Check if the unit has died
        if (unit.CurrentHp <= 0)
        {
            unit.IsDead = true;
            unit.CurrentHp = 0;
            // TODO: Play death animation
            // TODO: End the combat
        }

        return damageTakenThisTurn;
    }
    
    /// <summary>
    /// Adds information in the combat text box and calls the player specific special attack method.
    /// </summary>
    public void Special()
    {
        if (Unit.CurrentTp < Unit.MaxTp / 2)
        {
            PlayerCombatHUD.CombatTextEvent.Invoke("<color=red>Not enough TP</color>");
        }
    }

    /// <summary>
    /// Responsible for displaying the damage text animation.
    /// </summary>
    public void DisplayDamageText()
    {
        damageText.text = damageTakenThisTurn.ToString();
        damageTextAnimator.SetTrigger(unit.IsPlayer ? "PlayerTookDamage" : "EnemyTookDamage");
    }

    /// <summary>
    /// The action for the player to open the special attack menu and select an action.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="target"></param>
    public void SpecialAction(int index, UnitController target)
    {
        // AI logic for using an item goes here
        if (!unit.IsPlayer)
        {
            //TODO: AI Logic
        }

        var damage = unit.Attack;

        // Apply damage to target
        target.TakeDamage(damage);
    }

    /// <summary>
    /// The logic for the player using an item.
    /// </summary>
    public void UseItemAction()
    {
        // AI logic for using an item goes here
        if (!unit.IsPlayer)
        {
            //TODO: AI Logic
        }
    }
    
    /// <summary>
    /// Adds information in the combat text box and calls the player run method [<see cref="RunLogic"/>].
    /// </summary>
    public void RunAction()
    {
        var gotAway = RunLogic();

        if (gotAway)
        {
            SceneManager.LoadScene("scn_game");
            PlayerCombatHUD.CombatTextEvent.Invoke($"<color=green>Ran away</color>");
            PlayerCombatHUD.TakenAction.Invoke();
        }
        else
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"<color=red>Failed to run away</color>");
            PlayerCombatHUD.TakenAction.Invoke();
        }
    }

    /// <summary>
    /// The logic for the player running away.
    /// </summary>
    public bool RunLogic()
    {
        var randomChance = Random.Range(0, 100);
        randomChance += Unit.Luck;
        randomChance = randomChance > 50 ? 1 : 0;
        if (randomChance == 1)
        {
            //TODO: play run animation
            return true;
        }
        return false;
    }

    /// <summary>
    /// The AI logic for selecting an action.
    /// </summary>
    /// <param name="target"></param>
    public void SelectAction(UnitController target)
    {

        if (unit.IsPlayer) return;
        // AI logic for selecting an action goes here
        AttackLogic(target);
        PlayerCombatHUD.CombatTextEvent.Invoke($"<color=blue>{unit.UnitName}</color> attacked <color=red>{target.Unit.UnitName}</color> for <color=red>{target.damageTakenThisTurn}</color> damage!");

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
}
