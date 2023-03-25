// Created by Sérgio Murillo da Costa Faria
// Date: 19/02/2023

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Responsible for storing the values of callbacks related to the game controls action map.
/// </summary>
public class PlayerControls : SerializedMonoBehaviour
{
    // Public variables
    public static PlayerControls Instance { get; private set; }

    // Private variables
    [SerializeField, ReadOnly]
    private GameControls gameControls;

    [SerializeField, InlineEditor]
    private BoolVariable showConsole;

    [SerializeField, InlineEditor]
    private Vector2Variable moveValue;

    [SerializeField, InlineEditor]
    private BoolVariable isRunning;

    [SerializeField, InlineEditor]
    private BoolVariable interacted;

    [SerializeField]
    private Dictionary<string, SceneType> sceneMap = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        gameControls = new GameControls();

        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneMap.TryGetValue(sceneName, out var currentScene))
        {
            print($"Current scene: {currentScene}");
            switch (currentScene)
            {
                case SceneType.Menu:
                    break;
                case SceneType.Game:
                    gameControls.Default.Walk.performed += OnMove;
                    gameControls.Default.Walk.canceled += OnMoveRelease;
                    gameControls.Default.Run.performed += OnRun;
                    gameControls.Default.Run.canceled += OnRun;
                    gameControls.Default.Interact.performed += OnInteract;
                    gameControls.Default.Interact.canceled += OnInteract;
                    break;
                case SceneType.Combat:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else Debug.LogError($"Scene {sceneName} is not mapped to any SceneType!");

        gameControls.Enable();
    }

    private void OnSceneUnloaded(Scene arg0)
    {
        if (gameControls == null) return;
        gameControls.Default.Walk.performed -= OnMove;
        gameControls.Default.Walk.canceled -= OnMoveRelease;
        gameControls.Default.Run.performed -= OnRun;
        gameControls.Default.Run.canceled -= OnRun;
        gameControls.Default.Interact.performed -= OnInteract;
        gameControls.Default.Interact.canceled -= OnInteract;
        gameControls.Disable();
    }
    
    public void ChangeSceneTo(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Is called when the "Walk" input of the "GameControls" Input Actions is performed.
    /// </summary>
    /// <param name="ctx"></param>
    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveValue.Value = ctx.ReadValue<Vector2>();
    }
    /// <summary>
    /// Is called when the "Walk" input of the "GameControls" Input Actions is released.
    /// </summary>
    /// <param name="ctx"></param>
    private void OnMoveRelease(InputAction.CallbackContext ctx)
    {
        moveValue.Value = Vector2.zero;
    }
    /// <summary>
    /// Is called when the "Run" input of the "GameControls" Input Actions is performed.
    /// </summary>
    /// <param name="ctx"></param>
    private void OnRun(InputAction.CallbackContext ctx)
    {
        isRunning.Value = ctx.ReadValueAsButton();
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            interacted.Value = true;
        else if (ctx.canceled)
            interacted.Value = false;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        gameControls?.Disable();
    }
}

public enum SceneType
{
    Menu,
    Game,
    Combat
}