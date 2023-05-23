// Created by Sérgio Murillo da Costa Faria
// Date: 01/04/2023

using System;
using CI.QuickSave;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.VFX;

[CreateAssetMenu(fileName = "New Consumable Item", menuName = "RPG/New Consumable Item", order = 0)]
public class Consumable : ScriptableObject, IItem
{
    [SerializeField] private ItemTyping itemType;
    [SerializeField, HideInInspector] private int itemID; // TODO: caso seja necessário
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private ConsumableTypes effectType;
    [SerializeField] private VisualEffectAsset vfx;
    [SerializeField] private string itemDescription;
    [SerializeField] private int maxStack;
    [SerializeField] private int currentStack;
    [SerializeField, HideInInspector] private int itemValue; // TODO: para quando der para vender
    [SerializeField] private int effectValue;

    public ItemTyping ItemType
    {
        get => itemType;
        set => itemType = value;
    }
    public int ItemID
    {
        get => itemID;
        set => itemID = value;
    }

    public string ItemName
    {
        get => itemName;
        set => itemName = value;
    }

    public Sprite ItemIcon
    {
        get => itemIcon;
        set => itemIcon = value;
    }

    public ConsumableTypes EffectType
    {
        get => effectType;
        set => effectType = value;
    }

    public string ItemDescription
    {
        get => itemDescription;
        set => itemDescription = value;
    }

    public int MaxStack
    {
        get => maxStack;
        set => maxStack = value;
    }

    public int CurrentStack
    {
        get => currentStack;
        set => currentStack = value;
    }

    public int ItemValue
    {
        get => itemValue;
        set => itemValue = value;
    }

    public int EffectValue
    {
        get => effectValue;
        set => effectValue = value;
    }

    public void Use()
    {
        switch (effectType)
        {
            case ConsumableTypes.Heal:
                Heal();
                break;
            case ConsumableTypes.Damage:
                Damage();
                break;
            case ConsumableTypes.IncreaseTp:
                IncreaseTp();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Discard()
    {
        Debug.Log("Discarded");
    }

    //In Combat Effects

    private void Heal()
    {
        var scene = SceneManager.GetActiveScene().name;
        if (PlayerControls.Instance.SceneMap.TryGetValue(scene, out var gameValue) && gameValue == SceneType.Game)
        {
            var inventory = FindObjectOfType<InventoryUI>();
            var player = inventory.PlayerUnit;
            if (player.CurrentHp + EffectValue < player.MaxHp)
                player.CurrentHp += EffectValue;
            else if (player.CurrentHp + EffectValue >= player.MaxHp)
                player.CurrentHp = player.MaxHp;
            if (CurrentStack <= 0)
            {
                InventoryManager.Instance.Inventory.Remove(this);
            }
            else
            {
                CurrentStack--;
                if (CurrentStack <= 0)
                {
                    InventoryManager.Instance.Inventory.Remove(this);
                }
            }
        }
        if (PlayerControls.Instance.SceneMap.TryGetValue(scene, out var combatValue) && combatValue == SceneType.Combat)
        {
            var target = FindObjectOfType<TurnManager>().PlayerUnitController;
            if (target.Unit.CurrentHp + EffectValue < target.Unit.MaxHp)
                target.Unit.CurrentHp += EffectValue;
            else if (target.Unit.CurrentHp + EffectValue >= target.Unit.MaxHp)
                target.Unit.CurrentHp = target.Unit.MaxHp;
            UpdateTrack(target);
            target.Director.Play(target.UseItem);
            if (CurrentStack <= 0)
            {
                InventoryManager.Instance.Inventory.Remove(this);
            }
            else
            {
                CurrentStack--;
                if (CurrentStack <= 0)
                {
                    InventoryManager.Instance.Inventory.Remove(this);
                }
            }
        }
        var writer = QuickSaveWriter.Create("Inventory");
        writer.Write(itemName, currentStack);
        writer.Commit();
    }

    private void Damage()
    {
        var scene = SceneManager.GetActiveScene().name;
        if (PlayerControls.Instance.SceneMap.TryGetValue(scene, out var combatValue) && combatValue == SceneType.Combat)
        {
            var player = FindObjectOfType<TurnManager>().PlayerUnitController;
            var target = FindObjectOfType<TurnManager>().EnemyUnitController;
            target.Unit.CurrentHp -= EffectValue;
            if (target.Unit.CurrentHp <= 0)
            {
                target.Unit.IsDead = true;
                target.Unit.CurrentHp = 0;
            }
            UpdateTrack(target);
            player.Director.Play(player.UseItem);
            if (CurrentStack <= 0)
            {
                InventoryManager.Instance.Inventory.Remove(this);
            }
            else
            {
                CurrentStack--;
                if (CurrentStack <= 0)
                {
                    InventoryManager.Instance.Inventory.Remove(this);
                }
            }
        }
        var writer = QuickSaveWriter.Create("Inventory");
        writer.Write(itemName, currentStack);
        writer.Commit();
    }

    private void IncreaseTp()
    {
        var scene = SceneManager.GetActiveScene().name;
        if (PlayerControls.Instance.SceneMap.TryGetValue(scene, out var gameValue) && gameValue == SceneType.Game)
        {
            var inventory = FindObjectOfType<InventoryUI>();
            var player = inventory.PlayerUnit;
            if (player.CurrentTp + EffectValue < player.MaxTp)
                player.CurrentTp += EffectValue;
            else if (player.CurrentTp + EffectValue >= player.MaxTp)
                player.CurrentTp = player.MaxTp;
            if (CurrentStack <= 0)
            {
                InventoryManager.Instance.Inventory.Remove(this);
            }
            else
            {
                CurrentStack--;
                if (CurrentStack <= 0)
                {
                    InventoryManager.Instance.Inventory.Remove(this);
                }
            }
        }
        if (PlayerControls.Instance.SceneMap.TryGetValue(scene, out var combatValue) && combatValue == SceneType.Combat)
        {
            var target = FindObjectOfType<TurnManager>().PlayerUnitController;
            if (target.Unit.CurrentTp + EffectValue < target.Unit.MaxTp)
                target.Unit.CurrentTp += EffectValue;
            else if (target.Unit.CurrentTp + EffectValue >= target.Unit.MaxTp)
                target.Unit.CurrentTp = target.Unit.MaxTp;
            UpdateTrack(target);
            target.Director.Play(target.UseItem);
            if (CurrentStack <= 0)
            {
                InventoryManager.Instance.Inventory.Remove(this);
            }
            else
            {
                CurrentStack--;
                if (CurrentStack <= 0)
                {
                    InventoryManager.Instance.Inventory.Remove(this);
                }
            }
        }
        var writer = QuickSaveWriter.Create("Inventory");
        writer.Write(itemName, currentStack);
        writer.Commit();
    }

    private void UpdateTrack(UnitController target)
    {
        if (target.Unit.IsPlayer)
        {
            var enemyObject = GameObject.FindWithTag("Enemy");
            foreach (var track in target.UseItem.GetOutputTracks())
            {
                switch (track.name)
                {
                    case "AttackAnimation":
                        target.Director.SetGenericBinding(track, target.gameObject.GetComponentInChildren<Animator>());
                        break;
                    case "MovementAnimation":
                        target.Director.SetGenericBinding(track, target.gameObject.GetComponentInChildren<Animator>());
                        break;
                    case "Signals":
                        target.Director.SetGenericBinding(track, enemyObject.GetComponentInChildren<SignalReceiver>());
                        break;
                    case "Vfx":
                        var vfxAsset = target.gameObject.GetComponentInChildren<VisualEffect>();
                        vfxAsset.visualEffectAsset = vfx;
                        target.Director.SetGenericBinding(track, vfxAsset);
                        break;
                }
            }
        }
    }
}