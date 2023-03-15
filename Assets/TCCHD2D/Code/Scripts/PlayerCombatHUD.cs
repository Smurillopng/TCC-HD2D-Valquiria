// Created by Sérgio Murillo da Costa Faria
// Date: 08/03/2023

using System;
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
    private Image enemyHelthbarFill;

    [SerializeField]
    private TMP_Text enemyHealthText;

    [SerializeField]
    private UnitController playerUnitController;

    [SerializeField]
    private UnitController enemyUnitController;

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
        playerHealthText.text = $"{playerUnitController.Unit.CurrentHp} / {playerUnitController.Unit.MaxHp}";
        playerHelthbarFill.fillAmount = (float)playerUnitController.Unit.CurrentHp / playerUnitController.Unit.MaxHp;
        enemyHealthText.text = $"{enemyUnitController.Unit.CurrentHp} / {enemyUnitController.Unit.MaxHp}";
        enemyHelthbarFill.fillAmount = (float)enemyUnitController.Unit.CurrentHp / enemyUnitController.Unit.MaxHp;

        takenAction += UpdatePlayerHealth;
    }

    private void Update()
    {
        if (playerUnitController.Unit.HasTakenTurn == true)
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

    public void Attack()
    {
        Debug.Log("<b>Pressed <color=red>Attack</color> button</b>");
        playerUnitController.UnitDirector.Play(playerUnitController.BasicAttack);
        playerUnitController.AttackAction(enemyUnitController);
        takenAction.Invoke();
    }

    public void Special()
    {
        Debug.Log("<b>Pressed  <color=magenta>Special</color> button</b>");
        //playerUnitController.UnitDirector.Play(playerUnitController.SpecialAttacks[0]);
        playerUnitController.HealSpecialAction(playerUnitController);
        UpdatePlayerHealth();
        takenAction.Invoke();
    }

    public void Item()
    {
        Debug.Log("<b>Pressed  <color=cyan>Item</color> button</b>");
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
            Debug.Log($"<b>Pressed <color=green>Run</color> button</b> | Run away <color=green>successfully</color>");
            takenAction.Invoke();
        }
        else
        {
            Debug.Log($"<b>Pressed <color=green>Run</color> button</b> | Run away <color=red>unsuccessfully</color>");
            takenAction -= UpdatePlayerHealth;
            takenAction.Invoke();
        }
    }
}