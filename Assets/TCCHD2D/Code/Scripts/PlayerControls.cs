// Created by Sérgio Murillo da Costa Faria
// Date: 19/02/2023

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton responsible for storing the values of callbacks related to the game controls
/// </summary>
public class PlayerControls : MonoBehaviour
{
    // Public variables
    public static PlayerControls Instance { get; private set; }

    // Private variables
    [SerializeField, ReadOnly] private GameControls gameControls;
    [SerializeField, ReadOnly] private Vector2 moveValue;
    [SerializeField, InlineEditor] private BoolVariable isRunning;
    
    // Properties
    public Vector2 MoveValue => moveValue;
    public bool IsRunning => isRunning.Value;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void OnEnable()
    {
        gameControls = new GameControls();
        gameControls.Default.Walk.performed += OnMove;
        gameControls.Default.Walk.canceled += OnMoveRelease;
        gameControls.Enable();
    }
    
    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveValue = ctx.ReadValue <Vector2>();
    }
    private void OnMoveRelease(InputAction.CallbackContext ctx)
    {
        moveValue = Vector2.zero;
    }
    
    private void OnRun(InputAction.CallbackContext ctx)
    {
        isRunning.Value = ctx.ReadValueAsButton();
    }

    private void OnDisable()
    {
        gameControls.Disable();
    }
}
