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
    private int maxHealth;

    [SerializeField]
    private int healthbarFill;

    [SerializeField]
    private Image healthbarFilImage;

    [SerializeField]
    private TMP_Text healthText;

    [SerializeField]
    private PlayableDirector playerDirector;

    [SerializeField]
    private PlayableAsset basicAttackTimeline;

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
        Debug.Log("<b>Pressed <color=red>Attack</color> button</b>");
        playerDirector.Play(basicAttackTimeline);
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
