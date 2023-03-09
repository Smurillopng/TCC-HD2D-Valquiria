// Created by Sérgio Murillo da Costa Faria
// Date: 08/03/2023

using UnityEngine;
using UnityEngine.UI;
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
    private int maxHealth;
    
    [SerializeField] 
    private int healthbarFill;
    
    [SerializeField] 
    private Image healthbarFilImage;
    
    [SerializeField] 
    private TMP_Text healthText;
    
    [SerializeField] 
    private Button attackButton;

    private void Awake()
    {
        healthText.text = $"{healthbarFill} / {maxHealth}";
    }

    private void Update()
    {
        healthText.text = $"{healthbarFill} / {maxHealth}";
        healthbarFilImage.fillAmount = (float)healthbarFill / maxHealth;

    }

    public void Attack()
    {
        player.GetComponent<Animator>().enabled = true;
    }
}
