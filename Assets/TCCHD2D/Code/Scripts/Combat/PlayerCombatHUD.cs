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
/// <remarks>
/// Created by Sérgio Murillo da Costa Faria on 08/03/2023.
/// </remarks>
[HideMonoScript]
public class PlayerCombatHUD : MonoBehaviour
{
    #region === Variables ===============================================================

    [TitleGroup("Player HUD Elements", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    [Tooltip("The fill image of the player's health bar.")]
    private Image playerHealthBarFill;

    [SerializeField]
    [Tooltip("The text displaying the player's current health.")]
    private TMP_Text playerHealthText;

    [SerializeField]
    [Tooltip("The fill image of the player's Tp bar.")]
    private Image playerTpBarFill;

    [SerializeField]
    [Tooltip("The text displaying the player's current Tp.")]
    private TMP_Text playerTpText;

    [TitleGroup("Enemy HUD Elements", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    [Tooltip("The name text of the enemy.")]
    private TMP_Text enemyName;

    [SerializeField]
    [Tooltip("The fill image of the enemy's health bar.")]
    private Image enemyHealthBarFill;

    [SerializeField]
    [Tooltip("The text displaying the enemy's current health.")]
    private TMP_Text enemyHealthText;

    [TitleGroup("Combat Text Box", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    [Tooltip("The text box displaying combat information.")]
    private TMP_Text combatTextBox;

    [SerializeField]
    [Tooltip("The time duration for displaying combat information in the text box.")]
    private float combatTextTimer;

    [TitleGroup("Combat Panels", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    [Tooltip("The prefab for combat buttons.")]
    private GameObject buttonPrefab;

    [SerializeField]
    [Tooltip("The panel containing combat options.")]
    private GameObject optionsPanel;

    [SerializeField]
    [Tooltip("The panel containing special combat options.")]
    private GameObject specialPanel;

    [SerializeField]
    [Tooltip("The panel containing item options.")]
    private GameObject itemPanel;

    [TitleGroup("Buttons", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    [Tooltip("The button for attacking the enemy.")]
    private Button attackButton;

    [SerializeField]
    [Tooltip("The button for using a special attack on the enemy.")]
    private Button specialButton;

    [SerializeField]
    [Tooltip("The button for using an item in combat.")]
    private Button itemButton;

    [SerializeField]
    [Tooltip("The button for attempting to run away from combat.")]
    private Button runButton;

    [SerializeField]
    [Tooltip("The button for attempting to run away from combat.")]
    private Button returnButton;

    [TitleGroup("Debug Info", Alignment = TitleAlignments.Centered)]
    [ShowInInspector, ReadOnly]

    public static UnityAction TakenAction;
    public static UnityAction<string> CombatTextEvent;
    public static UnityAction UpdateCombatHUDPlayerHp;
    public static UnityAction UpdateCombatHUDPlayerTp;
    public static UnityAction UpdateCombatHUDEnemyHp;
    public static UnityAction UpdateCombatHUD;

    [SerializeField]
    [Tooltip("The manager for controlling turns in combat.")]
    private TurnManager turnManager;

    [SerializeField]
    [Tooltip("The collection of special attacks available to the player.")]
    private Specials specials;

    public GameObject SpecialPanel => specialPanel;
    public GameObject OptionsPanel => optionsPanel;
    public Button ReturnButton => returnButton;

    #endregion

    #region === Unity Methods ===========================================================

    /// <summary>
    /// Adds the methods to the events.
    /// </summary>
    private void OnEnable()
    {
        CombatTextEvent += DisplayCombatText;
        UpdateCombatHUDPlayerHp += UpdatePlayerHealth;
        UpdateCombatHUDPlayerTp += UpdatePlayerTp;
        UpdateCombatHUDEnemyHp += UpdateEnemyHealth;
        UpdateCombatHUD += UpdateCombatHUDs;
    }
    /// <summary>
    /// Initializes the combat HUD.
    /// Updates the combat HUD to display the player's and enemy's health and Tp.
    /// </summary>
    private void Start()
    {
        playerHealthText.text = $"HP: {turnManager.PlayerUnitController.Unit.CurrentHp} / {turnManager.PlayerUnitController.Unit.MaxHp}";
        playerHealthBarFill.fillAmount = (float)turnManager.PlayerUnitController.Unit.CurrentHp / turnManager.PlayerUnitController.Unit.MaxHp;
        playerTpText.text = $"TP: {turnManager.PlayerUnitController.Unit.CurrentTp}%";
        playerTpBarFill.fillAmount = (float)turnManager.PlayerUnitController.Unit.CurrentTp / turnManager.PlayerUnitController.Unit.MaxTp;
        enemyHealthText.text = $"HP: {turnManager.EnemyUnitController.Unit.CurrentHp} / {turnManager.EnemyUnitController.Unit.MaxHp}";
        enemyHealthBarFill.fillAmount = (float)turnManager.EnemyUnitController.Unit.CurrentHp / turnManager.EnemyUnitController.Unit.MaxHp;
        enemyName.text = $"{turnManager.EnemyUnitController.Unit.UnitName}:";
        combatTextBox.text = "";

        UpdateEnemyHealth();
        UpdatePlayerTp();
        UpdatePlayerHealth();

        CombatTextEvent += DisplayCombatText;
        UpdateCombatHUDPlayerHp += UpdatePlayerHealth;
        UpdateCombatHUDPlayerTp += UpdatePlayerTp;
        UpdateCombatHUDEnemyHp += UpdateEnemyHealth;
    }
    /// <summary>
    /// Updates the buttons to be disabled if it is not the player's turn.
    /// </summary>
    private void Update()
    {
        DisableButtons(!turnManager.isPlayerTurn);
    }
    /// <summary>
    /// Removes the methods from the events.
    /// </summary>
    private void OnDisable()
    {
        CombatTextEvent -= DisplayCombatText;
        UpdateCombatHUDPlayerHp -= UpdatePlayerHealth;
        UpdateCombatHUDPlayerTp -= UpdatePlayerTp;
        UpdateCombatHUDEnemyHp -= UpdateEnemyHealth;
        UpdateCombatHUD -= UpdateCombatHUDs;
    }

    #endregion

    #region === Methods =================================================================

    /// <summary>
    /// Updates the player's and enemy's health and Tp.
    /// </summary>
    private void UpdateCombatHUDs()
    {
        UpdatePlayerHealth();
        UpdatePlayerTp();
        UpdateEnemyHealth();
    }
    /// <summary>
    /// Disables or enables the buttons.
    /// </summary>
    /// <param name="disabled"></param>
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
    /// <summary>
    /// Displays combat text in the combat text box.
    /// </summary>
    /// <param name="text"></param>
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
                itemPanel.SetActive(false);
                returnButton.gameObject.SetActive(false);
                returnButton.interactable = false;
                optionsPanel.SetActive(true);
                CombatTextEvent.Invoke($"<b>Usou {item.ItemName}!</b>");
                //turnManager.isPlayerTurn = false;
                TakenAction.Invoke();
            });
        }
    }
    /// <summary>
    /// Goes back to the options panel.
    /// </summary>
    public void Return()
    {
        itemPanel.SetActive(false);
        specialPanel.SetActive(false);
        optionsPanel.SetActive(true);
        returnButton.gameObject.SetActive(false);
        returnButton.interactable = false;
    }
    /// <summary>
    /// Shows the player's special attacks options.
    /// </summary>
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

        foreach (var specialAction in specials.specialsList)
        {
            returnButton.gameObject.SetActive(true);
            returnButton.interactable = true;
            var button = Instantiate(buttonPrefab, specialPanel.transform);
            button.GetComponentInChildren<TextMeshProUGUI>().text = specialAction.specialName;
            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                specials.UseSpecial(specialAction);
            });
        }
    }

    #endregion
}