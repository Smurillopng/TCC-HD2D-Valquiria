// Created by Sérgio Murillo da Costa Faria

using System;
using System.Collections.Generic;
using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[HideMonoScript]
public class InventoryUI : MonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Inventory UI")]
    [BoxGroup("Inventory UI/Panels")][SerializeField] private GameObject inventoryPanel;
    [BoxGroup("Inventory UI/Panels")][SerializeField] private GameObject bagPanel;
    [BoxGroup("Inventory UI/Panels")][SerializeField] private GameObject equipmentPanel;
    [BoxGroup("Inventory UI/Panels")][SerializeField] private GameObject itemDisplayPanel;
    [BoxGroup("Inventory UI/Panels")][SerializeField] private GameObject statusPanel;
    [BoxGroup("Inventory UI/Panels")][SerializeField] private GameObject exitPanel;

    [BoxGroup("Inventory UI/Equipment Slots")]
    [SerializeField]
    private Image headSlot;

    [BoxGroup("Inventory UI/Equipment Slots")]
    [SerializeField]
    private Image chestSlot;

    [BoxGroup("Inventory UI/Equipment Slots")]
    [SerializeField]
    private Image legsSlot;

    [BoxGroup("Inventory UI/Equipment Slots")]
    [SerializeField]
    private Image weaponSlot;

    [BoxGroup("Inventory UI/Equipment Slots")]
    [SerializeField]
    private Image runeSlot;

    [BoxGroup("Inventory UI/Status")][SerializeField] private TMP_Text playerLvl;
    [BoxGroup("Inventory UI/Status")][SerializeField] private Image playerHealthBarFill;
    [BoxGroup("Inventory UI/Status")][SerializeField] private TMP_Text playerHealthText;
    [BoxGroup("Inventory UI/Status")][SerializeField] private Image playerTpFill;
    [BoxGroup("Inventory UI/Status")][SerializeField] private TMP_Text playerTpText;
    [BoxGroup("Inventory UI/Status")][SerializeField] private Image playerXpBarFill;
    [BoxGroup("Inventory UI/Status")][SerializeField] private TMP_Text playerXpText;
    [BoxGroup("Inventory UI/Status")][SerializeField] private TMP_Text playerAttack;
    [BoxGroup("Inventory UI/Status")][SerializeField] private TMP_Text playerDefence;
    [BoxGroup("Inventory UI/Status")][SerializeField] private TMP_Text playerSpeed;
    [BoxGroup("Inventory UI/Status")][SerializeField] private TMP_Text playerLuck;
    [BoxGroup("Inventory UI/Status")][SerializeField] private TMP_Text playerDexterity;
    [BoxGroup("Inventory UI/Status")][SerializeField] private GameObject lvlUpAttributesButtons;
    [BoxGroup("Inventory UI/Status")][SerializeField] private TMP_Text attributePointsText;
    [BoxGroup("Inventory UI/Status")][SerializeField] private TMP_Text availableAttributePointsText;

    [BoxGroup("Inventory UI/Left Bars")][SerializeField] private Image topLeftPlayerHealthBarFill;
    [BoxGroup("Inventory UI/Left Bars")][SerializeField] private TMP_Text topLeftPlayerHealthText;
    [BoxGroup("Inventory UI/Left Bars")][SerializeField] private Image topLeftPlayerTpFill;
    [BoxGroup("Inventory UI/Left Bars")][SerializeField] private TMP_Text topLeftPlayerTpText;
    [BoxGroup("Inventory UI/Left Bars")][SerializeField] private Image topLeftPlayerXpBarFill;
    [BoxGroup("Inventory UI/Left Bars")][SerializeField] private TMP_Text topLeftPlayerXpText;

    [BoxGroup("Inventory UI/Item Display")][SerializeField] private TMP_Text itemName;
    [BoxGroup("Inventory UI/Item Display")][SerializeField] private TMP_Text itemDescription;
    [BoxGroup("Inventory UI/Item Display")][SerializeField] private Image itemIcon;
    [BoxGroup("Inventory UI/Item Display")][SerializeField] private TMP_Text itemQuantity;
    [BoxGroup("Inventory UI/Item Display")][SerializeField] private Button itemUseButton;

    [BoxGroup("Inventory UI/Item Scroll")][SerializeField] private RectTransform bagRectTransform;
    [BoxGroup("Inventory UI/Item Scroll")][SerializeField] private ScrollRect bagScrollRect;
    [BoxGroup("Inventory UI/Item Scroll")][SerializeField] private float scrollOffset;

    [BoxGroup("Inventory UI/External References")]
    [SerializeField]
    private Button autoHealButton;

    [BoxGroup("Inventory UI/External References")]
    [SerializeField]
    private Unit playerUnit;

    [BoxGroup("Inventory UI/External References")]
    [SerializeField]
    private GameObject itemPrefab;

    [BoxGroup("Inventory UI/External References")]
    [SerializeField]
    private BoolVariable isInventoryOpen;

    [BoxGroup("Inventory UI/Debug")]
    [SerializeField, ReadOnly]
    private bool updatedStatus;

    [BoxGroup("Inventory UI/Debug")]
    [SerializeField, ReadOnly]
    private InventoryManager inventoryManager;

    public Unit PlayerUnit => playerUnit;

    private bool _gameStarted, _tutorialFinished;
    private int totalAttack, totalDefence, totalSpeed, totalLuck, totalDexterity;
    private QuickSaveReader _reader;
    #endregion ==========================================================================

    #region === Unity Methods ===========================================================
    private void Start()
    {
        inventoryManager = InventoryManager.Instance;
        if (QuickSaveReader.Create("GameInfo").Exists("GameStarted"))
            _gameStarted = QuickSaveReader.Create("GameInfo").Read<bool>("GameStarted");
        if (_gameStarted) return;
        _reader = QuickSaveReader.Create("GameSave");
        if (_reader.Exists("PlayerAttack")) playerUnit.Attack = _reader.Read<int>("PlayerAttack");
        if (_reader.Exists("PlayerDefence")) playerUnit.Defence = _reader.Read<int>("PlayerDefence");
        if (_reader.Exists("PlayerSpeed")) playerUnit.Speed = _reader.Read<int>("PlayerSpeed");
        if (_reader.Exists("PlayerLuck")) playerUnit.Luck = _reader.Read<int>("PlayerLuck");
        if (_reader.Exists("PlayerDexterity")) playerUnit.Dexterity = _reader.Read<int>("PlayerDexterity");
        if (_reader.Exists("Level")) playerUnit.Level = _reader.Read<int>("Level");
        if (_reader.Exists("Experience")) playerUnit.Experience = _reader.Read<int>("Experience");
        if (_reader.Exists("AttributesPoints")) playerUnit.AttributesPoints = _reader.Read<int>("AttributesPoints");
        if (_reader.Exists("PlayerMaxHealth")) playerUnit.MaxHp = _reader.Read<int>("PlayerMaxHealth");
        if (_reader.Exists("PlayerCurrentHealth")) playerUnit.CurrentHp = _reader.Read<int>("PlayerCurrentHealth");
        if (_reader.Exists("PlayerMaxTp")) playerUnit.MaxTp = _reader.Read<int>("PlayerMaxTp");
        if (_reader.Exists("PlayerCurrentTp")) playerUnit.CurrentTp = _reader.Read<int>("PlayerCurrentTp");
        _gameStarted = true;
        QuickSaveWriter.Create("GameInfo").Write("GameStarted", _gameStarted).Commit();
    }

    public void Update()
    {
        if (!_tutorialFinished)
        {
            _reader = QuickSaveReader.Create("GameSave");
            if (_reader.Exists("FinishedTutorial"))
                _tutorialFinished = _reader.Read<bool>("FinishedTutorial");
        }
        if (!SceneTransitioner.currentlyTransitioning && _tutorialFinished)
        {
            inventoryPanel.SetActive(isInventoryOpen.Value);
            if (isInventoryOpen.Value)
            {
                PlayerControls.Instance.ToggleDefaultControls(false);
                if (!updatedStatus)
                {
                    ResetPanels();
                    UpdatePlayerStatus(playerUnit);
                    updatedStatus = true;
                }
            }
            else
            {
                PlayerControls.Instance.ToggleDefaultControls(true);
                updatedStatus = false;
            }
        }
        else
        {
            inventoryPanel.SetActive(false);
            updatedStatus = false;
        }

        if (autoHealButton.gameObject.activeSelf) autoHealButton.interactable = playerUnit.CurrentTp >= 10 && playerUnit.CurrentHp < playerUnit.MaxHp;
        availableAttributePointsText.text = playerUnit.AttributesPoints > 0 ? $"Pontos de Atributos disponíveis: {playerUnit.AttributesPoints}" : string.Empty;
        lvlUpAttributesButtons.SetActive(playerUnit.AttributesPoints > 0);
        attributePointsText.gameObject.SetActive(playerUnit.AttributesPoints > 0);
        attributePointsText.text = $"Pontos de Atributos disponíveis: {playerUnit.AttributesPoints}";
        UpdateTopLeftBars();
    }
    #endregion ==========================================================================

    #region === Methods =================================================================
    public void ShowBagPanel()
    {
        inventoryPanel.SetActive(true);
        bagPanel.SetActive(true);
        UpdateBag(inventoryManager.Inventory);
        equipmentPanel.SetActive(false);
        itemDisplayPanel.SetActive(false);
    }

    public void ShowEquipmentPanel()
    {
        inventoryPanel.SetActive(true);
        bagPanel.SetActive(false);
        itemDisplayPanel.SetActive(false);
        UpdatePlayerStatus(playerUnit);
        UpdateEquipments(inventoryManager.EquipmentSlots);
        equipmentPanel.SetActive(true);
    }

    public void UpdateBag(List<IItem> inventory)
    {
        var totalHeight = 0f;
        foreach (Transform child in bagPanel.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (var item in inventory)
        {
            var itemObject = Instantiate(itemPrefab, bagPanel.transform);
            var uiScript = itemObject.GetComponent<ItemUI>();
            var buttonHeight = itemObject.GetComponent<RectTransform>().rect.height;
            uiScript.SetItem(item);
            var useButton = uiScript.useButton;
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(() => uiScript.DisplayItem(itemName, itemDescription, itemIcon, itemQuantity, itemDisplayPanel, item));
            useButton.onClick.AddListener(() => SelectDisplayAction(item, uiScript));
            totalHeight += buttonHeight;
        }

        bagRectTransform.sizeDelta = new Vector2(0, totalHeight - scrollOffset);
        bagScrollRect.verticalNormalizedPosition = 1;
    }

    public void DisplayEquipment(GameObject slot)
    {
        var image = slot.GetComponent<Image>();
        if (image.sprite == null) return;
        var equipments = Resources.LoadAll<Equipment>("Scriptable Objects/Items");
        foreach (var equipment in equipments)
        {
            if (equipment.ItemIcon == image.sprite)
            {
                itemName.text = equipment.ItemName;
                itemDescription.text = equipment.ItemDescription;
                itemDescription.text += UpdateEquipmentDescriptions(equipment);
                itemIcon.sprite = equipment.ItemIcon;
                itemQuantity.text = $"x{equipment.CurrentStack}";
                itemDisplayPanel.SetActive(true);
                SelectEquipmentDisplayAction(equipment);
            }
        }
    }

    private void SelectEquipmentDisplayAction(Equipment equipment)
    {
        if (inventoryManager.EquipmentSlots.Find(x => equipment != null && x.slotType == equipment.SlotType).equipItem != equipment)
        {
            itemUseButton.gameObject.GetComponentInChildren<TMP_Text>().text = "Equipar";
            itemUseButton.onClick.RemoveAllListeners();
            itemUseButton.onClick.AddListener(() => inventoryManager.Equip(equipment));
            itemUseButton.onClick.AddListener(ShowEquipmentPanel);
        }
        else
        {
            itemUseButton.gameObject.GetComponentInChildren<TMP_Text>().text = "Desequipar";
            itemUseButton.onClick.RemoveAllListeners();
            itemUseButton.onClick.AddListener(() => inventoryManager.Unequip(equipment));
            itemUseButton.onClick.AddListener(ShowEquipmentPanel);
        }
    }

    private void SelectDisplayAction(IItem currentItem, ItemUI script)
    {
        if (itemDisplayPanel.activeSelf)
        {
            if (currentItem.ItemType == ItemTyping.Consumable)
            {
                var consumable = currentItem as Consumable;
                if (consumable != null)
                {
                    itemUseButton.gameObject.GetComponentInChildren<TMP_Text>().text = "Usar";
                    switch (consumable.EffectType)
                    {
                        case ConsumableTypes.Damage:
                            itemUseButton.onClick.RemoveAllListeners();
                            itemUseButton.onClick.AddListener(() => itemDescription.text = "Não é possível usar esse item fora de combate.");
                            break;
                        case ConsumableTypes.Heal:
                            itemUseButton.onClick.RemoveAllListeners();
                            itemUseButton.onClick.AddListener(() => consumable.Use());
                            itemUseButton.onClick.AddListener(() => UpdateBag(inventoryManager.Inventory));
                            itemUseButton.onClick.AddListener(() => script.DisplayItem(itemName, itemDescription,
                                itemIcon, itemQuantity, itemDisplayPanel, currentItem));
                            itemUseButton.onClick.AddListener(() => UpdatePlayerStatus(playerUnit));
                            break;
                        case ConsumableTypes.IncreaseTp:
                            itemUseButton.onClick.RemoveAllListeners();
                            itemUseButton.onClick.AddListener(() => consumable.Use());
                            itemUseButton.onClick.AddListener(() => UpdateBag(inventoryManager.Inventory));
                            itemUseButton.onClick.AddListener(() => script.DisplayItem(itemName, itemDescription,
                                itemIcon, itemQuantity, itemDisplayPanel, currentItem));
                            itemUseButton.onClick.AddListener(() => UpdatePlayerStatus(playerUnit));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            else if (currentItem.ItemType == ItemTyping.Equipment)
            {
                var equipment = currentItem as Equipment;
                itemDescription.text += UpdateEquipmentDescriptions(equipment);
                if (inventoryManager.EquipmentSlots.Find(x => equipment != null && x.slotType == equipment.SlotType).equipItem != equipment)
                {
                    itemUseButton.gameObject.GetComponentInChildren<TMP_Text>().text = "Equipar";
                    itemUseButton.onClick.RemoveAllListeners();
                    itemUseButton.onClick.AddListener(() => inventoryManager.Equip(equipment));
                    itemUseButton.onClick.AddListener(ShowEquipmentPanel);
                }
                else
                {
                    itemUseButton.gameObject.GetComponentInChildren<TMP_Text>().text = "Desequipar";
                    itemUseButton.onClick.RemoveAllListeners();
                    itemUseButton.onClick.AddListener(() => inventoryManager.Unequip(equipment));
                    itemUseButton.onClick.AddListener(ShowEquipmentPanel);
                }
            }
        }
    }

    private string UpdateEquipmentDescriptions(Equipment equipment)
    {
        var text = "\n";
        if (equipment.StatusValue.Attack > 0) text += $"\nAtaque: +{equipment.StatusValue.Attack}";
        if (equipment.StatusValue.Defence > 0) text += $"\nDefesa: +{equipment.StatusValue.Defence}";
        if (equipment.StatusValue.Speed > 0) text += $"\nVelocidade: +{equipment.StatusValue.Speed}";
        if (equipment.StatusValue.Luck > 0) text += $"\nSorte: +{equipment.StatusValue.Luck}";
        if (equipment.StatusValue.Dexterity > 0) text += $"\nDestreza: +{equipment.StatusValue.Dexterity}";
        return text;
    }

    public void UpdateEquipments(List<EquipmentSlot> equipmentSlots)
    {
        if (equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Head).equipItem != null)
        {
            headSlot.sprite = equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Head).equipItem.ItemIcon;
            headSlot.type = Image.Type.Simple;
            headSlot.color = new Color(1, 1, 1, 1);
            headSlot.preserveAspect = true;
        }
        else
        {
            headSlot.sprite = null;
            headSlot.color = new Color(1, 1, 1, 0);
        }

        if (equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Chest).equipItem != null)
        {
            chestSlot.sprite = equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Chest).equipItem.ItemIcon;
            chestSlot.type = Image.Type.Simple;
            chestSlot.color = new Color(1, 1, 1, 1);
            chestSlot.preserveAspect = true;
        }
        else
        {
            chestSlot.sprite = null;
            chestSlot.color = new Color(1, 1, 1, 0);
        }

        if (equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Legs).equipItem != null)
        {
            legsSlot.sprite = equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Legs).equipItem.ItemIcon;
            legsSlot.type = Image.Type.Simple;
            legsSlot.color = new Color(1, 1, 1, 1);
            legsSlot.preserveAspect = true;
        }
        else
        {
            legsSlot.sprite = null;
            legsSlot.color = new Color(1, 1, 1, 0);
        }

        if (equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Weapon).equipItem != null)
        {
            weaponSlot.sprite = equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Weapon).equipItem.ItemIcon;
            weaponSlot.type = Image.Type.Simple;
            weaponSlot.color = new Color(1, 1, 1, 1);
            weaponSlot.preserveAspect = true;
        }
        else
        {
            weaponSlot.sprite = null;
            weaponSlot.color = new Color(1, 1, 1, 0);
        }

        if (equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Rune).equipItem != null)
        {
            runeSlot.sprite = equipmentSlots.Find(x => x.slotType == EquipmentSlotType.Rune).equipItem.ItemIcon;
            runeSlot.type = Image.Type.Simple;
            runeSlot.color = new Color(1, 1, 1, 1);
            runeSlot.preserveAspect = true;
        }
        else
        {
            runeSlot.sprite = null;
            runeSlot.color = new Color(1, 1, 1, 0);
        }
    }

    public void UpdatePlayerStatus(Unit unit)
    {
        playerLvl.text = $"Lv. {unit.Level}";
        playerHealthText.text = $"HP: {unit.CurrentHp} / {unit.MaxHp}";
        playerHealthBarFill.fillAmount = (float)unit.CurrentHp / unit.MaxHp;
        playerTpText.text = $"TP: {unit.CurrentTp} / {unit.MaxTp}";
        playerTpFill.fillAmount = (float)unit.CurrentTp / unit.MaxTp;
        playerXpText.text = $"XP: {unit.Experience} / {unit.StatsTables.Find(x => x.Level == unit.Level + 1).Experience}";
        playerXpBarFill.fillAmount = (float)unit.Experience / unit.StatsTables.Find(x => x.Level == unit.Level + 1).Experience;

        totalAttack = 0;
        totalDefence = 0;
        totalSpeed = 0;
        totalLuck = 0;
        totalDexterity = 0;

        foreach (var equipment in InventoryManager.Instance.EquipmentSlots)
        {
            if (equipment.equipItem != null)
            {
                totalAttack += equipment.equipItem.StatusValue.Attack;
                totalDefence += equipment.equipItem.StatusValue.Defence;
                totalSpeed += equipment.equipItem.StatusValue.Speed;
                totalLuck += equipment.equipItem.StatusValue.Luck;
                totalDexterity += equipment.equipItem.StatusValue.Dexterity;
            }
        }
        playerAttack.text = totalAttack > 0 ? $"Ataque: {unit.Attack} <color=#00FF00>(+{totalAttack})" : $"Ataque: {unit.Attack}</color>";
        playerDefence.text = totalDefence > 0 ? $"Defesa: {unit.Defence} <color=#00FF00>(+{totalDefence})" : $"Defesa: {unit.Defence}</color>";
        playerSpeed.text = totalSpeed > 0 ? $"Velocidade: {unit.Speed} <color=#00FF00>(+{totalSpeed})" : $"Velocidade: {unit.Speed}</color>";
        playerLuck.text = totalLuck > 0 ? $"Sorte: {unit.Luck} <color=#00FF00>(+{totalLuck})" : $"Sorte: {unit.Luck}</color>";
        playerDexterity.text = totalDexterity > 0 ? $"Destreza: {unit.Dexterity} <color=#00FF00>(+{totalDexterity})" : $"Destreza: {unit.Dexterity}</color>";
    }

    private void UpdateTopLeftBars()
    {
        topLeftPlayerHealthText.text = $"HP: {playerUnit.CurrentHp} / {playerUnit.MaxHp}";
        topLeftPlayerHealthBarFill.fillAmount = (float)playerUnit.CurrentHp / playerUnit.MaxHp;
        topLeftPlayerTpText.text = $"TP: {playerUnit.CurrentTp} / {playerUnit.MaxTp}";
        topLeftPlayerTpFill.fillAmount = (float)playerUnit.CurrentTp / playerUnit.MaxTp;
        topLeftPlayerXpText.text = $"XP: {playerUnit.Experience} / {playerUnit.StatsTables.Find(x => x.Level == playerUnit.Level + 1).Experience}";
        topLeftPlayerXpBarFill.fillAmount = (float)playerUnit.Experience / playerUnit.StatsTables.Find(x => x.Level == playerUnit.Level + 1).Experience;
    }

    private void ResetPanels()
    {
        bagPanel.SetActive(true);
        UpdateBag(inventoryManager.Inventory);
        equipmentPanel.SetActive(false);
        itemDisplayPanel.SetActive(false);
        statusPanel.SetActive(true);
        exitPanel.SetActive(false);
    }

    public void ToggleInventory()
    {
        isInventoryOpen.Value = !isInventoryOpen.Value;
        inventoryPanel.SetActive(isInventoryOpen.Value);
        if (isInventoryOpen.Value)
        {
            PlayerControls.Instance.ToggleDefaultControls(false);
            if (!updatedStatus)
            {
                ResetPanels();
                UpdatePlayerStatus(playerUnit);
                updatedStatus = true;
            }
        }
        else
        {
            PlayerControls.Instance.ToggleDefaultControls(true);
            updatedStatus = false;
        }
    }

    // Lvl Up Methods
    public void LvlUpAttack()
    {
        playerUnit.Attack++;
        playerUnit.AttributesPoints--;
        UpdatePlayerStatus(playerUnit);
    }
    public void LvlUpDefence()
    {
        playerUnit.Defence++;
        playerUnit.AttributesPoints--;
        UpdatePlayerStatus(playerUnit);
    }
    public void LvlUpSpeed()
    {
        playerUnit.Speed++;
        playerUnit.AttributesPoints--;
        UpdatePlayerStatus(playerUnit);
    }
    public void LvlUpLuck()
    {
        playerUnit.Luck++;
        playerUnit.AttributesPoints--;
        UpdatePlayerStatus(playerUnit);
    }
    public void LvlUpDexterity()
    {
        playerUnit.Dexterity++;
        playerUnit.AttributesPoints--;
        UpdatePlayerStatus(playerUnit);
    }

    public void AutoHeal()
    {
        if (playerUnit.CurrentTp >= 10 && playerUnit.CurrentHp < playerUnit.MaxHp)
            while (playerUnit.CurrentTp >= 10 && playerUnit.CurrentHp < playerUnit.MaxHp)
            {
                playerUnit.CurrentTp -= 10;
                playerUnit.CurrentHp += 1;
                if (playerUnit.CurrentHp <= playerUnit.MaxHp) continue;
                playerUnit.CurrentHp = playerUnit.MaxHp;
                break;
            }
        UpdatePlayerStatus(playerUnit);
    }
    #endregion ==========================================================================
}