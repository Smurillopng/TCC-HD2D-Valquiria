using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.VFX;

/// <summary>
/// Responsible for controlling the combat UI and the player's combat actions.
/// </summary>
/// <remarks>
/// This class contains the player's health bar, the player's Tp bar, the player's charges bar, the player's charged stance vfx, the enemy's health bar, the enemy's name, the player's attack button, the player's defend button, the player's item button, the player's flee button, the player's attack button text, the player's defend button text, the player's item button text, the player's flee button text, the player's attack button event, the player's defend button event, the player's item button event, the player's flee button event, the player's attack button text event, the player's defend button text event, the player's item button text event, the player's flee button text event, the player's attack button text, the player's defend button text, the player's item button text, and the player's flee button text.
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

    [SerializeField]
    [Tooltip("The fill image of the player's charges bar")]
    private Image playerCharges;

    [SerializeField]
    [Tooltip("The vfx for the player charged stance")]
    private VisualEffect playerChargedVfx;

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

    [SerializeField]
    [Tooltip("The button for charging up the player's basic attack.")]
    private Button chargeButton;

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

    private bool _charging = false;

    #endregion

    #region === Unity Methods ===========================================================

    /// <summary>Enables the various event handlers for the combat HUD.</summary>
    private void OnEnable()
    {
        CombatTextEvent += DisplayCombatText;
        UpdateCombatHUDPlayerHp += UpdatePlayerHealth;
        UpdateCombatHUDPlayerTp += UpdatePlayerTp;
        UpdateCombatHUDEnemyHp += UpdateEnemyHealth;
        UpdateCombatHUD += UpdateCombatHUDs;
        TakenAction += UpdateCharges;
    }
    /// <summary>Updates the combat HUD with the current player and enemy health, TP, and charges.</summary>
    /// <remarks>Also subscribes to various events to update the HUD as the combat progresses.</remarks>
    private void Start()
    {
        playerHealthText.text = $"HP: {turnManager.PlayerUnitController.Unit.CurrentHp} / {turnManager.PlayerUnitController.Unit.MaxHp}";
        playerHealthBarFill.fillAmount = (float)turnManager.PlayerUnitController.Unit.CurrentHp / turnManager.PlayerUnitController.Unit.MaxHp;
        playerTpText.text = $"TP: {turnManager.PlayerUnitController.Unit.CurrentTp}%";
        playerTpBarFill.fillAmount = (float)turnManager.PlayerUnitController.Unit.CurrentTp / turnManager.PlayerUnitController.Unit.MaxTp;
        playerCharges.fillAmount = 0;
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
    /// <summary>Updates the UI elements based on the current state of the game.</summary>
    /// <remarks>
    /// Disables the buttons if it's not the player's turn or if the player is charging a special attack.
    /// Enables the buttons if it's the player's turn and they're not charging a special attack.
    /// Stops the player charged VFX if the player has no charges left.
    /// </remarks>
    private void Update()
    {
        DisableButtons(!turnManager.isPlayerTurn);
        if (_charging)
        {
            specialButton.interactable = false;
            itemButton.interactable = false;
        }

        if (turnManager.isPlayerTurn && !_charging)
        {
            specialButton.interactable = true;
            itemButton.interactable = true;
        }
        if (turnManager.PlayerUnitController.Charges == 0)
        {
            playerChargedVfx.Stop();
            _charging = false;
        }
    }
    /// <summary>Unsubscribes from events when the script is disabled.</summary>
    private void OnDisable()
    {
        CombatTextEvent -= DisplayCombatText;
        UpdateCombatHUDPlayerHp -= UpdatePlayerHealth;
        UpdateCombatHUDPlayerTp -= UpdatePlayerTp;
        UpdateCombatHUDEnemyHp -= UpdateEnemyHealth;
        UpdateCombatHUD -= UpdateCombatHUDs;
        TakenAction -= UpdateCharges;
    }

    #endregion

    #region === Methods =================================================================

    /// <summary>Updates the combat HUDs for the player and enemy.</summary>
    /// <remarks>
    /// This method updates the player's health and TP, as well as the enemy's health.
    /// </remarks>
    private void UpdateCombatHUDs()
    {
        UpdatePlayerHealth();
        UpdatePlayerTp();
        UpdateEnemyHealth();
    }
    /// <summary>Updates the player's charges if the player's turn has started and the player is not currently charging.</summary>
    /// <remarks>Increments the fill amount of the player's charges by 0.25f if it is less than 1.</remarks>
    private void UpdateCharges()
    {
        if (playerCharges.fillAmount < 1 && turnManager.isPlayerTurn && !_charging)
            playerCharges.fillAmount += 0.25f;
    }
    /// <summary>Disables or enables the buttons for the player's actions.</summary>
    /// <param name="disabled">True to disable the buttons, false to enable them.</param>
    /// <remarks>
    /// When the buttons are disabled, the attack, special, item, run, and charge buttons are hidden.
    /// When the buttons are enabled, the attack, special, and item buttons are shown, as well as the run button.
    /// The charge button is shown only if the player has charges remaining.
    /// </remarks>
    private void DisableButtons(bool disabled)
    {
        switch (disabled)
        {
            case true:
                attackButton.gameObject.SetActive(false);
                specialButton.gameObject.SetActive(false);
                itemButton.gameObject.SetActive(false);
                runButton.gameObject.SetActive(false);
                chargeButton.gameObject.SetActive(false);
                break;
            case false:
                attackButton.gameObject.SetActive(true);
                specialButton.gameObject.SetActive(true);
                itemButton.gameObject.SetActive(true);
                runButton.gameObject.SetActive(true);
                chargeButton.gameObject.SetActive(playerCharges.fillAmount != 0);
                break;
        }
    }
    /// <summary>Updates the player's health UI.</summary>
    /// <remarks>
    /// This method updates the player's health text and health bar fill based on the current and maximum health of the player's unit.
    /// </remarks>
    public void UpdatePlayerHealth()
    {
        if (playerHealthText == null || playerHealthBarFill == null) return;
        playerHealthText.text = $"HP: {turnManager.PlayerUnitController.Unit.CurrentHp} / {turnManager.PlayerUnitController.Unit.MaxHp}";
        playerHealthBarFill.fillAmount = (float)turnManager.PlayerUnitController.Unit.CurrentHp / turnManager.PlayerUnitController.Unit.MaxHp;
    }
    /// <summary>Updates the enemy's health text and health bar fill.</summary>
    /// <remarks>
    /// If either the enemyHealthText or enemyHealthBarFill is null, this method does nothing.
    /// </remarks>
    public void UpdateEnemyHealth()
    {
        if (enemyHealthText == null || enemyHealthBarFill == null) return;
        enemyHealthText.text = $"{turnManager.EnemyUnitController.Unit.CurrentHp} / {turnManager.EnemyUnitController.Unit.MaxHp}";
        enemyHealthBarFill.fillAmount = (float)turnManager.EnemyUnitController.Unit.CurrentHp / turnManager.EnemyUnitController.Unit.MaxHp;
    }
    /// <summary>Updates the player's TP text and bar fill.</summary>
    /// <remarks>
    /// If either the player TP text or bar fill is null, this method does nothing.
    /// The player's TP text is updated to display the current TP percentage.
    /// The player's TP bar fill is updated to reflect the current TP percentage.
    /// If the player's TP is at its maximum, the player TP text is updated to display "TP: MAX".
    /// </remarks>
    private void UpdatePlayerTp()
    {
        if (playerTpText == null || playerTpBarFill == null) return;
        playerTpText.text = $"TP: {turnManager.PlayerUnitController.Unit.CurrentTp}%";
        playerTpBarFill.fillAmount = (float)turnManager.PlayerUnitController.Unit.CurrentTp / turnManager.PlayerUnitController.Unit.MaxTp;
        if (turnManager.PlayerUnitController.Unit.CurrentTp == turnManager.PlayerUnitController.Unit.MaxTp)
            playerTpText.text = "TP: MAX";
    }
    /// <summary>Displays combat text in the combat text box.</summary>
    /// <param name="text">The text to display.</param>
    /// <remarks>
    /// If the combat text box is null, the text will not be displayed.
    /// </remarks>
    private void DisplayCombatText(string text)
    {
        if (combatTextBox != null)
            StartCoroutine(DisplayCombatTextCoroutine(text));
    }
    /// <summary>Displays combat text for a set amount of time.</summary>
    /// <param name="text">The text to display.</param>
    /// <returns>An IEnumerator that waits for a set amount of time before clearing the text.</returns>
    private IEnumerator DisplayCombatTextCoroutine(string text)
    {
        combatTextBox.text = text;
        yield return new WaitForSeconds(combatTextTimer);
        combatTextBox.text = "";
    }
    /// <summary>Displays the player's inventory of consumable items and allows the player to use them.</summary>
    /// <remarks>
    /// If the player has no consumable items, a message is displayed and the method returns.
    /// Otherwise, the options panel is hidden and the item panel is shown.
    /// If the item panel already has child objects, they are destroyed.
    /// For each consumable item in the player's inventory, a button is instantiated in the item panel.
    /// When the button is clicked, the item is used, the player and enemy health and TP are updated, the item panel is hidden,
    /// the return button is hidden and disabled, the options panel is shown, and a message is displayed indicating that the item was used
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
                item.Use();
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
    /// <summary>Hides the item and special panels and shows the options panel. Disables the return button.</summary>
    /// <remarks>This method is typically called when the user wants to return to the options panel after viewing an item or special panel.</remarks>
    public void Return()
    {
        itemPanel.SetActive(false);
        specialPanel.SetActive(false);
        optionsPanel.SetActive(true);
        returnButton.gameObject.SetActive(false);
        returnButton.interactable = false;
    }
    /// <summary>Displays the special panel and populates it with buttons for each special action.</summary>
    /// <remarks>
    /// This method sets the options panel to inactive and the special panel to active. It then destroys any existing child objects
    /// in the special panel and creates a new button for each special action in the specials list. Each button is given the name of
    /// the corresponding special action and is set up to call the UseSpecial method of the specials object when clicked.
    /// </remarks>
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
            button.GetComponent<Button>().onClick.AddListener(() => specials.UseSpecial(specialAction));
        }
    }
    /// <summary>Charges the player's unit.</summary>
    /// <remarks>
    /// If the player has at least 0.25 charges, the player's charges are decreased by 0.25 and the player's unit is charged.
    /// The charging effect is updated based on the number of charges the player has.
    /// </remarks>
    public void Charge()
    {
        if (playerCharges.fillAmount >= 0.25f)
        {
            playerCharges.fillAmount -= 0.25f;
            turnManager.PlayerUnitController.Charges += 1;
            _charging = true;
            switch (turnManager.PlayerUnitController.Charges)
            {
                case 1:
                    playerChargedVfx.Play();
                    break;
                case 2:
                    playerChargedVfx.SetVector4("DropColor", new Vector4(0f, 0f, 1f, 1));
                    break;
                case 3:
                    playerChargedVfx.SetVector4("DropColor", new Vector4(0f, 1f, 0f, 1));
                    break;
                case 4:
                    playerChargedVfx.SetVector4("DropColor", new Vector4(1f, 0f, 0f, 1));
                    break;
            }
        }
    }

    #endregion
}