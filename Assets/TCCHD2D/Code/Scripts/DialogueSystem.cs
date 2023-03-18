// Created by Sérgio Murillo da Costa Faria
// Date: 17/03/2023

using Sirenix.OdinInspector;
using UnityEngine;

public class DialogueSystem : MonoBehaviour
{
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private GameObject dialogueBoxPrefab;
    [SerializeField] private DialogueBox dialogueBox;
    [SerializeField] private Interactable interactable;
    [SerializeField, InlineEditor] private BoolVariable interactBool;

    private void Update()
    {
        if (CanInteract())
        {
            StartDialogue();
        }
    }
    
    private bool CanInteract()
    {
        return interactable != null && interactable.isActiveAndEnabled && interactable.CanInteract() && interactBool.Value;
    }

    public void StartDialogue()
    {
        dialogueBoxPrefab.SetActive(true);
        dialogueBox.DialogueData = dialogueData;
    }

    public void EndDialogue()
    {
        dialogueBoxPrefab.SetActive(false);
    }
}
