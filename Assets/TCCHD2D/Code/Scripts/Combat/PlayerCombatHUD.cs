// Created by Sérgio Murillo da Costa Faria
// Date: 08/03/2023

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using Sirenix.OdinInspector;
using TMPro;

/// <summary>
/// Responsible for controlling the combat UI and the player's combat actions.
/// </summary>
public class PlayerCombatHUD : MonoBehaviour
{
    [TitleGroup("Units Info", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private UnitController playerUnitController;

    [SerializeField]
    private UnitController enemyUnitController;

    [TitleGroup("Player HUD Elements", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private Image playerHelthbarFill;

    [SerializeField]
    private TMP_Text playerHealthText;

    [SerializeField]
    private Image playerTpbarFill;

    [SerializeField]
    private TMP_Text playerTpText;

    [TitleGroup("Enemy HUD Elements", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private TMP_Text enemyName;

    [SerializeField]
    private Image enemyHelthbarFill;

    [SerializeField]
    private TMP_Text enemyHealthText;

    [TitleGroup("Combat Text Box", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private TMP_Text combatTextBox;

    [SerializeField]
    private float combatTextTimer;

    [TitleGroup("Buttons", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private Button attackButton;

    [SerializeField]
    private Button specialButton;

    [SerializeField]
    private Button itemButton;

    [SerializeField]
    private Button runButton;

    [TitleGroup("Debug Info", Alignment = TitleAlignments.Centered)]
    [ShowInInspector, ReadOnly]
    public static UnityAction TakenAction;
    public static UnityAction<string> CombatTextEvent;
    public static UnityAction UpdateCombatHUDPlayerHp;
    public static UnityAction UpdateCombatHUDPlayerTp;
    public static UnityAction UpdateCombatHUDEnemyHp;

    private void Start()
    {
        UpdateEnemyHealth();
        UpdatePlayerTp();
        UpdatePlayerHealth();

        playerHealthText.text = $"HP: {playerUnitController.Unit.CurrentHp} / {playerUnitController.Unit.MaxHp}";
        playerHelthbarFill.fillAmount = (float)playerUnitController.Unit.CurrentHp / playerUnitController.Unit.MaxHp;
        playerTpText.text = $"TP: {playerUnitController.Unit.CurrentTp}%";
        playerTpbarFill.fillAmount = (float)playerUnitController.Unit.CurrentTp / playerUnitController.Unit.MaxTp;
        enemyHealthText.text = $"HP: {enemyUnitController.Unit.CurrentHp} / {enemyUnitController.Unit.MaxHp}";
        enemyHelthbarFill.fillAmount = (float)enemyUnitController.Unit.CurrentHp / enemyUnitController.Unit.MaxHp;
        enemyName.text = $"{enemyUnitController.Unit.UnitName}:";
        playerUnitController.Unit.CurrentTp = 0;
        combatTextBox.text = "";
        
        CombatTextEvent += DisplayCombatText;
        UpdateCombatHUDPlayerHp += UpdatePlayerHealth;
        UpdateCombatHUDPlayerTp += UpdatePlayerTp;
        UpdateCombatHUDEnemyHp += UpdateEnemyHealth;
    }

    private void Update()
    {
        if (playerUnitController.Director.state == PlayState.Playing || enemyUnitController.Director.state == PlayState.Playing)
            DisableButtons(true);
        else
            DisableButtons(false);
    }

    private void DisableButtons(bool disabled)
    {
        switch (disabled)
        {
            case true:
                attackButton.interactable = false;
                specialButton.interactable = false;
                itemButton.interactable = false;
                runButton.interactable = false;
                break;
            case false:
                attackButton.interactable = true;
                specialButton.interactable = true;
                itemButton.interactable = true;
                runButton.interactable = true;
                break;
        }
    }

    /// <summary>
    /// Updates the player's health bar and text.
    /// </summary>
    public void UpdatePlayerHealth()
    {
        if (playerHealthText == null || playerHelthbarFill == null) return;
        playerHealthText.text = $"HP: {playerUnitController.Unit.CurrentHp} / {playerUnitController.Unit.MaxHp}";
        playerHelthbarFill.fillAmount = (float)playerUnitController.Unit.CurrentHp / playerUnitController.Unit.MaxHp;
    }

    /// <summary>
    /// Updates the enemy's health bar and text.
    /// </summary>
    public void UpdateEnemyHealth()
    {
        if (enemyHealthText == null || enemyHelthbarFill == null) return;
        enemyHealthText.text = $"{enemyUnitController.Unit.CurrentHp} / {enemyUnitController.Unit.MaxHp}";
        enemyHelthbarFill.fillAmount = (float)enemyUnitController.Unit.CurrentHp / enemyUnitController.Unit.MaxHp;
    }

    /// <summary>
    /// Updates the player's TP bar and text.
    /// </summary>
    public void UpdatePlayerTp()
    {
        if (playerTpText == null || playerTpbarFill == null) return;
        playerTpText.text = $"TP: {playerUnitController.Unit.CurrentTp}%";
        playerTpbarFill.fillAmount = (float)playerUnitController.Unit.CurrentTp / playerUnitController.Unit.MaxTp;
    }

    /// <summary>
    /// Adds damage text to the combat text box and calls the player unit attack method [<see cref="UnitController.AttackAction"/>].
    /// </summary>
    public void Attack()
    {
        playerUnitController.AttackAction(enemyUnitController);
        UpdatePlayerTp();
        CombatTextEvent.Invoke($"<b>Attacked <color=blue>{enemyUnitController.Unit.UnitName}</color> for <color=red>{playerUnitController.Unit.Attack}</color> damage</b>");
        TakenAction.Invoke();
    }

    /// <summary>
    /// Adds information in the combat text box and calls the player specific special attack method.
    /// </summary>
    public void Special()
    {
        if (playerUnitController.Unit.CurrentTp < playerUnitController.Unit.MaxTp / 2)
        {
            CombatTextEvent.Invoke("<color=red>Not enough TP</color>");
        }
        else
        {
            //playerUnitController.UnitDirector.Play(playerUnitController.SpecialAttacks[0]);
            playerUnitController.HealSpecialAction(playerUnitController);
            UpdatePlayerHealth();
            playerUnitController.Unit.CurrentTp -= 50;
            UpdatePlayerTp();
            CombatTextEvent.Invoke($"Healed <color=green>{playerUnitController.Unit.Attack}</color> HP");
            TakenAction.Invoke();
        }
    }

    /// <summary>
    /// Adds information in the combat text box and calls the player specific item method.
    /// </summary>
    public void Item()
    {
        CombatTextEvent.Invoke($"<b>PLACEHOLDER: Pressed <color=brown>Item</color> button</b>");
        //playerUnitController.UnitDirector.Play(playerUnitController.UseItem);
        TakenAction.Invoke();
    }

    /// <summary>
    /// Adds information in the combat text box and calls the player run method [<see cref="UnitController.RunAction"/>].
    /// </summary>
    public void Run()
    {
        var gotAway = playerUnitController.RunAction();

        if (gotAway)
        {
            SceneManager.LoadScene("scn_game");
            CombatTextEvent.Invoke($"<color=green>Ran away</color>");
            TakenAction.Invoke();
        }
        else
        {
            CombatTextEvent.Invoke($"<color=red>Failed to run away</color>");
            TakenAction.Invoke();
        }
    }
    
    public void DisplayCombatText(string text)
    {
        StartCoroutine(DisplayCombatTextCoroutine(text));
    }

    /// <summary>
    /// Displays combat text in the combat text box for a set amount of time then clears the text.
    /// </summary>
    /// <param name="text"></param>
    public IEnumerator DisplayCombatTextCoroutine(string text)
    {
        combatTextBox.text = text;
        yield return new WaitForSeconds(combatTextTimer);
        combatTextBox.text = "";
    }
}