// Created by Sérgio Murillo da Costa Faria
// Date: 12/04/2023

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public class Specials : MonoBehaviour
{
    public List<string> specialsList = new();
    private PlayerCombatHUD combatHUD;
    private TurnManager turnManager;

    private void Start()
    {
        combatHUD = FindObjectOfType<PlayerCombatHUD>();
        turnManager = FindObjectOfType<TurnManager>();
        specialsList.Add("Cura");
        specialsList.Add("Golpe Pesado");
        specialsList.Add("Ataque Fulminante");
    }

    public void UseSpecial(string specialName)
    {
        switch (specialName)
        {
            case "Cura":
                HealSpecialAction();
                break;
            case "Golpe Pesado":
                HeavyHitSpecialAction();
                break;
            case "Ataque Fulminante":
                FireAttackSpecialAction();
                break;
        }
    }

    /// <summary>
    /// An example of a special action.
    /// </summary>
    /// <param name="player"></param>
    private void HealSpecialAction()
    {
        var player = GameObject.FindWithTag("Player").GetComponent<UnitController>();
        if (player.Unit.CurrentTp >= 20)
        {
            turnManager.isPlayerTurn = false;
            combatHUD.SpecialPanel.SetActive(false);
            combatHUD.ReturnButton.gameObject.SetActive(false);
            combatHUD.ReturnButton.interactable = false;
            combatHUD.OptionsPanel.SetActive(true);
            PlayerCombatHUD.UpdateCombatHUD.Invoke();

            // Calculate heal based on luck
            var heal = player.Unit.Luck * 2;

            // Apply heal to target
            if (player.Unit.CurrentHp < player.Unit.MaxHp)
                player.Unit.CurrentHp += heal;

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
            player.Unit.CurrentTp -= 20;
            PlayerCombatHUD.UpdateCombatHUD.Invoke();
            PlayerCombatHUD.CombatTextEvent.Invoke($"Curou <color=green>{heal}</color> HP");
            PlayerCombatHUD.TakenAction.Invoke();
        }
        else
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"<color=red>TP Insuficiente</color>\n" +
                                                   $"Quantidade necessária: <color=red>20 TP</color>");
        }
    }

    private void HeavyHitSpecialAction()
    {
        var player = GameObject.FindWithTag("Player").GetComponent<UnitController>();
        var target = GameObject.FindWithTag("Enemy").GetComponent<UnitController>();

        if (player.Unit.CurrentTp >= 40)
        {
            turnManager.isPlayerTurn = false;
            combatHUD.SpecialPanel.SetActive(false);
            combatHUD.ReturnButton.gameObject.SetActive(false);
            combatHUD.ReturnButton.interactable = false;
            combatHUD.OptionsPanel.SetActive(true);
            PlayerCombatHUD.UpdateCombatHUD.Invoke();

            // Standard attack damage calculation
            var attackDamageCalculated = player.Unit.Attack + player.Unit.Attack;
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
            player.Unit.CurrentTp -= 40;

            // Damage multiplied
            target.TakeDamage(attackDamageCalculated);

            PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
            PlayerCombatHUD.CombatTextEvent.Invoke($"<b>Usou <color=purple>Golpe Pesado</color> em <color=blue>{target.Unit.UnitName}</color> causando <color=red>{target.damageTakenThisTurn}</color> de dano</b>");
            PlayerCombatHUD.TakenAction.Invoke();
        }
        else
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"<color=red>TP Insuficiente</color>\n" +
                                                   $"Quantidade necessária: <color=red>40 TP</color>");
        }
    }

    private void FireAttackSpecialAction()
    {
        var player = GameObject.FindWithTag("Player").GetComponent<UnitController>();
        var enemy = GameObject.FindWithTag("Enemy").GetComponent<UnitController>();
        var playerAilments = player.gameObject.GetComponent<Ailments>();
        var enemyAilments = enemy.gameObject.GetComponent<Ailments>();

        if (player.Unit.CurrentTp >= 30)
        {
            turnManager.isPlayerTurn = false;
            combatHUD.SpecialPanel.SetActive(false);
            combatHUD.ReturnButton.gameObject.SetActive(false);
            combatHUD.ReturnButton.interactable = false;
            combatHUD.OptionsPanel.SetActive(true);
            PlayerCombatHUD.UpdateCombatHUD.Invoke();

            // Standard attack damage calculation
            var attackDamageCalculated = player.Unit.Attack + player.Unit.Attack;
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
            player.Unit.CurrentTp -= 30;

            // Damage multiplied
            enemy.TakeDamage(attackDamageCalculated);
            enemyAilments.SetOnFire(true);

            PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
            PlayerCombatHUD.CombatTextEvent.Invoke($"<b>Usou <color=purple>Ataque Fulminante</color> em <color=blue>{enemy.Unit.UnitName}</color> causando <color=red>{enemy.damageTakenThisTurn}</color> de dano</b>");
            PlayerCombatHUD.TakenAction.Invoke();
        }
        else
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"<color=red>TP Insuficiente</color>\n" +
                                                   $"Quantidade necessária: <color=red>30 TP</color>");
        }
    }
}
