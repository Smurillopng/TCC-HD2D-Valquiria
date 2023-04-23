// Created by Sérgio Murillo da Costa Faria
// Date: 08/03/2023

using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using TMPro;

/// <summary>
/// Responsible for controlling the combat UI and the player's combat actions.
/// </summary>
public class PlayerCombatHUD : MonoBehaviour
{
    [TitleGroup("Player HUD Elements", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private Image playerHealthBarFill;
    [SerializeField]
    private TMP_Text playerHealthText;
    [SerializeField]
    private Image playerTpBarFill;
    [SerializeField]
    private TMP_Text playerTpText;

    [TitleGroup("Enemy HUD Elements", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private TMP_Text enemyName;
    [SerializeField]
    private Image enemyHealthBarFill;
    [SerializeField]
    private TMP_Text enemyHealthText;

    [TitleGroup("Combat Text Box", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private TMP_Text combatTextBox;
    [SerializeField]
    private float combatTextTimer;

    [TitleGroup("Combat Panels", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private GameObject buttonPrefab;
    [SerializeField]
    private GameObject optionsPanel;
    [SerializeField]
    private GameObject specialPanel;
    [SerializeField]
    private GameObject itemPanel;

    [TitleGroup("Buttons", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    private Button attackButton;
    [SerializeField]
    private Button specialButton;
    [SerializeField]
    private Button itemButton;
    [SerializeField]
    private Button runButton;
    [SerializeField]
    private Button returnButton;

    [TitleGroup("Debug Info", Alignment = TitleAlignments.Centered)]
    [ShowInInspector, ReadOnly]
    public static UnityAction TakenAction;
    public static UnityAction<string> CombatTextEvent;
    public static UnityAction UpdateCombatHUDPlayerHp;
    public static UnityAction UpdateCombatHUDPlayerTp;
    public static UnityAction UpdateCombatHUDEnemyHp;
    public static UnityAction UpdateCombatHUD;

    [SerializeField] private TurnManager turnManager;
    [SerializeField] private Specials specials;
    private bool _wasPlayerTurn;
    
    public GameObject SpecialPanel => specialPanel;
    public GameObject OptionsPanel => optionsPanel;
    public Button ReturnButton => returnButton;

    private void OnEnable()
    {
        CombatTextEvent += DisplayCombatText;
        UpdateCombatHUDPlayerHp += UpdatePlayerHealth;
        UpdateCombatHUDPlayerTp += UpdatePlayerTp;
        UpdateCombatHUDEnemyHp += UpdateEnemyHealth;
        UpdateCombatHUD += UpdateCombatHUDs;
    }

    private void Start()
    {
        playerHealthText.text = $"HP: {turnManager.PlayerUnitController.Unit.CurrentHp} / {turnManager.PlayerUnitController.Unit.MaxHp}";
        playerHealthBarFill.fillAmount = (float)turnManager.PlayerUnitController.Unit.CurrentHp / turnManager.PlayerUnitController.Unit.MaxHp;
        playerTpText.text = $"TP: {turnManager.PlayerUnitController.Unit.CurrentTp}%";
        playerTpBarFill.fillAmount = (float)turnManager.PlayerUnitController.Unit.CurrentTp / turnManager.PlayerUnitController.Unit.MaxTp;
        enemyHealthText.text = $"HP: {turnManager.EnemyUnitController.Unit.CurrentHp} / {turnManager.EnemyUnitController.Unit.MaxHp}";
        enemyHealthBarFill.fillAmount = (float)turnManager.EnemyUnitController.Unit.CurrentHp / turnManager.EnemyUnitController.Unit.MaxHp;
        enemyName.text = $"{turnManager.EnemyUnitController.Unit.UnitName}:";
        turnManager.PlayerUnitController.Unit.CurrentTp = 0;
        combatTextBox.text = "";

        UpdateEnemyHealth();
        UpdatePlayerTp();
        UpdatePlayerHealth();
        
        CombatTextEvent += DisplayCombatText;
        UpdateCombatHUDPlayerHp += UpdatePlayerHealth;
        UpdateCombatHUDPlayerTp += UpdatePlayerTp;
        UpdateCombatHUDEnemyHp += UpdateEnemyHealth;
    }
    
    private void UpdateCombatHUDs()
    {
        UpdatePlayerHealth();
        UpdatePlayerTp();
        UpdateEnemyHealth();
    }

    private void Update()
    {
        if (_wasPlayerTurn == turnManager.isPlayerTurn) return;
        _wasPlayerTurn = turnManager.isPlayerTurn;
        DisableButtons(!turnManager.isPlayerTurn);
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
        if (playerHealthText == null || playerHealthBarFill == null) return;
        playerHealthText.text = $"HP: {turnManager.PlayerUnitController.Unit.CurrentHp} / {turnManager.PlayerUnitController.Unit.MaxHp}";
        playerHealthBarFill.fillAmount = (float)turnManager.PlayerUnitController.Unit.CurrentHp / turnManager.PlayerUnitController.Unit.MaxHp;
    }

    /// <summary>
    /// Updates the enemy's health bar and text.
    /// </summary>
    public void UpdateEnemyHealth()
    {
        if (enemyHealthText == null || enemyHealthBarFill == null) return;
        enemyHealthText.text = $"{turnManager.EnemyUnitController.Unit.CurrentHp} / {turnManager.EnemyUnitController.Unit.MaxHp}";
        enemyHealthBarFill.fillAmount = (float)turnManager.EnemyUnitController.Unit.CurrentHp / turnManager.EnemyUnitController.Unit.MaxHp;
    }

    /// <summary>
    /// Updates the player's TP bar and text.
    /// </summary>
    private void UpdatePlayerTp()
    {
        if (playerTpText == null || playerTpBarFill == null) return;
        playerTpText.text = $"TP: {turnManager.PlayerUnitController.Unit.CurrentTp}%";
        playerTpBarFill.fillAmount = (float)turnManager.PlayerUnitController.Unit.CurrentTp / turnManager.PlayerUnitController.Unit.MaxTp;
    }

    private void DisplayCombatText(string text)
    {
        if (combatTextBox != null)
            StartCoroutine(DisplayCombatTextCoroutine(text));
    }

    /// <summary>
    /// Displays combat text in the combat text box for a set amount of time then clears the text.
    /// </summary>
    /// <param name="text"></param>
    private IEnumerator DisplayCombatTextCoroutine(string text)
    {
        combatTextBox.text = text;
        yield return new WaitForSeconds(combatTextTimer);
        combatTextBox.text = "";
    }

    private void OnDisable()
    {
        CombatTextEvent -= DisplayCombatText;
        UpdateCombatHUDPlayerHp -= UpdatePlayerHealth;
        UpdateCombatHUDPlayerTp -= UpdatePlayerTp;
        UpdateCombatHUDEnemyHp -= UpdateEnemyHealth;
        UpdateCombatHUD -= UpdateCombatHUDs;
    }
    
    /// <summary>
    /// Adds information in the combat text box and calls the player specific item method.
    /// </summary>
    public void Item()
    {
        var hasItem = InventoryManager.Instance.Inventory.OfType<Consumable>().Any();
        
        if (!hasItem)
        {
            CombatTextEvent.Invoke("<b>Sem itens!</b>");
            return;
        }
        
        optionsPanel.SetActive(false);
        itemPanel.SetActive(true);
        
        if (itemPanel.transform.childCount > 0)
        {
            foreach (Transform child in itemPanel.transform)
            {
                Destroy(child.gameObject);
            }
        }
        
        foreach (var item in InventoryManager.Instance.Inventory.OfType<Consumable>())
        {
            returnButton.gameObject.SetActive(true);
            returnButton.interactable = true;
            var button = Instantiate(buttonPrefab, itemPanel.transform);
            button.GetComponentInChildren<TextMeshProUGUI>().text = item.ItemName;
            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                InventoryManager.Instance.UseItem(item);
                UpdatePlayerHealth();
                UpdatePlayerTp();
                UpdateEnemyHealth();
                CombatTextEvent.Invoke($"<b>Usou {item.ItemName}!</b>");
                //turnManager.isPlayerTurn = false;
                TakenAction.Invoke();
                itemPanel.SetActive(false);
                returnButton.gameObject.SetActive(false);
                returnButton.interactable = false;
                optionsPanel.SetActive(true);
            });
        }
        //CombatTextEvent.Invoke($"<b>PLACEHOLDER: Pressed <color=brown>Item</color> button</b>");
        //playerUnitController.UnitDirector.Play(playerUnitController.UseItem);
        //turnManager.isPlayerTurn = false;
        //TakenAction.Invoke();
    }
    
    public void Return()
    {
        itemPanel.SetActive(false);
        specialPanel.SetActive(false);
        optionsPanel.SetActive(true);
        returnButton.gameObject.SetActive(false);
        returnButton.interactable = false;
    }
    
    public void Special()
    {
        optionsPanel.SetActive(false);
        specialPanel.SetActive(true);
        
        if (specialPanel.transform.childCount > 0)
        {
            foreach (Transform child in specialPanel.transform)
            {
                Destroy(child.gameObject);
            }
        }
        
        foreach (var method in specials.specialsList)
        {
            returnButton.gameObject.SetActive(true);
            returnButton.interactable = true;
            var button = Instantiate(buttonPrefab, specialPanel.transform);
            button.GetComponentInChildren<TextMeshProUGUI>().text = method;
            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                specials.UseSpecial(method);
            });
        }
    }
}