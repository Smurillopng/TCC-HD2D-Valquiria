// Created by Sérgio Murillo da Costa Faria
// Date: 17/03/2023

using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : SerializedMonoBehaviour
{
    #region === Variables ===============================================================

    [FoldoutGroup("Interaction Settings")]
    [SerializeField, EnumPaging, Tooltip("Type of interaction that the object will have with the player.")]
    private InteractionType interactionType;

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

    private bool _interacted;

    private enum InteractionState { InRange, OffRange, Interacting }
    private InteractionState _interactionState;

    #endregion

    #region === Unity Methods ===========================================================

    private void Start()
    {
        if (interactionType == InteractionType.Item)
        {
            var reader = QuickSaveReader.Create("GameSave");
            if (reader.Exists($"{name}") && reader.Read<bool>($"{name}"))
                gameObject.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        var inRange = (transform.position - playerTransform.position).sqrMagnitude <= interactionRange * interactionRange;
        if (inRange)
        {
            switch (interactBool.Value)
            {
                case true:
                    if (!_interacted)
                    {
                        _interacted = true;
                        StartInteraction();
                    }
                    break;
                case false:
                    _interacted = false;
                    break;
            }
            if (_interactionState == InteractionState.InRange || _interactionState == InteractionState.Interacting) return;
            _interactionState = InteractionState.InRange;
            onInteractionInRange?.Invoke();
        }
        else
        {
            if (_interactionState == InteractionState.OffRange) return;
            _interactionState = InteractionState.OffRange;
            onInteractionOffRange?.Invoke();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }

    #endregion

    #region === Methods =================================================================

    public void StartInteraction()
    {
        _interactionState = InteractionState.Interacting;
        onInteractionStart?.Invoke();
    }

    #endregion
}