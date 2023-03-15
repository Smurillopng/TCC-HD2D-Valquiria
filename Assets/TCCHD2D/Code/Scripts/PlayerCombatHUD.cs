// Created by Sérgio Murillo da Costa Faria
// Date: 08/03/2023

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using Sirenix.OdinInspector;
using TMPro;

/// <summary>
/// Responsible for controling the combat UI and the player's combat actions.
/// </summary>
public class PlayerCombatHUD : MonoBehaviour
{
    [SerializeField]
    private GameObject player;

    [SerializeField]
    private Image playerHelthbarFill;

    [SerializeField]
    private TMP_Text playerHealthText;

    [SerializeField]
    private Image enemyHelthbarFill;

    [SerializeField]
    private TMP_Text enemyHealthText;
    
    [SerializeField]
    private UnitController playerUnitController;
    
    [SerializeField]
    private UnitController enemyUnitController;

    private void Start()
    {
        playerHealthText.text = $"{playerUnitController.Unit.CurrentHp} / {playerUnitController.Unit.MaxHp}";
        playerHelthbarFill.fillAmount = (float)playerUnitController.Unit.CurrentHp / playerUnitController.Unit.MaxHp;
        enemyHealthText.text = $"{enemyUnitController.Unit.CurrentHp} / {enemyUnitController.Unit.MaxHp}";
        enemyHelthbarFill.fillAmount = (float)enemyUnitController.Unit.CurrentHp / enemyUnitController.Unit.MaxHp;
    }

    public void UpdatePlayerHealth()
    {
        playerHealthText.text = $"{playerUnitController.Unit.CurrentHp} / {playerUnitController.Unit.MaxHp}";
        playerHelthbarFill.fillAmount = (float)playerUnitController.Unit.CurrentHp / playerUnitController.Unit.MaxHp;
    }

    public void UpdateEnemyHealth()
    {
        enemyHealthText.text = $"{enemyUnitController.Unit.CurrentHp} / {enemyUnitController.Unit.MaxHp}";
        enemyHelthbarFill.fillAmount = (float)enemyUnitController.Unit.CurrentHp / enemyUnitController.Unit.MaxHp;
    }

    public void Attack()
    {
        Debug.Log("<b>Pressed <color=red>Attack</color> button</b>"); 
        playerUnitController.UnitDirector.Play(playerUnitController.BasicAttack);
        playerUnitController.AttackAction(enemyUnitController);
    }

    public void Special()
    {
        Debug.Log("<b>Pressed  <color=magenta>Special</color> button</b>");
    }

    public void Item()
    {
        Debug.Log("<b>Pressed  <color=cyan>Item</color> button</b>");
    }

    public void Run()
    {
        Debug.Log($"<b>Pressed <color=green>Run</color> button</b>");
    }
}
