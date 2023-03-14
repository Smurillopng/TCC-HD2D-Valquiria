// Created by Sérgio Murillo da Costa Faria
// Date: 08/03/2023

using UnityEngine;
using UnityEngine.UI;
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
    private Image healthbarFilImage;

    [SerializeField]
    private TMP_Text healthText;

    [SerializeField] 
    private GameObject playerUnit;
    
    [SerializeField]
    private GameObject enemyUnit;
    
    private UnitController _playerUnitController;
    private UnitController _enemyUnitController;

    private void Awake()
    {
        if (playerUnit == null) playerUnit = GameObject.FindGameObjectWithTag("Player");
        if (enemyUnit == null) enemyUnit = GameObject.FindGameObjectWithTag("Enemy");
        _playerUnitController = playerUnit.GetComponent<UnitController>();
        _enemyUnitController = enemyUnit.GetComponent<UnitController>();
        
        healthText.text = $"{_playerUnitController.Unit.CurrentHp} / {_playerUnitController.Unit.MaxHp}";
    }

    private void Update()
    {
        healthText.text = $"{_playerUnitController.Unit.CurrentHp} / {_playerUnitController.Unit.MaxHp}";
        healthbarFilImage.fillAmount = (float)_playerUnitController.Unit.CurrentHp / _playerUnitController.Unit.MaxHp;
    }

    public void Attack()
    {
        Debug.Log("<b>Pressed <color=red>Attack</color> button</b>"); 
        _playerUnitController.UnitDirector.Play(_playerUnitController.BasicAttack);
        _playerUnitController.AttackAction(_enemyUnitController);
    }

    public void Special()
    {
        Debug.Log("<b>Pressed  <color=magenta>Special</color> button</b>");
    }

    public void Item()
    {
        Debug.Log("<b>Pressed  <color=cyan>Item</color> button</b>");
    }

    public void Run()
    {
        Debug.Log($"<b>Pressed <color=green>Run</color> button</b>");
    }
}
