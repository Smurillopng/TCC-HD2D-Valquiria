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
    public static PlayerControls Instance;

    [SerializeField, ReadOnly] private GameControls _gameControls;

    [SerializeField, ReadOnly] private Vector2 moveValue;
    public Vector2 MoveValue => moveValue;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    private void OnEnable()
    {
        _gameControls = new GameControls();
        _gameControls.Enable();
        _gameControls.Default.Movement.performed += OnMove;
        _gameControls.Default.Movement.canceled += OnMoveRelease;
    }
    
    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveValue = ctx.ReadValue <Vector2>();
    }
    private void OnMoveRelease(InputAction.CallbackContext ctx)
    {
        moveValue = Vector2.zero;
    }

    private void OnDisable()
    {
        _gameControls.Disable();
    }
}
