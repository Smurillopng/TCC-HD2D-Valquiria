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
    [TitleGroup("Unit Info", Alignment = TitleAlignments.Centered)]
    [SerializeField, InlineEditor]
    private Unit unit;
    
    [TitleGroup("Action Timelines", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private PlayableDirector director;
    
    [SerializeField]
    private PlayableAsset basicAttack;
    
    [SerializeField]
    private List<PlayableAsset> specialAttacks = new();
    
    [SerializeField]
    private PlayableAsset useItem;
    
    [SerializeField]
    private PlayableAsset run;
    
    [TitleGroup("Unit Floating Numbers", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private Animator damageTextAnimator;
    
    [SerializeField]
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

    public void Start()
    {
        // Set the current HP to the maximum
        if (unit.IsPlayer == false)
        {
            unit.IsDead = false;
            unit.CurrentHp = unit.MaxHp;
        }
        else
        {
            if (unit.CurrentHp == unit.MaxHp && unit.IsDead == true)
                unit.IsDead = false;
            if (unit.IsDead == false && unit.CurrentHp == 0)
                unit.CurrentHp = unit.MaxHp;
            if (unit.IsDead == true && unit.CurrentHp == 0)
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
        var heal = unit.Attack;

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
        if (unit.IsPlayer == false)
        {
            //
        }
    }

    /// <summary>
    /// The logic for the player running away.
    /// </summary>
    public void RunAction()
    {
        // AI logic for running away goes here
        if (unit.IsPlayer == false)
        {
            //
        }
    }

    /// <summary>
    /// The AI logic for selecting an action.
    /// </summary>
    /// <param name="target"></param>
    public void SelectAction(UnitController target)
    {
        // AI logic for selecting an action goes here
        if (unit.IsPlayer == false)
        {
            Director.Play(basicAttack);
            AttackAction(target);
        }
    }
}
