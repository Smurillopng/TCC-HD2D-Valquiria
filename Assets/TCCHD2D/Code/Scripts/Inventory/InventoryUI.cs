// Created by Sérgio Murillo da Costa Faria
// Date: 04/04/2023

using System;
using System.Collections.Generic;
using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [BoxGroup("Panels")][SerializeField] private GameObject inventoryPanel;
    [BoxGroup("Panels")][SerializeField] private GameObject bagPanel;
    [BoxGroup("Panels")][SerializeField] private GameObject equipmentPanel;
    [BoxGroup("Panels")][SerializeField] private GameObject itemDisplayPanel;
    [BoxGroup("Panels")][SerializeField] private GameObject statusPanel;
    [BoxGroup("Panels")][SerializeField] private GameObject exitPanel;

    [BoxGroup("Equipment Slots")]
    [SerializeField]
    private Image headSlot;

    [BoxGroup("Equipment Slots")]
    [SerializeField]
    private Image chestSlot;

    [BoxGroup("Equipment Slots")]
    [SerializeField]
    private Image legsSlot;

    [BoxGroup("Equipment Slots")]
    [SerializeField]
    private Image weaponSlot;

    [BoxGroup("Equipment Slots")]
    [SerializeField]
    private Image runeSlot;

    [BoxGroup("Status")][SerializeField] private TMP_Text playerLvl;
    [BoxGroup("Status")][SerializeField] private Image playerHealthBarFill;
    [BoxGroup("Status")][SerializeField] private TMP_Text playerHealthText;
    [BoxGroup("Status")][SerializeField] private Image playerTpFill;
    [BoxGroup("Status")][SerializeField] private TMP_Text playerTpText;
    [BoxGroup("Status")][SerializeField] private Image playerXpBarFill;
    [BoxGroup("Status")][SerializeField] private TMP_Text playerXpText;
    [BoxGroup("Status")][SerializeField] private TMP_Text playerAttack;
    [BoxGroup("Status")][SerializeField] private TMP_Text playerDefence;
    [BoxGroup("Status")][SerializeField] private TMP_Text playerSpeed;
    [BoxGroup("Status")][SerializeField] private TMP_Text playerLuck;
    [BoxGroup("Status")][SerializeField] private TMP_Text playerDexterity;
    [BoxGroup("Status")][SerializeField] private GameObject lvlUpAttributesButtons;
    [BoxGroup("Status")][SerializeField] private TMP_Text attributePointsText;
    [BoxGroup("Status")][SerializeField] private TMP_Text availableAttributePointsText;
    
    [BoxGroup("Left Bars")][SerializeField] private Image topLeftPlayerHealthBarFill;
    [BoxGroup("Left Bars")][SerializeField] private TMP_Text topLeftPlayerHealthText;
    [BoxGroup("Left Bars")][SerializeField] private Image topLeftPlayerTpFill;
    [BoxGroup("Left Bars")][SerializeField] private TMP_Text topLeftPlayerTpText;
    [BoxGroup("Left Bars")][SerializeField] private Image topLeftPlayerXpBarFill;
    [BoxGroup("Left Bars")][SerializeField] private TMP_Text topLeftPlayerXpText;

    [BoxGroup("Item Display")][SerializeField] private TMP_Text itemName;
    [BoxGroup("Item Display")][SerializeField] private TMP_Text itemDescription;
    [BoxGroup("Item Display")][SerializeField] private Image itemIcon;
    [BoxGroup("Item Display")][SerializeField] private TMP_Text itemQuantity;
    [BoxGroup("Item Display")][SerializeField] private Button itemUseButton;

    [BoxGroup("Item Scroll")][SerializeField] private RectTransform bagRectTransform;
    [BoxGroup("Item Scroll")][SerializeField] private ScrollRect bagScrollRect;
    [BoxGroup("Item Scroll")][SerializeField] private float scrollOffset;

    [BoxGroup("External References")]
    [SerializeField]
    private Button autoHealButton;

    [BoxGroup("External References")]
    [SerializeField]
    private Unit playerUnit;

    [BoxGroup("External References")]
    [SerializeField]
    private GameObject itemPrefab;

    [BoxGroup("External References")]
    [SerializeField]
    private BoolVariable isInventoryOpen;

    [BoxGroup("Debug")]
    [SerializeField, ReadOnly]
    private bool updatedStatus;

    [BoxGroup("Debug")]
    [SerializeField, ReadOnly]
    private InventoryManager inventoryManager;

    public Unit PlayerUnit => playerUnit;
    
    private bool _gameStarted;

    private void Start()
    {
        inventoryManager = InventoryManager.Instance;
        if (QuickSaveReader.Create("GameInfo").Exists("GameStarted"))
            _gameStarted = QuickSaveReader.Create("GameInfo").Read<bool>("GameStarted");
        if (_gameStarted) return;
        var reader = QuickSaveReader.Create("GameSave");
        if (reader.Exists("PlayerAttack")) playerUnit.Attack = reader.Read<int>("PlayerAttack");
        if (reader.Exists("PlayerDefence")) playerUnit.Defence = reader.Read<int>("PlayerDefence");
        if (reader.Exists("PlayerSpeed")) playerUnit.Speed = reader.Read<int>("PlayerSpeed");
        if (reader.Exists("PlayerLuck")) playerUnit.Luck = reader.Read<int>("PlayerLuck");
        if (reader.Exists("PlayerDexterity")) playerUnit.Dexterity = reader.Read<int>("PlayerDexterity");
        if (reader.Exists("Level")) playerUnit.Level = reader.Read<int>("Level");
        if (reader.Exists("Experience")) playerUnit.Experience = reader.Read<int>("Experience");
        if (reader.Exists("AttributesPoints")) playerUnit.AttributesPoints = reader.Read<int>("AttributesPoints");
        if (reader.Exists("PlayerMaxHealth")) playerUnit.MaxHp = reader.Read<int>("PlayerMaxHealth");
        if (reader.Exists("PlayerCurrentHealth")) playerUnit.CurrentHp = reader.Read<int>("PlayerCurrentHealth");
        if (reader.Exists("PlayerMaxTp")) playerUnit.MaxTp = reader.Read<int>("PlayerMaxTp");
        if (reader.Exists("PlayerCurrentTp")) playerUnit.CurrentTp = reader.Read<int>("PlayerCurrentTp");
        _gameStarted = true;
        QuickSaveWriter.Create("GameInfo").Write("GameStarted", _gameStarted).Commit();
    }

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

        bagRectTransform.sizeDelta = new Vector2(0,totalHeight - scrollOffset);
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
            else if (currentItem.ItemType == ItemTyping.Equipment)
            {
                var equipment = currentItem as Equipment;
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
        playerAttack.text = inventoryManager.EquipmentSlots.Find(x => x.slotType == EquipmentSlotType.Weapon).equipItem != null ? $"Ataque: {unit.Attack} <color=#00FF00>(+{inventoryManager.EquipmentSlots.Find(x => x.slotType == EquipmentSlotType.Weapon).equipItem.StatusValue})" : $"Ataque: {unit.Attack}</color>";
        playerDefence.text = inventoryManager.EquipmentSlots.Find(x => x.slotType == EquipmentSlotType.Chest).equipItem != null ? $"Defesa: {unit.Defence} <color=#00FF00>(+{inventoryManager.EquipmentSlots.Find(x => x.slotType == EquipmentSlotType.Chest).equipItem.StatusValue})" : $"Defesa: {unit.Defence}</color>";
        playerSpeed.text = inventoryManager.EquipmentSlots.Find(x => x.slotType == EquipmentSlotType.Legs).equipItem != null ? $"Velocidade: {unit.Speed} <color=#00FF00>(+{inventoryManager.EquipmentSlots.Find(x => x.slotType == EquipmentSlotType.Legs).equipItem.StatusValue})" : $"Velocidade: {unit.Speed}</color>";
        playerLuck.text = inventoryManager.EquipmentSlots.Find(x => x.slotType == EquipmentSlotType.Rune).equipItem != null ? $"Sorte: {unit.Luck} <color=#00FF00>(+{inventoryManager.EquipmentSlots.Find(x => x.slotType == EquipmentSlotType.Rune).equipItem.StatusValue})" : $"Sorte: {unit.Luck}</color>";
        playerDexterity.text = inventoryManager.EquipmentSlots.Find(x => x.slotType == EquipmentSlotType.Head).equipItem != null ? $"Destreza: {unit.Dexterity} <color=#00FF00>(+{inventoryManager.EquipmentSlots.Find(x => x.slotType == EquipmentSlotType.Head).equipItem.StatusValue})" : $"Destreza: {unit.Dexterity}</color>";
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

    public void Update()
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
        
        if (autoHealButton.gameObject.activeSelf) autoHealButton.interactable = playerUnit.CurrentTp >= 10 && playerUnit.CurrentHp < playerUnit.MaxHp;
        availableAttributePointsText.text = playerUnit.AttributesPoints > 0 ? $"Pontos de Atributos disponíveis: {playerUnit.AttributesPoints}": string.Empty;
        lvlUpAttributesButtons.SetActive(playerUnit.AttributesPoints > 0);
        attributePointsText.gameObject.SetActive(playerUnit.AttributesPoints > 0);
        attributePointsText.text = $"Pontos de Atributos disponíveis: {playerUnit.AttributesPoints}";
        UpdateTopLeftBars();
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
}