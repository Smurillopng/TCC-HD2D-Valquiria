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

    #endregion

    #region === Unity Methods ===========================================================

    /// <summary>Initializes the Start method.</summary>
    /// <remarks>This method finds the PlayerCombatHUD and TurnManager objects in the scene.</remarks>
    private void Start()
    {
        _combatHUD = FindObjectOfType<PlayerCombatHUD>();
        _turnManager = FindObjectOfType<TurnManager>();
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
                        FireAttackSpecialAction(specialAction.specialCost, specialAction);
                        break;
                    case AilmentType.Stunned:
                        StunAttackSpecialAction(specialAction.specialCost, specialAction);
                        break;
                    case AilmentType.Frozen:
                        FrozenAttackSpecialAction(specialAction.specialCost, specialAction);
                        break;
                    case AilmentType.Bleeding:
                        BleedingAttackSpecialAction(specialAction.specialCost, specialAction);
                        break;
                    case AilmentType.Incapacitated:
                        IncapacitateAttackSpecialAction(specialAction.specialCost, specialAction);
                        break;
                }
                break;
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
        var player = GameObject.FindWithTag("Player").GetComponent<UnitController>();
        if (player.Unit.CurrentTp >= cost)
        {
            _turnManager.isPlayerTurn = false;
            _combatHUD.SpecialPanel.SetActive(false);
            _combatHUD.ReturnButton.gameObject.SetActive(false);
            _combatHUD.ReturnButton.interactable = false;
            _combatHUD.OptionsPanel.SetActive(true);
            PlayerCombatHUD.UpdateCombatHUD.Invoke();

            // Calculate heal based on luck
            var heal = player.Unit.Luck + healAmount;

            // Apply heal to target
            if (player.Unit.CurrentHp < player.Unit.MaxHp)
                player.Unit.CurrentHp += heal;
            if (player.Unit.CurrentHp > player.Unit.MaxHp)
                player.Unit.CurrentHp = player.Unit.MaxHp;

            if (player.Unit.IsPlayer)
            {
                var enemyObject = GameObject.FindWithTag("Enemy");
                foreach (var track in player.UseItem.GetOutputTracks())
                {
                    switch (track.name)
                    {
                        case "AttackAnimation":
                            player.Director.SetGenericBinding(track, player.gameObject.GetComponentInChildren<Animator>());
                            break;
                        case "MovementAnimation":
                            player.Director.SetGenericBinding(track, player.gameObject.GetComponentInChildren<Animator>());
                            break;
                        case "Signals":
                            player.Director.SetGenericBinding(track, enemyObject.GetComponent<SignalReceiver>());
                            break;
                    }
                }
            }
            // Animation
            player.Director.playableAsset = player.UseItem;
            player.Director.Play();

            // HUD Update
            player.Unit.CurrentTp -= cost;
            PlayerCombatHUD.UpdateCombatHUD.Invoke();
            PlayerCombatHUD.CombatTextEvent.Invoke($"Curou <color=green>{heal}</color> HP");
            PlayerCombatHUD.TakenAction.Invoke();
            PlayerCombatHUD.ForceDisableButtons.Invoke(true);
        }
        else
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"<color=red>TP Insuficiente</color>\n" +
                                                   $"Quantidade necessária: <color=red>{cost} TP</color>");
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
        var player = GameObject.FindWithTag("Player").GetComponent<UnitController>();
        var target = GameObject.FindWithTag("Enemy").GetComponent<UnitController>();

        if (player.Unit.CurrentTp >= cost)
        {
            _turnManager.isPlayerTurn = false;
            _combatHUD.SpecialPanel.SetActive(false);
            _combatHUD.ReturnButton.gameObject.SetActive(false);
            _combatHUD.ReturnButton.interactable = false;
            _combatHUD.OptionsPanel.SetActive(true);
            PlayerCombatHUD.UpdateCombatHUD.Invoke();

            // Standard attack damage calculation
            var attackDamageCalculated = player.Unit.Attack + damageAmount;
            if (InventoryManager.Instance.EquipmentSlots[3].equipItem != null)
                attackDamageCalculated += InventoryManager.Instance.EquipmentSlots[3].equipItem.StatusValue;

            if (player.Unit.IsPlayer)
            {
                var enemyObject = GameObject.FindWithTag("Enemy");
                foreach (var track in player.BasicAttack.GetOutputTracks())
                {
                    switch (track.name)
                    {
                        case "AttackAnimation":
                            player.Director.SetGenericBinding(track, player.gameObject.GetComponentInChildren<Animator>());
                            break;
                        case "MovementAnimation":
                            player.Director.SetGenericBinding(track, player.gameObject.GetComponentInChildren<Animator>());
                            break;
                        case "Signals":
                            player.Director.SetGenericBinding(track, enemyObject.GetComponent<SignalReceiver>());
                            break;
                    }
                }
            }

            // Animation
            player.Director.Play(player.BasicAttack); // TODO: Change to heavy hit animation

            // HUD Update
            player.Unit.CurrentTp -= cost;

            // Damage multiplied
            target.TakeDamage(attackDamageCalculated);

            PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
            PlayerCombatHUD.CombatTextEvent.Invoke($"<b>Usou <color=purple>Golpe Pesado</color> em <color=blue>{target.Unit.UnitName}</color> causando <color=red>{target.damageTakenThisTurn}</color> de dano</b>");
            PlayerCombatHUD.TakenAction.Invoke();
            PlayerCombatHUD.ForceDisableButtons.Invoke(true);
        }
        else
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"<color=red>TP Insuficiente</color>\n" +
                                                   $"Quantidade necessária: <color=red>{cost} TP</color>");
        }
    }
    /// <summary>Performs a special attack action, deducting the cost from the player's TP and dealing damage to the enemy.</summary>
    /// <param name="cost">The cost of the special attack in TP.</param>
    /// <exception cref="NullReferenceException">Thrown when the player or enemy game object cannot be found.</exception>
    private void FireAttackSpecialAction(int cost, Special specialAction)
    {
        var player = GameObject.FindWithTag("Player").GetComponent<UnitController>();
        var enemy = GameObject.FindWithTag("Enemy").GetComponent<UnitController>();
        var playerAilments = player.gameObject.GetComponent<Ailments>();
        var enemyAilments = enemy.gameObject.GetComponent<Ailments>();

        if (player.Unit.CurrentTp >= cost)
        {
            _turnManager.isPlayerTurn = false;
            _combatHUD.SpecialPanel.SetActive(false);
            _combatHUD.ReturnButton.gameObject.SetActive(false);
            _combatHUD.ReturnButton.interactable = false;
            _combatHUD.OptionsPanel.SetActive(true);
            PlayerCombatHUD.UpdateCombatHUD.Invoke();

            // Standard attack damage calculation
            var attackDamageCalculated = player.Unit.Attack;
            if (InventoryManager.Instance.EquipmentSlots[3].equipItem != null)
                attackDamageCalculated += InventoryManager.Instance.EquipmentSlots[3].equipItem.StatusValue;

            if (player.Unit.IsPlayer)
            {
                var enemyObject = GameObject.FindWithTag("Enemy");
                foreach (var track in player.BasicAttack.GetOutputTracks())
                {
                    switch (track.name)
                    {
                        case "AttackAnimation":
                            player.Director.SetGenericBinding(track, player.gameObject.GetComponentInChildren<Animator>());
                            break;
                        case "MovementAnimation":
                            player.Director.SetGenericBinding(track, player.gameObject.GetComponentInChildren<Animator>());
                            break;
                        case "Signals":
                            player.Director.SetGenericBinding(track, enemyObject.GetComponent<SignalReceiver>());
                            break;
                    }
                }
            }

            // Animation
            player.Director.Play(player.BasicAttack); // TODO: Change to heavy hit animation

            // HUD Update
            player.Unit.CurrentTp -= cost;

            // Damage multiplied
            enemy.TakeDamage(attackDamageCalculated);
            enemyAilments.SetAilment(AilmentType.OnFire, true, specialAction.turnsToLast);

            PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
            PlayerCombatHUD.CombatTextEvent.Invoke($"<b>Usou <color=purple>Ataque Fulminante</color> em <color=blue>{enemy.Unit.UnitName}</color> causando <color=red>{enemy.damageTakenThisTurn}</color> de dano</b>");
            PlayerCombatHUD.TakenAction.Invoke();
            PlayerCombatHUD.ForceDisableButtons.Invoke(true);
        }
        else
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"<color=red>TP Insuficiente</color>\n" +
                                                   $"Quantidade necessária: <color=red>{cost} TP</color>");
        }
    }

    private void StunAttackSpecialAction(int cost, Special specialAction)
    {
        var player = GameObject.FindWithTag("Player").GetComponent<UnitController>();
        var enemy = GameObject.FindWithTag("Enemy").GetComponent<UnitController>();
        var playerAilments = player.gameObject.GetComponent<Ailments>();
        var enemyAilments = enemy.gameObject.GetComponent<Ailments>();

        if (player.Unit.CurrentTp >= cost)
        {
            _turnManager.isPlayerTurn = false;
            _combatHUD.SpecialPanel.SetActive(false);
            _combatHUD.ReturnButton.gameObject.SetActive(false);
            _combatHUD.ReturnButton.interactable = false;
            _combatHUD.OptionsPanel.SetActive(true);
            PlayerCombatHUD.UpdateCombatHUD.Invoke();

            // Standard attack damage calculation
            var attackDamageCalculated = player.Unit.Attack;
            if (InventoryManager.Instance.EquipmentSlots[3].equipItem != null)
                attackDamageCalculated += InventoryManager.Instance.EquipmentSlots[3].equipItem.StatusValue;

            if (player.Unit.IsPlayer)
            {
                var enemyObject = GameObject.FindWithTag("Enemy");
                foreach (var track in player.BasicAttack.GetOutputTracks())
                {
                    switch (track.name)
                    {
                        case "AttackAnimation":
                            player.Director.SetGenericBinding(track, player.gameObject.GetComponentInChildren<Animator>());
                            break;
                        case "MovementAnimation":
                            player.Director.SetGenericBinding(track, player.gameObject.GetComponentInChildren<Animator>());
                            break;
                        case "Signals":
                            player.Director.SetGenericBinding(track, enemyObject.GetComponent<SignalReceiver>());
                            break;
                    }
                }
            }

            // Animation
            player.Director.Play(player.BasicAttack); // TODO: Change to heavy hit animation

            // HUD Update
            player.Unit.CurrentTp -= cost;

            // Damage multiplied
            enemy.TakeDamage(attackDamageCalculated);
            enemyAilments.SetAilment(AilmentType.Stunned, true, specialAction.turnsToLast);

            PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
            PlayerCombatHUD.CombatTextEvent.Invoke($"<b>Usou <color=purple>Ataque Fulminante</color> em <color=blue>{enemy.Unit.UnitName}</color> causando <color=red>{enemy.damageTakenThisTurn}</color> de dano</b>");
            PlayerCombatHUD.TakenAction.Invoke();
            PlayerCombatHUD.ForceDisableButtons.Invoke(true);
        }
        else
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"<color=red>TP Insuficiente</color>\n" +
                                                   $"Quantidade necessária: <color=red>{cost} TP</color>");
        }
    }

    private void FrozenAttackSpecialAction(int cost, Special specialAction)
    {
        var player = GameObject.FindWithTag("Player").GetComponent<UnitController>();
        var enemy = GameObject.FindWithTag("Enemy").GetComponent<UnitController>();
        var playerAilments = player.gameObject.GetComponent<Ailments>();
        var enemyAilments = enemy.gameObject.GetComponent<Ailments>();

        if (player.Unit.CurrentTp >= cost)
        {
            _turnManager.isPlayerTurn = false;
            _combatHUD.SpecialPanel.SetActive(false);
            _combatHUD.ReturnButton.gameObject.SetActive(false);
            _combatHUD.ReturnButton.interactable = false;
            _combatHUD.OptionsPanel.SetActive(true);
            PlayerCombatHUD.UpdateCombatHUD.Invoke();

            // Standard attack damage calculation
            var attackDamageCalculated = player.Unit.Attack;
            if (InventoryManager.Instance.EquipmentSlots[3].equipItem != null)
                attackDamageCalculated += InventoryManager.Instance.EquipmentSlots[3].equipItem.StatusValue;

            if (player.Unit.IsPlayer)
            {
                var enemyObject = GameObject.FindWithTag("Enemy");
                foreach (var track in player.BasicAttack.GetOutputTracks())
                {
                    switch (track.name)
                    {
                        case "AttackAnimation":
                            player.Director.SetGenericBinding(track, player.gameObject.GetComponentInChildren<Animator>());
                            break;
                        case "MovementAnimation":
                            player.Director.SetGenericBinding(track, player.gameObject.GetComponentInChildren<Animator>());
                            break;
                        case "Signals":
                            player.Director.SetGenericBinding(track, enemyObject.GetComponent<SignalReceiver>());
                            break;
                    }
                }
            }

            // Animation
            player.Director.Play(player.BasicAttack); // TODO: Change to heavy hit animation

            // HUD Update
            player.Unit.CurrentTp -= cost;

            // Damage multiplied
            enemy.TakeDamage(attackDamageCalculated);
            enemyAilments.SetAilment(AilmentType.Frozen, true, specialAction.turnsToLast);

            PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
            PlayerCombatHUD.CombatTextEvent.Invoke($"<b>Usou <color=purple>Ataque Fulminante</color> em <color=blue>{enemy.Unit.UnitName}</color> causando <color=red>{enemy.damageTakenThisTurn}</color> de dano</b>");
            PlayerCombatHUD.TakenAction.Invoke();
            PlayerCombatHUD.ForceDisableButtons.Invoke(true);
        }
        else
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"<color=red>TP Insuficiente</color>\n" +
                                                   $"Quantidade necessária: <color=red>{cost} TP</color>");
        }
    }

    private void BleedingAttackSpecialAction(int cost, Special specialAction)
    {
        var player = GameObject.FindWithTag("Player").GetComponent<UnitController>();
        var enemy = GameObject.FindWithTag("Enemy").GetComponent<UnitController>();
        var playerAilments = player.gameObject.GetComponent<Ailments>();
        var enemyAilments = enemy.gameObject.GetComponent<Ailments>();

        if (player.Unit.CurrentTp >= cost)
        {
            _turnManager.isPlayerTurn = false;
            _combatHUD.SpecialPanel.SetActive(false);
            _combatHUD.ReturnButton.gameObject.SetActive(false);
            _combatHUD.ReturnButton.interactable = false;
            _combatHUD.OptionsPanel.SetActive(true);
            PlayerCombatHUD.UpdateCombatHUD.Invoke();

            // Standard attack damage calculation
            var attackDamageCalculated = player.Unit.Attack;
            if (InventoryManager.Instance.EquipmentSlots[3].equipItem != null)
                attackDamageCalculated += InventoryManager.Instance.EquipmentSlots[3].equipItem.StatusValue;

            if (player.Unit.IsPlayer)
            {
                var enemyObject = GameObject.FindWithTag("Enemy");
                foreach (var track in player.BasicAttack.GetOutputTracks())
                {
                    switch (track.name)
                    {
                        case "AttackAnimation":
                            player.Director.SetGenericBinding(track, player.gameObject.GetComponentInChildren<Animator>());
                            break;
                        case "MovementAnimation":
                            player.Director.SetGenericBinding(track, player.gameObject.GetComponentInChildren<Animator>());
                            break;
                        case "Signals":
                            player.Director.SetGenericBinding(track, enemyObject.GetComponent<SignalReceiver>());
                            break;
                    }
                }
            }

            // Animation
            player.Director.Play(player.BasicAttack); // TODO: Change to heavy hit animation

            // HUD Update
            player.Unit.CurrentTp -= cost;

            // Damage multiplied
            enemy.TakeDamage(attackDamageCalculated);
            enemyAilments.SetAilment(AilmentType.Bleeding, true, specialAction.turnsToLast);

            PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
            PlayerCombatHUD.CombatTextEvent.Invoke($"<b>Usou <color=purple>Ataque Fulminante</color> em <color=blue>{enemy.Unit.UnitName}</color> causando <color=red>{enemy.damageTakenThisTurn}</color> de dano</b>");
            PlayerCombatHUD.TakenAction.Invoke();
            PlayerCombatHUD.ForceDisableButtons.Invoke(true);
        }
        else
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"<color=red>TP Insuficiente</color>\n" +
                                                   $"Quantidade necessária: <color=red>{cost} TP</color>");
        }
    }

    private void IncapacitateAttackSpecialAction(int cost, Special specialAction)
    {
        var player = GameObject.FindWithTag("Player").GetComponent<UnitController>();
        var enemy = GameObject.FindWithTag("Enemy").GetComponent<UnitController>();
        var playerAilments = player.gameObject.GetComponent<Ailments>();
        var enemyAilments = enemy.gameObject.GetComponent<Ailments>();

        if (player.Unit.CurrentTp >= cost)
        {
            _turnManager.isPlayerTurn = false;
            _combatHUD.SpecialPanel.SetActive(false);
            _combatHUD.ReturnButton.gameObject.SetActive(false);
            _combatHUD.ReturnButton.interactable = false;
            _combatHUD.OptionsPanel.SetActive(true);
            PlayerCombatHUD.UpdateCombatHUD.Invoke();

            // Standard attack damage calculation
            var attackDamageCalculated = player.Unit.Attack;
            if (InventoryManager.Instance.EquipmentSlots[3].equipItem != null)
                attackDamageCalculated += InventoryManager.Instance.EquipmentSlots[3].equipItem.StatusValue;

            if (player.Unit.IsPlayer)
            {
                var enemyObject = GameObject.FindWithTag("Enemy");
                foreach (var track in player.BasicAttack.GetOutputTracks())
                {
                    switch (track.name)
                    {
                        case "AttackAnimation":
                            player.Director.SetGenericBinding(track, player.gameObject.GetComponentInChildren<Animator>());
                            break;
                        case "MovementAnimation":
                            player.Director.SetGenericBinding(track, player.gameObject.GetComponentInChildren<Animator>());
                            break;
                        case "Signals":
                            player.Director.SetGenericBinding(track, enemyObject.GetComponent<SignalReceiver>());
                            break;
                    }
                }
            }

            // Animation
            player.Director.Play(player.BasicAttack); // TODO: Change to heavy hit animation

            // HUD Update
            player.Unit.CurrentTp -= cost;

            // Damage multiplied
            enemy.TakeDamage(attackDamageCalculated);
            enemyAilments.SetAilment(AilmentType.Incapacitated, true, specialAction.turnsToLast);

            PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
            PlayerCombatHUD.CombatTextEvent.Invoke($"<b>Usou <color=purple>Ataque Fulminante</color> em <color=blue>{enemy.Unit.UnitName}</color> causando <color=red>{enemy.damageTakenThisTurn}</color> de dano</b>");
            PlayerCombatHUD.TakenAction.Invoke();
            PlayerCombatHUD.ForceDisableButtons.Invoke(true);
        }
        else
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"<color=red>TP Insuficiente</color>\n" +
                                                   $"Quantidade necessária: <color=red>{cost} TP</color>");
        }
    }

    #endregion
}
