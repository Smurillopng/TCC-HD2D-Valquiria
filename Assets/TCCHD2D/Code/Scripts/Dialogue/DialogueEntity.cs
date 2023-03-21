// Created by Sérgio Murillo da Costa Faria
// Date: 17/03/2023

using Sirenix.OdinInspector;
using UnityEngine;

public class DialogueEntity : MonoBehaviour
{
    [SerializeField, Required]
    private DialogueData dialogueData;

    [SerializeField, Required]
    private DialogueManager dialogueManager;
    
    [SerializeField, Required]
    private SpriteRenderer spriteRenderer;
    
    [SerializeField, Required]
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
