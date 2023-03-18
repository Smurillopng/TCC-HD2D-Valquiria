// Created by Sérgio Murillo da Costa Faria
// Date: 17/03/2023

using Sirenix.OdinInspector;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private DialogueSystem dialogueSystem;
    [SerializeField, InlineEditor] private BoolVariable interactBool;

    private bool canInteract = false;
    
    public bool CanInteract() => canInteract;

    private void Update()
    {
        if (Vector3.Distance(transform.position, playerTransform.position) <= interactionRange)
        {
            print("Can interact");
            canInteract = true;
            PlayerControls.Instance.OnInteract();
            if (interactBool.Value)
            {
                print("Interacting");
                StartInteraction();
            }
        }
        else
        {
            canInteract = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }

    private void StartInteraction()
    {
        dialogueSystem.StartDialogue();
    }
}