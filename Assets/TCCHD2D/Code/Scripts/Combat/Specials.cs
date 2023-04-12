// Created by Sérgio Murillo da Costa Faria
// Date: 12/04/2023

using System;
using System.Collections.Generic;
using UnityEngine;

public class Specials : MonoBehaviour
{
    public List<string> specialsList = new();
    private PlayerCombatHUD combatHUD;
    private TurnManager turnManager;
    
    private void Start()
    {
        combatHUD = FindObjectOfType<PlayerCombatHUD>();
        turnManager = FindObjectOfType<TurnManager>();
        specialsList.Add("Heal");
        specialsList.Add("Heavy Hit");
    }
    
    public void UseSpecial(string specialName)
    {
        switch (specialName)
        {
            case "Heal":
                HealSpecialAction();
                break;
            case "Heavy Hit":
                HeavyHitSpecialAction();
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
            player.Unit.CurrentHp += heal;
        
            // Animation
            player.Director.Play(player.BasicAttack); // TODO: Change to heal animation
        
            // HUD Update
            player.Unit.CurrentTp -= 20;
            PlayerCombatHUD.UpdateCombatHUD.Invoke();
            PlayerCombatHUD.CombatTextEvent.Invoke($"Healed <color=green>{heal}</color> HP");
            PlayerCombatHUD.TakenAction.Invoke();
        }
        else
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"Not enough <color=red>TP</color>\n" +
                                                   $"Cost: <color=red>20 TP</color>");
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

            // Animation
            player.Director.Play(player.BasicAttack); // TODO: Change to heavy hit animation

            // HUD Update
            player.Unit.CurrentTp -= 40;

            // Damage multiplied
            target.TakeDamage(attackDamageCalculated);
            
            PlayerCombatHUD.UpdateCombatHUDPlayerTp.Invoke();
            PlayerCombatHUD.CombatTextEvent.Invoke($"<b>Used <color=purple>Heavy Hit</color> on <color=blue>{target.Unit.UnitName}</color> for <color=red>{target.damageTakenThisTurn}</color> damage</b>");
            PlayerCombatHUD.TakenAction.Invoke();
        }
        else
        {
            PlayerCombatHUD.CombatTextEvent.Invoke($"Not enough <color=red>TP</color>\n" +
                                                   $"Cost: <color=red>40 TP</color>");
        }
    }
}
