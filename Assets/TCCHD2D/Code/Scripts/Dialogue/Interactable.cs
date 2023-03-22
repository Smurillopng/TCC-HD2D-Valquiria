// Created by Sérgio Murillo da Costa Faria
// Date: 17/03/2023

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [TitleGroup("Interaction Settings", Alignment = TitleAlignments.Centered)]
    [SerializeField, Range(0.1f, 100f)]
    private float interactionRange = 3f;

    [SerializeField, Required]
    private Transform playerTransform;

    [SerializeField, InlineEditor, Required]
    private BoolVariable interactBool;

    [TitleGroup("Events", Alignment = TitleAlignments.Centered)]
    public UnityEvent onInteractionStart;
    public UnityEvent onInteractionInRange;
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