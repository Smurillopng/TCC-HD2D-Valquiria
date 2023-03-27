// Created by Sérgio Murillo da Costa Faria
// Date: 13/03/2023

using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

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

    private int _damageTakenThisTurn;

    /// <summary>
    /// The <see cref="Unit"/> that this controller controls.
    /// </summary>
    public Unit Unit => unit;

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
    /// The attack action.
    /// </summary>
    /// <param name="target"></param>
    public void AttackAction(UnitController target)
    {
        // Calculate damage based on strength
        var damage = unit.Attack;
        Director.Play(basicAttack);
        
        if (unit.IsPlayer && unit.CurrentTp < unit.MaxTp)
            unit.CurrentTp += 10;

        // Apply damage to target
        target.TakeDamage(damage);
    }

    /// <summary>
    /// Responsible for handling the damage taken by the unit.
    /// </summary>
    /// <param name="damage"></param>
    public void TakeDamage(int damage)
    {
        // Calculate damage taken based on defense
        _damageTakenThisTurn = Mathf.Max(1, damage - unit.Defence);

        // Subtract damage from health
        unit.CurrentHp -= _damageTakenThisTurn;
        
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
    }

    /// <summary>
    /// An example of a special action.
    /// </summary>
    /// <param name="target"></param>
    public void HealSpecialAction(UnitController target)
    {
        // Calculate heal based on strength
        var heal = unit.Luck * 2;

        // Apply heal to target
        target.Unit.CurrentHp += heal;
    }

    /// <summary>
    /// Responsible for displaying the damage text animation.
    /// </summary>
    public void DisplayDamageText()
    {
        damageText.text = _damageTakenThisTurn.ToString();
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
    /// The logic for the player running away.
    /// </summary>
    public bool RunAction()
    {
        if (!unit.IsPlayer)
        {
            //TODO: AI Logic
        }
        
        var randomChance = Random.Range(0, 100);
        randomChance += Unit.Luck;
        randomChance = randomChance > 50 ? 1 : 0;
        if (randomChance == 1)
        {
            //TODO: play run animation
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// The AI logic for selecting an action.
    /// </summary>
    /// <param name="target"></param>
    public void SelectAction(UnitController target)
    {
        
        if (unit.IsPlayer) return;
        // AI logic for selecting an action goes here
        if (unit.CurrentHp > unit.MaxHp/4)
        {
            AttackAction(target);
            PlayerCombatHUD.CombatTextEvent.Invoke(
                $"<color=blue>{unit.UnitName}</color> attacked <color=red>{target.Unit.UnitName}</color> for <color=red>{_damageTakenThisTurn}</color> damage!");
        }
        else if (unit.CurrentHp < unit.MaxHp / 2)
        {
            HealSpecialAction(this);
            PlayerCombatHUD.CombatTextEvent.Invoke(
                $"<color=blue>{unit.UnitName}</color> healed for <color=green>{unit.Luck * 2}</color>!");
            PlayerCombatHUD.UpdateCombatHUDEnemyHp.Invoke();
        }
        else
        {
            var enemyRan = RunAction();
            if (enemyRan)
            {
                //TODO: give player exp reward
                //TODO: play run animation
                //TODO: change scenes
                PlayerCombatHUD.CombatTextEvent.Invoke(
                    $"<color=blue>{unit.UnitName}</color> ran away!");
            }
            else
            {
                //Enemy lost a turn
                PlayerCombatHUD.CombatTextEvent.Invoke(
                    $"<color=blue>{unit.UnitName}</color> tried to run away but <color=red>failed</color>");
            }
        }
    }
}
