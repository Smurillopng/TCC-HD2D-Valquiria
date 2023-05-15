// Created by Sérgio Murillo da Costa Faria
// Date: 13/03/2023

using System;
using System.Collections.Generic;
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
    private TimelineAsset basicAttack;

    [FoldoutGroup("Action Timelines")]
    [SerializeField, Tooltip("The list of PlayableAssets representing the unit's special attacks.")]
    private List<PlayableAsset> specialAttacks = new();

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
    public TimelineAsset BasicAttack => basicAttack;

    /// <summary>
    /// The <see cref="PlayableAsset"/> representing the unit's special attacks.
    /// </summary>
    public List<PlayableAsset> SpecialAttacks => specialAttacks;

    /// <summary>
    /// The <see cref="PlayableAsset"/> representing the unit's use item action.
    /// </summary>
    public TimelineAsset UseItem => useItem;

    /// <summary>
    /// The <see cref="PlayableAsset"/> representing the unit's run action.
    /// </summary>
    public TimelineAsset Run => run;
    public int Charges
    {
        get => _charges;
        set => _charges = value;
    }

    public void Awake()
    {
        // Set the current HP to the maximum
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

    private void Update()
    {
        if (unit.IsPlayer && unit.CurrentTp > unit.MaxTp)
            unit.CurrentTp = unit.MaxTp;
        if (unit.IsPlayer && unit.CurrentHp > unit.MaxHp)
            unit.CurrentHp = unit.MaxHp;
        if (!unit.IsPlayer && unit.CurrentHp > unit.MaxHp)
            unit.CurrentHp = unit.MaxHp;
    }

    /// <summary>
    /// Adds damage text to the combat text box and calls the player unit attack method [<see cref="AttackLogic"/>].
    /// </summary>
    public void AttackAction(UnitController target)
    {
        AttackLogic(target);
        PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
        PlayerCombatHUD.CombatTextEvent.Invoke($"<b>Atacou <color=blue>{target.Unit.UnitName}</color> causando <color=red>{target.damageTakenThisTurn}</color> de dano</b>");
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
        Director.Play(basicAttack);

        if (unit.IsPlayer && unit.CurrentTp < unit.MaxTp)
        {
            unit.CurrentTp += 10;
            if (unit.CurrentTp > unit.MaxTp)
                unit.CurrentTp = unit.MaxTp;
            PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
        }

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
            PlayerCombatHUD.CombatTextEvent.Invoke("<color=red>TP insuficiente</color>");
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
            var reader = QuickSaveReader.Create("GameSave");
            SceneManager.LoadScene(reader.Read<string>("LastScene"));
            var save = QuickSaveWriter.Create("GameSave");
            save.Write("LastScene", SceneManager.GetActiveScene().name);
            save.Commit();
            PlayerCombatHUD.CombatTextEvent.Invoke($"Você <color=green>fugiu com sucesso</color>");
            PlayerCombatHUD.TakenAction.Invoke();
        }
        else
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"Você <color=red>falhou em fugir</color>");
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
}
