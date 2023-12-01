// Created by Sérgio Murillo da Costa Faria

using System.Collections.Generic;
using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[HideMonoScript]
public class Interactable : SerializedMonoBehaviour
{
    #region === Variables ===============================================================

    [FoldoutGroup("Interactable")]
    [FoldoutGroup("Interactable/Interaction Settings")]
    [SerializeField, EnumPaging, Tooltip("Type of interaction that the object will have with the player.")]
    private InteractionType interactionType;

    [FoldoutGroup("Interactable/Interaction Settings")]
    [SerializeField, Range(0.1f, 2f), Tooltip("Distance needed to interact with the object.")]
    private float interactionRange = 1f;

    [FoldoutGroup("Interactable/Interaction Settings")]
    [SerializeField]
    private Transform playerTransform;

    [FoldoutGroup("Interactable/Interaction Settings")]
    [SerializeField, InlineEditor, Required, Tooltip("Bool variable that will be used to interact with the object.")]
    private BoolVariable interactBool;

    [FoldoutGroup("Interactable/Events"), Tooltip("Event called when the player interacts with the object.")]
    public UnityEvent onAwake;

    [FoldoutGroup("Interactable/Events"), Tooltip("Event called when the player interacts with the object.")]
    public UnityEvent onInteractionStart;

    [FoldoutGroup("Interactable/Events"), Tooltip("Event called when the player is in range of the object.")]
    public UnityEvent onInteractionInRange;

    [FoldoutGroup("Interactable/Events"), Tooltip("Event called when the player is out of range of the object.")]
    public UnityEvent onInteractionOffRange;

    private bool _interacted, _setStage;
    private static bool _canInteract = true;

    private enum InteractionState { InRange, OffRange, Interacting }
    private InteractionState _interactionState;
    private QuickSaveReader _reader;
    public static bool CanInteract
    {
        get { return _canInteract; }
        set { _canInteract = value; }
    }

    #endregion

    #region === Unity Methods ===========================================================

    private void Start()
    {
        if (playerTransform == null) playerTransform = GameObject.FindWithTag("Player").GetComponent<Transform>();
        onAwake?.Invoke();
        if (interactionType == InteractionType.Item)
        {
            _reader = QuickSaveReader.Create("GameSave");
            if (_reader.Exists($"{name}") && _reader.Read<bool>($"{name}"))
                gameObject.SetActive(false);
            _reader = QuickSaveReader.Create("ItemInfo");
            if (_reader.Exists($"{name}") && _reader.Read<bool>($"{name}"))
                gameObject.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        _reader ??= QuickSaveReader.Create("GameSave");
        if (_reader.Exists("PlayedTutorial") && !_reader.Read<bool>("PlayedTutorial") && _setStage) return;
        var inRange = (transform.position - playerTransform.position).sqrMagnitude <= interactionRange * interactionRange;
        if (inRange)
        {
            switch (interactBool.Value)
            {
                case true:
                    if (!_interacted && _canInteract)
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
        _setStage = true;
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