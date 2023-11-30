// Created by Sérgio Murillo da Costa Faria

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.VFX;

[HideMonoScript]
public class PlayerCombatHUD : MonoBehaviour
{
    #region === Variables ===============================================================

    [FoldoutGroup("Player Combat HUD")]
    [BoxGroup("Player Combat HUD/HUD Elements")]
    [SerializeField, Tooltip("The fill image of the player's health bar.")]
    private Image playerHealthBarFill;

    [BoxGroup("Player Combat HUD/HUD Elements")]
    [SerializeField, Tooltip("The text displaying the player's current health.")]
    private TMP_Text playerHealthText;

    [BoxGroup("Player Combat HUD/HUD Elements")]
    [SerializeField, Tooltip("The fill image of the player's Tp bar.")]
    private Image playerTpBarFill;

    [BoxGroup("Player Combat HUD/HUD Elements")]
    [SerializeField, Tooltip("The text displaying the player's current Tp.")]
    private TMP_Text playerTpText;

    [BoxGroup("Player Combat HUD/HUD Elements")]
    [SerializeField]
    private TMP_Text experienceBarText;

    [BoxGroup("Player Combat HUD/HUD Elements")]
    [SerializeField]
    [Tooltip("The fill image of the player's charges bar")]
    public Image playerCharges;

    [BoxGroup("Player Combat HUD/HUD Elements")]
    [SerializeField]
    [Tooltip("The vfx for the player charged stance")]
    private VisualEffect playerChargedVfx;

    [BoxGroup("Player Combat HUD/Enemy HUD Elements")]
    [SerializeField, Tooltip("The name text of the enemy.")]
    private TMP_Text enemyName;

    [BoxGroup("Player Combat HUD/Enemy HUD Elements")]
    [SerializeField, Tooltip("The fill image of the enemy's health bar.")]
    private Image enemyHealthBarFill;

    [BoxGroup("Player Combat HUD/Enemy HUD Elements")]
    [SerializeField, Tooltip("The text displaying the enemy's current health.")]
    private TMP_Text enemyHealthText;

    [BoxGroup("Player Combat HUD/Combat Text Box")]
    [SerializeField, Tooltip("The text box displaying combat information.")]
    private TMP_Text combatTextBox;

    [BoxGroup("Player Combat HUD/Combat Text Box")]
    [SerializeField, Tooltip("The text box displaying combat information.")]
    private GameObject textBoxObject;

    [BoxGroup("Player Combat HUD/Combat Panels")]
    [SerializeField, Tooltip("The prefab for combat buttons.")]
    private GameObject buttonPrefab;

    [BoxGroup("Player Combat HUD/Combat Panels")]
    [SerializeField, Tooltip("The panel containing combat options.")]
    private GameObject optionsPanel;

    [BoxGroup("Player Combat HUD/Combat Panels")]
    [SerializeField, Tooltip("The panel containing special combat options.")]
    private GameObject specialPanel;

    [BoxGroup("Player Combat HUD/Combat Panels")]
    [SerializeField, Tooltip("The panel containing special combat options.")]
    private GameObject specialPanelContainer;

    [BoxGroup("Player Combat HUD/Combat Panels")]
    [SerializeField, Tooltip("The panel containing item options.")]
    private GameObject itemPanel;

    [BoxGroup("Player Combat HUD/Combat Panels")]
    [SerializeField, Tooltip("The panel containing item options.")]
    private GameObject itemPanelContainer;

    [BoxGroup("Player Combat HUD/Buttons")]
    [SerializeField, Tooltip("The button for attacking the enemy.")]
    private Button attackButton;

    [BoxGroup("Player Combat HUD/Buttons")]
    [SerializeField, Tooltip("The button for using a special attack on the enemy.")]
    private Button specialButton;

    [BoxGroup("Player Combat HUD/Buttons")]
    [SerializeField, Tooltip("The button for using an item in combat.")]
    private Button itemButton;

    [BoxGroup("Player Combat HUD/Buttons")]
    [SerializeField, Tooltip("The button for attempting to run away from combat.")]
    private Button runButton;

    [BoxGroup("Player Combat HUD/Buttons")]
    [SerializeField, Tooltip("The button for attempting to run away from combat.")]
    private Button returnButton;

    [BoxGroup("Player Combat HUD/Buttons")]
    [SerializeField, Tooltip("The button for charging up the player's basic attack.")]
    private Button chargeButton;

    [BoxGroup("Player Combat HUD/Buttons")]
    [SerializeField, Tooltip("The button for discharging the player's basic attack.")]
    private Button dischargeButton;

    public static UnityAction TakenAction;
    public static UnityAction<string, float> CombatTextEvent;
    public static UnityAction UpdateCombatHUDPlayerHp;
    public static UnityAction UpdateCombatHUDPlayerTp;
    public static UnityAction UpdateCombatHUDEnemyHp;
    public static UnityAction UpdateCombatHUD;
    public static UnityAction<bool> ForceDisableButtons;
    public static UnityAction UpdateExperience;

    [BoxGroup("Player Combat HUD/Debug Info")]
    [SerializeField, Tooltip("The manager for controlling turns in combat.")]
    private TurnManager turnManager;

    [BoxGroup("Player Combat HUD/Debug Info")]
    [SerializeField, Tooltip("The collection of special attacks available to the player.")]
    private Specials specials;

    public GameObject SpecialPanel => specialPanel;
    public GameObject OptionsPanel => optionsPanel;
    public Button ReturnButton => returnButton;

    private bool _charging;
    public static int _usedItemValue;

    #endregion ==========================================================================

    #region === Unity Methods ===========================================================

    /// <summary>Enables the various event handlers for the combat HUD.</summary>
    private void OnEnable()
    {
        CombatTextEvent += DisplayCombatText;
        UpdateCombatHUDPlayerHp += UpdatePlayerHealth;
        UpdateCombatHUDPlayerTp += UpdatePlayerTp;
        UpdateCombatHUDEnemyHp += UpdateEnemyHealth;
        UpdateExperience += UpdateXp;
        UpdateCombatHUD += UpdateCombatHUDs;
        TakenAction += UpdateCharges;
        ForceDisableButtons += DisableButtons;
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

        textBoxObject.SetActive(false);
        chargeButton.gameObject.SetActive(false);
    }
    /// <summary>Updates the UI elements based on the current state of the game.</summary>
    /// <remarks>
    /// Disables the buttons if it's not the player's turn or if the player is charging a special attack.
    /// Enables the buttons if it's the player's turn and they're not charging a special attack.
    /// Stops the player charged VFX if the player has no charges left.
    /// </remarks>
    private void Update()
    {
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

        if (playerCharges.fillAmount < 0.25f)
            chargeButton.interactable = false;
        else
            chargeButton.interactable = true;

        if (turnManager.PlayerUnitController.Charges > 0)
            dischargeButton.interactable = true;
        else
            dischargeButton.interactable = false;
    }
    /// <summary>Unsubscribes from events when the script is disabled.</summary>
    private void OnDisable()
    {
        CombatTextEvent -= DisplayCombatText;
        UpdateCombatHUDPlayerHp -= UpdatePlayerHealth;
        UpdateCombatHUDPlayerTp -= UpdatePlayerTp;
        UpdateCombatHUDEnemyHp -= UpdateEnemyHealth;
        UpdateExperience -= UpdateXp;
        UpdateCombatHUD -= UpdateCombatHUDs;
        TakenAction -= UpdateCharges;
        ForceDisableButtons -= DisableButtons;
    }

    #endregion ==========================================================================

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
    private static IEnumerator UpdateHealth(UnitController unit, TMP_Text text, Image fillImage)
    {
        text.text = $"HP: {unit.Unit.CurrentHp} / {unit.Unit.MaxHp}";
        if (fillImage.fillAmount != (float)unit.Unit.CurrentHp / unit.Unit.MaxHp)
        {
            var fillAmount = fillImage.fillAmount;
            var targetFillAmount = (float)unit.Unit.CurrentHp / unit.Unit.MaxHp;
            var time = 0f;
            while (time < 1f)
            {
                time += Time.deltaTime;
                fillImage.fillAmount = Mathf.Lerp(fillAmount, targetFillAmount, time);
                yield return null;
            }
        }
    }
    private static IEnumerator UpdateTP(UnitController unit, TMP_Text text, Image fillImage)
    {
        text.text = $"TP: {unit.Unit.CurrentTp}%";
        if (fillImage.fillAmount != (float)unit.Unit.CurrentHp / unit.Unit.MaxHp)
        {
            var fillAmount = fillImage.fillAmount;
            var targetFillAmount = (float)unit.Unit.CurrentTp / unit.Unit.MaxTp;
            var time = 0f;
            while (time < 1f)
            {
                time += Time.deltaTime;
                fillImage.fillAmount = Mathf.Lerp(fillAmount, targetFillAmount, time);
                yield return null;
            }
            if (unit.Unit.CurrentTp == unit.Unit.MaxTp)
                text.text = "TP: MAX";
        }
    }

    private void UpdateXp()
    {
        experienceBarText.gameObject.SetActive(true);
        experienceBarText.text = turnManager.PlayerUnitController.Unit.Experience + turnManager.EnemyUnitController.Unit.ExperienceDrop >
                                 turnManager.PlayerUnitController
                                     .Unit.StatsTables.First(statGroup =>
                                         statGroup.Level == turnManager.PlayerUnitController.Unit.Level + 1)
                                     .Experience ? $"Aumento de Nível => {turnManager.PlayerUnitController.Unit.Level + 1}\n+{turnManager.EnemyUnitController.Unit.ExperienceDrop} XP" : $"+{turnManager.EnemyUnitController.Unit.ExperienceDrop} XP";
    }
    /// <summary>Updates the player's charges if the player's turn has started and the player is not currently charging.</summary>
    /// <remarks>Increments the fill amount of the player's charges by 0.25f if it is less than 1.</remarks>
    public void UpdateCharges()
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
                dischargeButton.gameObject.SetActive(false);
                break;
            case false:
                attackButton.gameObject.SetActive(true);
                specialButton.gameObject.SetActive(true);
                itemButton.gameObject.SetActive(true);
                if (!turnManager.EnemyUnitController.Unit.IsDangerous) runButton.gameObject.SetActive(true);
                chargeButton.gameObject.SetActive(playerCharges.fillAmount != 0);
                dischargeButton.gameObject.SetActive(playerCharges.fillAmount != 0);
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
        StartCoroutine(UpdateHealth(turnManager.PlayerUnitController, playerHealthText, playerHealthBarFill));
    }
    /// <summary>Updates the enemy's health text and health bar fill.</summary>
    /// <remarks>
    /// If either the enemyHealthText or enemyHealthBarFill is null, this method does nothing.
    /// </remarks>
    public void UpdateEnemyHealth()
    {
        if (enemyHealthText == null || enemyHealthBarFill == null) return;
        StartCoroutine(UpdateHealth(turnManager.EnemyUnitController, enemyHealthText, enemyHealthBarFill));
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
        StartCoroutine(UpdateTP(turnManager.PlayerUnitController, playerTpText, playerTpBarFill));
    }
    /// <summary>Displays combat text in the combat text box.</summary>
    /// <param name="text">The text to display.</param>
    /// <remarks>
    /// If the combat text box is null, the text will not be displayed.
    /// </remarks>
    private void DisplayCombatText(string text, float duration)
    {
        StartCoroutine(DisplayCombatTextCoroutine(text, duration));
    }
    /// <summary>Displays combat text for a set amount of time.</summary>
    /// <param name="text">The text to display.</param>
    /// <returns>An IEnumerator that waits for a set amount of time before clearing the text.</returns>
    private IEnumerator DisplayCombatTextCoroutine(string text, float duration)
    {
        textBoxObject.SetActive(true);
        combatTextBox.text = text;
        yield return new WaitForSeconds(duration);
        combatTextBox.text = "";
        textBoxObject.SetActive(false);
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
            CombatTextEvent.Invoke("<b>Sem itens!</b>", 2f);
            return;
        }

        optionsPanel.SetActive(false);
        itemPanel.SetActive(true);

        if (itemPanelContainer.transform.childCount > 0)
        {
            foreach (Transform child in itemPanelContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (var item in InventoryManager.Instance.Inventory.OfType<Consumable>())
        {
            returnButton.gameObject.SetActive(true);
            returnButton.interactable = true;
            var buttonObject = Instantiate(buttonPrefab, itemPanelContainer.transform);
            buttonObject.GetComponentInChildren<TextMeshProUGUI>().text = item.ItemName;
            var button = buttonObject.GetComponent<Button>();
            buttonObject.AddComponent<EventTrigger>();
            var trigger = buttonObject.GetComponent<EventTrigger>();
            button.onClick.AddListener(() =>
            {
                item.Use();
                _usedItemValue = item.EffectValue;
                UpdatePlayerHealth();
                UpdatePlayerTp();
                UpdateEnemyHealth();
                itemPanel.SetActive(false);
                returnButton.gameObject.SetActive(false);
                returnButton.interactable = false;
                optionsPanel.SetActive(true);
                CombatTextEvent.Invoke($"<b>Usou {item.ItemName}!</b>", 2f);
                //turnManager.isPlayerTurn = false;
                TakenAction.Invoke();
                ForceDisableButtons.Invoke(true);
            });
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener(_ =>
            {
                // Add code here to show the text when the mouse hovers over the button
                textBoxObject.SetActive(true);
                combatTextBox.text = $"{item.ItemDescription}";
            });
            var entry2 = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            entry2.callback.AddListener(_ =>
            {
                // Add code here to hide the text when the mouse stops hovering over the button
                textBoxObject.SetActive(false);
                combatTextBox.text = "";
            });
            trigger.triggers.Add(entry);
            trigger.triggers.Add(entry2);
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
        List<Button> specialButtons = new List<Button>();

        if (specialPanelContainer.transform.childCount > 0)
        {
            foreach (Transform child in specialPanelContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (var specialAction in specials.specialsList)
        {
            returnButton.gameObject.SetActive(true);
            returnButton.interactable = true;
            var button = Instantiate(buttonPrefab, specialPanelContainer.transform);
            button.GetComponentInChildren<TextMeshProUGUI>().text = specialAction.specialName;
            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                specials.UseSpecial(specialAction);
                _usedItemValue = specialAction.specialHeal;
                textBoxObject.SetActive(false);
            });
            button.AddComponent<EventTrigger>();
            var trigger = button.GetComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener(_ =>
            {
                // Add code here to show the text when the mouse hovers over the button
                textBoxObject.SetActive(true);
                combatTextBox.text = $"{specialAction.specialDescription}\n[ Custo de TP: {specialAction.specialCost} ]";
            });
            var entry2 = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            entry2.callback.AddListener(_ =>
            {
                // Add code here to hide the text when the mouse stops hovering over the button
                textBoxObject.SetActive(false);
                combatTextBox.text = "";
            });
            trigger.triggers.Add(entry);
            trigger.triggers.Add(entry2);
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
                    playerChargedVfx.Reinit();
                    playerChargedVfx.SetVector4("DropColor", new Vector4(0f, 0f, 1f, 1));
                    break;
                case 3:
                    playerChargedVfx.Reinit();
                    playerChargedVfx.SetVector4("DropColor", new Vector4(0f, 1f, 0f, 1));
                    break;
                case 4:
                    playerChargedVfx.Reinit();
                    playerChargedVfx.SetVector4("DropColor", new Vector4(1f, 0f, 0f, 1));
                    break;
            }
        }
    }

    public void Discharge()
    {
        if (turnManager.PlayerUnitController.Charges > 0)
        {
            turnManager.PlayerUnitController.Charges -= 1;
            playerCharges.fillAmount += 0.25f;
            switch (turnManager.PlayerUnitController.Charges)
            {
                case 0:
                    playerChargedVfx.Stop();
                    break;
                case 1:
                    playerChargedVfx.Reinit();
                    playerChargedVfx.SetVector4("DropColor", new Vector4(0f, 0f, 1f, 1));
                    break;
                case 2:
                    playerChargedVfx.Reinit();
                    playerChargedVfx.SetVector4("DropColor", new Vector4(0f, 1f, 0f, 1));
                    break;
                case 3:
                    playerChargedVfx.Reinit();
                    playerChargedVfx.SetVector4("DropColor", new Vector4(1f, 0f, 0f, 1));
                    break;
            }
        }
        else if (turnManager.PlayerUnitController.Charges == 0)
        {
            _charging = false;
        }
    }

    #endregion ==========================================================================
}