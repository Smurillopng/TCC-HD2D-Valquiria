using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Represents a dialogue entity in the game world.
/// </summary>
/// <remarks>
/// This class is used to represent a dialogue entity in the game world. A dialogue entity is an object that can be interacted with by the player to start a dialogue.
/// </remarks>
public class DialogueEntity : MonoBehaviour
{
    #region === Variables ===============================================================

    [SerializeField, Required, Tooltip("Dialogue data that represents the dialogue of this unit.")]
    private DialogueData dialogueData;

    [SerializeField, Required]
    private DialogueManager dialogueManager;

    [SerializeField, Required, Tooltip("Sprite renderer of the icon that will hover above the unit's head.")]
    private SpriteRenderer spriteRenderer;

    [SerializeField, Required, Tooltip("Default icon that will be displayed above the unit's head when the player is in range of interaction.")]
    private Sprite defaultIcon;

    #endregion

    #region === Methods =================================================================

    /// <summary>Starts a dialogue.</summary>
    /// <remarks>
    /// This method calls the StartDialogue method of the dialogue manager, passing in the dialogue data.
    /// </remarks>
    public void StartDialogue()
    {
        dialogueManager.StartDialogue(dialogueData);
    }
    /// <summary>Sets the icon to the default icon.</summary>
    /// <remarks>
    /// If the current icon is already the default icon, this method does nothing.
    /// </remarks>
    public void SetIcon()
    {
        if (spriteRenderer.sprite == defaultIcon) return;
        spriteRenderer.sprite = defaultIcon;
    }
    /// <summary>Resets the icon to its default state.</summary>
    /// <remarks>If the icon is already in its default state, this method does nothing.</remarks>
    public void ResetIcon()
    {
        if (spriteRenderer.sprite == null) return;
        spriteRenderer.sprite = null;
    }

    #endregion
}
