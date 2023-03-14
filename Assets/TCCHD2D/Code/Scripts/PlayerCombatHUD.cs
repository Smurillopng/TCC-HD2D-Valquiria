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
    private Image playerHelthbarFill;

    [SerializeField]
    private TMP_Text playerHealthText;

    [SerializeField]
    private Image enemyHelthbarFill;

    [SerializeField]
    private TMP_Text enemyHealthText;
    
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
        
        playerHealthText.text = $"{_playerUnitController.Unit.CurrentHp} / {_playerUnitController.Unit.MaxHp}";
        enemyHealthText.text = $"{_enemyUnitController.Unit.CurrentHp} / {_enemyUnitController.Unit.MaxHp}";
    }

    private void Update()
    {
        playerHealthText.text = $"{_playerUnitController.Unit.CurrentHp} / {_playerUnitController.Unit.MaxHp}";
        playerHelthbarFill.fillAmount = (float)_playerUnitController.Unit.CurrentHp / _playerUnitController.Unit.MaxHp;
        
        enemyHealthText.text = $"{_enemyUnitController.Unit.CurrentHp} / {_enemyUnitController.Unit.MaxHp}";
        enemyHelthbarFill.fillAmount = (float)_enemyUnitController.Unit.CurrentHp / _enemyUnitController.Unit.MaxHp;
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
