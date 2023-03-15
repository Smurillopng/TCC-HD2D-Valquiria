// Created by Sérgio Murillo da Costa Faria
// Date: 08/03/2023

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
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
    private Image playerTpbarFill;

    [SerializeField]
    private TMP_Text playerTpText;

    [SerializeField]
    private TMP_Text enemyName;

    [SerializeField]
    private Image enemyHelthbarFill;

    [SerializeField]
    private TMP_Text enemyHealthText;

    [SerializeField]
    private UnitController playerUnitController;

    [SerializeField]
    private UnitController enemyUnitController;

    [SerializeField]
    private TMP_Text combatTextBox;

    [SerializeField] 
    private float combatTextTimer;

    [TitleGroup("Buttons")]
    [SerializeField]
    private Button attackButton;

    [SerializeField]
    private Button specialButton;

    [SerializeField]
    private Button itemButton;

    [SerializeField]
    private Button runButton;

    public static UnityAction takenAction;

    private void Start()
    {
        playerHealthText.text = $"HP: {playerUnitController.Unit.CurrentHp} / {playerUnitController.Unit.MaxHp}";
        playerHelthbarFill.fillAmount = (float)playerUnitController.Unit.CurrentHp / playerUnitController.Unit.MaxHp;
        playerTpText.text = $"TP: {playerUnitController.Unit.CurrentTp}%";
        playerTpbarFill.fillAmount = (float)playerUnitController.Unit.CurrentTp / playerUnitController.Unit.MaxTp;
        enemyHealthText.text = $"HP: {enemyUnitController.Unit.CurrentHp} / {enemyUnitController.Unit.MaxHp}";
        enemyHelthbarFill.fillAmount = (float)enemyUnitController.Unit.CurrentHp / enemyUnitController.Unit.MaxHp;
        enemyName.text = $"{enemyUnitController.Unit.UnitName}:";
        playerUnitController.Unit.CurrentTp = 0;
        combatTextBox.text = "";
        UpdatePlayerTp();
    }

    private void Update()
    {
        if (playerUnitController.UnitDirector.state == PlayState.Playing)
        {
            attackButton.interactable = false;
            specialButton.interactable = false;
            itemButton.interactable = false;
            runButton.interactable = false;
        }
        else
        {
            attackButton.interactable = true;
            specialButton.interactable = true;
            itemButton.interactable = true;
            runButton.interactable = true;
        }
    }

    public void UpdatePlayerHealth()
    {
        if (playerHealthText != null && playerHelthbarFill != null)
        {
            playerHealthText.text = $"{playerUnitController.Unit.CurrentHp} / {playerUnitController.Unit.MaxHp}";
            playerHelthbarFill.fillAmount = (float)playerUnitController.Unit.CurrentHp / playerUnitController.Unit.MaxHp;
        }
    }

    public void UpdateEnemyHealth()
    {
        if (enemyHealthText != null && enemyHelthbarFill != null)
        {
            enemyHealthText.text = $"{enemyUnitController.Unit.CurrentHp} / {enemyUnitController.Unit.MaxHp}";
            enemyHelthbarFill.fillAmount = (float)enemyUnitController.Unit.CurrentHp / enemyUnitController.Unit.MaxHp;
        }
    }
    
    public void UpdatePlayerTp()
    {
        if (playerTpText != null && playerTpbarFill != null)
        {
            playerTpText.text = $"TP: {playerUnitController.Unit.CurrentTp}%";
            playerTpbarFill.fillAmount = (float)playerUnitController.Unit.CurrentTp / playerUnitController.Unit.MaxTp;
        }
    }

    public void Attack()
    {
        playerUnitController.AttackAction(enemyUnitController);
        if (playerUnitController.Unit.CurrentTp < playerUnitController.Unit.MaxTp)
        {
            playerUnitController.Unit.CurrentTp += 10;
            UpdatePlayerTp();
        }
        StartCoroutine(DisplayCombatText($"<b>Attacked <color=blue>{enemyUnitController.Unit.UnitName}</color> for <color=red>{playerUnitController.Unit.Attack}</color> damage</b>"));
        takenAction.Invoke();
    }

    public void Special()
    {
        if (playerUnitController.Unit.CurrentTp < playerUnitController.Unit.MaxTp/2)
        {
            StartCoroutine(DisplayCombatText("<color=red>Not enough TP</color>"));
        }
        else
        {
            //playerUnitController.UnitDirector.Play(playerUnitController.SpecialAttacks[0]);
            playerUnitController.HealSpecialAction(playerUnitController);
            UpdatePlayerHealth();
            playerUnitController.Unit.CurrentTp -= 50;
            UpdatePlayerTp();
            StartCoroutine(DisplayCombatText($"Healed <color=green>{playerUnitController.Unit.Attack}</color> HP"));
            takenAction.Invoke();
        }
    }

    public void Item()
    {
        StartCoroutine(DisplayCombatText("<b>PLACEHOLDER: Pressed <color=cyan>Item</color> button</b>"));
        //playerUnitController.UnitDirector.Play(playerUnitController.UseItem);
        takenAction.Invoke();
    }

    public void Run()
    {
        var randomChance = UnityEngine.Random.Range(0, 100);
        randomChance += playerUnitController.Unit.Luck;
        randomChance = randomChance > 50 ? 1 : 0;
        if (randomChance == 1)
        {
            //playerUnitController.UnitDirector.Play(playerUnitController.Run);
            SceneManager.LoadScene("scn_game");
            StartCoroutine(DisplayCombatText("Run away <color=green>successfully</color>"));
            takenAction.Invoke();
        }
        else
        {
            StartCoroutine(DisplayCombatText("Run away <color=red>unsuccessfully</color>"));
            takenAction.Invoke();
        }
    }
    
    public IEnumerator DisplayCombatText(string text)
    {
        combatTextBox.text = text;
        yield return new WaitForSeconds(combatTextTimer);
        combatTextBox.text = "";
    }
}