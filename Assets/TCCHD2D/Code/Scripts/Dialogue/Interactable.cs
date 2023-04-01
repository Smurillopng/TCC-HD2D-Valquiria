// Created by Sérgio Murillo da Costa Faria
// Date: 17/03/2023

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [FoldoutGroup("Interaction Settings")]
    [SerializeField, Range(0.1f, 100f), Tooltip("Distance needed to interact with the object.")]
    private float interactionRange = 3f;

    [FoldoutGroup("Interaction Settings")]
    [SerializeField, Required]
    private Transform playerTransform;

    [FoldoutGroup("Interaction Settings")]
    [SerializeField, InlineEditor, Required, Tooltip("Bool variable that will be used to interact with the object.")]
    private BoolVariable interactBool;

    [FoldoutGroup("Events"), Tooltip("Event called when the player interacts with the object.")]
    public UnityEvent onInteractionStart;
    [FoldoutGroup("Events"), Tooltip("Event called when the player is in range of the object.")]
    public UnityEvent onInteractionInRange;
    [FoldoutGroup("Events"), Tooltip("Event called when the player is out of range of the object.")]
    public UnityEvent onInteractionOffRange;

    private bool _hasInteracted;
    public bool CanInteract() => _hasInteracted;

    private void Update()
    {
        if (Vector3.Distance(transform.position, playerTransform.position) <= interactionRange)
        {
            onInteractionInRange?.Invoke();
            switch (interactBool.Value)
            {
                case true:
                    if (_hasInteracted)
                        StartInteraction();
                    break;
                case false:
                    _hasInteracted = true;
                    break;
            }
        }
        else
        {
            if (!_hasInteracted) return;
            _hasInteracted = false;
            onInteractionOffRange?.Invoke();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            _hasInteracted = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            _hasInteracted = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }

    private void StartInteraction()
    {
        onInteractionStart?.Invoke();
        _hasInteracted = false;
    }
}