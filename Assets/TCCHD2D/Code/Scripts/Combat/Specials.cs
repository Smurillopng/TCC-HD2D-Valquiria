using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

/// <summary>
/// This class represents a special action.
/// </summary>
/// <remarks>
/// It contains the special action's name, description, type, cost, heal, damage, and debuff.
/// </remarks>
public class Specials : MonoBehaviour
{
    #region === Variables ===============================================================

    public List<Special> specialsList = new();
    private PlayerCombatHUD _combatHUD;
    private TurnManager _turnManager;
    private UnitController _player;
    private UnitController _enemy;
    private Ailments _enemyAilments;

    #endregion

    #region === Unity Methods ===========================================================

    /// <summary>Initializes the Start method.</summary>
    /// <remarks>This method finds the PlayerCombatHUD and TurnManager objects in the scene.</remarks>
    private void Start()
    {
        _combatHUD = FindObjectOfType<PlayerCombatHUD>();
        _turnManager = FindObjectOfType<TurnManager>();
        _player = GameObject.FindWithTag("Player").GetComponent<UnitController>();
        _enemy = GameObject.FindWithTag("Enemy").GetComponent<UnitController>();
        _enemyAilments = _enemy.gameObject.GetComponent<Ailments>();
    }

    #endregion

    #region === Methods =================================================================

    /// <summary>Uses a special action based on its type.</summary>
    /// <param name="specialAction">The special action to use.</param>
    /// <exception cref="ArgumentException">Thrown when the special action type is not recognized.</exception>
    public void UseSpecial(Special specialAction)
    {
        switch (specialAction.specialType)
        {
            case SpecialType.Heal:
                HealSpecialAction(specialAction.specialCost, specialAction.specialHeal);
                break;
            case SpecialType.Damage:
                HeavyHitSpecialAction(specialAction.specialCost, specialAction.specialDamage);
                break;
            case SpecialType.Debuff:
                switch (specialAction.specialAilment)
                {
                    case AilmentType.OnFire:
                        SpecialAction(specialAction.specialCost, specialAction, AilmentType.OnFire);
                        break;
                    case AilmentType.Stunned:
                        SpecialAction(specialAction.specialCost, specialAction, AilmentType.Stunned);
                        break;
                    case AilmentType.Frozen:
                        SpecialAction(specialAction.specialCost, specialAction, AilmentType.Frozen);
                        break;
                    case AilmentType.Bleeding:
                        SpecialAction(specialAction.specialCost, specialAction, AilmentType.Bleeding);
                        break;
                    case AilmentType.Incapacitated:
                        SpecialAction(specialAction.specialCost, specialAction, AilmentType.Incapacitated);
                        break;
                }
                break;
        }
    }

    private void InsufficientTP(int cost)
    {
        PlayerCombatHUD.CombatTextEvent.Invoke($"<color=red>TP Insuficiente</color>\n" +
                                                       $"Quantidade necessária: <color=red>{cost} TP</color>", 2f);
    }
    private void SetBindings(TimelineAsset timeline)
    {
        if (_player.Unit.IsPlayer)
        {
            var enemyObject = GameObject.FindWithTag("Enemy");
            foreach (var track in timeline.GetOutputTracks())
            {
                switch (track.name)
                {
                    case "AttackAnimation":
                        _player.Director.SetGenericBinding(track, _player.gameObject.GetComponentInChildren<Animator>());
                        break;
                    case "MovementAnimation":
                        _player.Director.SetGenericBinding(track, _player.gameObject.GetComponentInChildren<Animator>());
                        break;
                    case "Signals":
                        _player.Director.SetGenericBinding(track, enemyObject.GetComponentInChildren<SignalReceiver>());
                        break;
                    case "Healing":
                        _player.Director.SetGenericBinding(track, _player.gameObject.GetComponentInChildren<SignalReceiver>());
                        break;
                }
            }
        }
    }
    private void ExecuteLogic(AilmentType ailment, int cost, Special specialAction, Ailments enemyAilments, int attackDamageCalculated)
    {
        // Animation
        _player.Director.Play(_player.BasicAttack);

        // HUD Update
        _player.Unit.CurrentTp -= cost;

        // Damage multiplied
        _enemy.TakeDamage(attackDamageCalculated);
        _enemyAilments.SetAilment(ailment, true, specialAction.turnsToLast);

        PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
        PlayerCombatHUD.TakenAction.Invoke();
        PlayerCombatHUD.ForceDisableButtons.Invoke(true);
    }
    private void ExecuteLogic(int cost, int attackDamageCalculated)
    {
        // Animation
        _player.Director.Play(_player.BasicAttack);

        // HUD Update
        _player.Unit.CurrentTp -= cost;

        // Damage multiplied
        _enemy.TakeDamage(attackDamageCalculated);

        PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
        PlayerCombatHUD.TakenAction.Invoke();
        PlayerCombatHUD.ForceDisableButtons.Invoke(true);
    }
    private void UpdateHUD()
    {
        _turnManager.isPlayerTurn = false;
        _combatHUD.SpecialPanel.SetActive(false);
        _combatHUD.ReturnButton.gameObject.SetActive(false);
        _combatHUD.ReturnButton.interactable = false;
        _combatHUD.OptionsPanel.SetActive(true);
        PlayerCombatHUD.UpdateCombatHUD.Invoke();
    }
    private void SpecialAction(int cost, Special specialAction, AilmentType ailmentType)
    {
        if (_player.Unit.CurrentTp >= cost)
        {
            UpdateHUD();
            // Standard attack damage calculation
            var attackDamageCalculated = _player.Unit.Attack;
            if (InventoryManager.Instance.EquipmentSlots[3].equipItem != null)
                attackDamageCalculated += InventoryManager.Instance.EquipmentSlots[3].equipItem.StatusValue;
            SetBindings(_player.BasicAttack);
            ExecuteLogic(ailmentType, cost, specialAction, _enemyAilments, attackDamageCalculated);
        }
        else
        {
            InsufficientTP(cost);
        }
    }

    /// <summary>Performs a special healing action.</summary>
    /// <param name="cost">The cost of the action in TP.</param>
    /// <param name="healAmount">The amount of HP to heal.</param>
    /// <remarks>
    /// This method retrieves the player object and checks if the player has enough TP to perform the action.
    /// If the player has enough TP, the method sets the UI elements and updates the player's HP and TP accordingly.
    /// The method also plays an animation and displays a combat text message.
    /// </remarks>
    private void HealSpecialAction(int cost, int healAmount)
    {
        if (_player.Unit.CurrentTp >= cost)
        {
            UpdateHUD();

            // Calculate heal based on luck
            var heal = _player.Unit.Luck + healAmount;

            // Apply heal to target
            if (_player.Unit.CurrentHp < _player.Unit.MaxHp)
                _player.Unit.CurrentHp += heal;
            if (_player.Unit.CurrentHp > _player.Unit.MaxHp)
                _player.Unit.CurrentHp = _player.Unit.MaxHp;

            SetBindings(_player.UseItem);

            // Animation
            _player.Director.playableAsset = _player.UseItem;
            _player.Director.Play();

            // HUD Update
            _player.Unit.CurrentTp -= cost;
            PlayerCombatHUD.UpdateCombatHUD.Invoke();
            PlayerCombatHUD.CombatTextEvent.Invoke($"Curou <color=green>{heal}</color> HP", 3f);
            PlayerCombatHUD.TakenAction.Invoke();
            PlayerCombatHUD.ForceDisableButtons.Invoke(true);
        }
        else
        {
            InsufficientTP(cost);
        }
    }

    /// <summary>Performs a special action that deals damage to the enemy and consumes TP.</summary>
    /// <param name="cost">The amount of TP required to perform the action.</param>
    /// <param name="damageAmount">The amount of damage to be dealt to the enemy.</param>
    /// <remarks>
    /// This method retrieves the player and enemy objects, checks if the player has enough TP to perform the action, and if so, plays the attack animation and deals damage to the enemy. If the player doesn't have enough TP, a message is displayed indicating the required amount.
    /// </remarks>
    private void HeavyHitSpecialAction(int cost, int damageAmount)
    {
        if (_player.Unit.CurrentTp >= cost)
        {
            UpdateHUD();
            // Standard attack damage calculation
            var attackDamageCalculated = _player.Unit.Attack + damageAmount;
            if (InventoryManager.Instance.EquipmentSlots[3].equipItem != null)
                attackDamageCalculated += InventoryManager.Instance.EquipmentSlots[3].equipItem.StatusValue;
            SetBindings(_player.BasicAttack);
            ExecuteLogic(cost, attackDamageCalculated);
        }
        else
        {
            InsufficientTP(cost);
        }
    }
    #endregion
}
