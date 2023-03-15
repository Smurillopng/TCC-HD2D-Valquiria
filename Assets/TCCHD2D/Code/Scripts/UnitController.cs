// Created by Sérgio Murillo da Costa Faria
// Date: 13/03/2023

using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class UnitController : MonoBehaviour
{
    //Stats
    [SerializeField, InlineEditor]
    private Unit unit;
    //Actions
    [SerializeField]
    private PlayableDirector unitDirector;
    [SerializeField]
    private PlayableAsset basicAttack;
    [SerializeField]
    private List<PlayableAsset> specialAttacks = new();
    [SerializeField]
    private PlayableAsset useItem;
    [SerializeField]
    private PlayableAsset run;
    [SerializeField]
    private Animator unitDamageTextAnimator;
    [SerializeField]
    private TMP_Text unitDamageText;

    private int damageTaken;

    public Unit Unit => unit;
    public PlayableDirector UnitDirector => unitDirector;
    public PlayableAsset BasicAttack => basicAttack;
    public List<PlayableAsset> SpecialAttacks => specialAttacks;
    public PlayableAsset UseItem => useItem;
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

    public void AttackAction(UnitController target)
    {
        // Calculate damage based on strength
        var damage = unit.Attack;

        // Apply damage to target
        target.TakeDamage(damage);
    }

    public void TakeDamage(int damage)
    {
        // Calculate damage taken based on defense
        damageTaken = Mathf.Max(1, damage - unit.Defence);

        // Subtract damage from health
        unit.CurrentHp -= damageTaken;

        // Check if the unit has died
        if (unit.CurrentHp <= 0)
        {
            unit.IsDead = true;
            unit.CurrentHp = 0;
            // TODO: Play death animation
            // TODO: End the combat
        }
    }

    public void HealSpecialAction(UnitController target)
    {
        // Calculate heal based on strength
        var heal = unit.Attack;

        // Apply heal to target
        target.Unit.CurrentHp += heal;
    }

    public void DisplayDamageText()
    {
        unitDamageText.text = damageTaken.ToString();
        unitDamageTextAnimator.SetTrigger(unit.IsPlayer ? "PlayerTookDamage" : "EnemyTookDamage");
    }

    public void SpecialAction(int index, UnitController target)
    {
        var damage = unit.Attack;

        // Apply damage to target
        target.TakeDamage(damage);
    }

    public void UseItemAction()
    {
        // AI logic for using an item goes here
        if (unit.IsPlayer == false)
        {
            //
        }
    }

    public void RunAction()
    {
        // AI logic for running away goes here
        if (unit.IsPlayer == false)
        {
            //
        }
    }

    public void SelectAction(UnitController target)
    {
        // AI logic for selecting an action goes here
        if (unit.IsPlayer == false)
            AttackAction(target);
        var combatHUD = GameObject.FindWithTag("CombatUI").GetComponent<PlayerCombatHUD>();
        combatHUD.UpdatePlayerHealth();
    }
}
