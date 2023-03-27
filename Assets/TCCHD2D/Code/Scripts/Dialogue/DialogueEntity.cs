// Created by Sérgio Murillo da Costa Faria
// Date: 17/03/2023

using Sirenix.OdinInspector;
using UnityEngine;

public class DialogueEntity : MonoBehaviour
{
    [SerializeField, Required, Tooltip("Dialogue data that represents the dialogue of this unit.")]
    private DialogueData dialogueData;

    [SerializeField, Required]
    private DialogueManager dialogueManager;

    [SerializeField, Required, Tooltip("Sprite renderer of the icon that will hover above the unit's head.")]
    private SpriteRenderer spriteRenderer;

    [SerializeField, Required, Tooltip("Default icon that will be displayed above the unit's head when the player is in range of interaction.")]
    private Sprite defaultIcon;

    public void StartDialogue()
    {
        dialogueManager.StartDialogue(dialogueData);
    }

    public void SetIcon()
    {
        if (spriteRenderer.sprite == defaultIcon) return;
        spriteRenderer.sprite = defaultIcon;
    }

    public void ResetIcon()
    {
        if (spriteRenderer.sprite == null) return;
        spriteRenderer.sprite = null;
    }
}
