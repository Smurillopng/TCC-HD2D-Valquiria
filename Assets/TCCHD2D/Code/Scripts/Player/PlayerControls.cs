using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// This class is responsible for storing the values of callbacks related to the game controls action map.
/// </summary>
/// <remarks>
/// Created by Sérgio Murillo da Costa Faria on 19/02/2023.
/// </remarks>
[HideMonoScript]
public class PlayerControls : SerializedMonoBehaviour
{
    #region === Variables ===============================================================
    
    public static PlayerControls Instance { get; private set; }

    [SerializeField]
    [ReadOnly]
    [Tooltip("The game controls asset used to create the input actions.")]
    private GameControls gameControls;

    [SerializeField]
    [InlineEditor]
    [Tooltip("Bool to toggle the console on and off.")]
    private BoolVariable showConsole;

    [SerializeField]
    [InlineEditor]
    [Tooltip("Bool that reads the current movement input value.")]
    private Vector2Variable moveValue;

    [SerializeField]
    [InlineEditor]
    [Tooltip("Bool that checks whether or not the player is running.")]
    private BoolVariable isRunning;

    [SerializeField]
    [InlineEditor]
    [Tooltip("Bool that checks whether or not the player has interacted with something.")]
    private BoolVariable interacted;

    [SerializeField]
    [InlineEditor]
    [Tooltip("Bool that checks whether or not the game is currently paused.")]
    private BoolVariable isPaused;

    [SerializeField]
    [InlineEditor]
    [Tooltip("Bool that checks whether or not the player has opened the inventory.")]
    private BoolVariable openInventory;

    [SerializeField]
    [Tooltip("A mapping of scene names to SceneType enums.")]
    private Dictionary<string, SceneType> sceneMap = new();

    public Dictionary<string, SceneType> SceneMap => sceneMap;

    #endregion

    #region === Unity Methods ===========================================================

    /// <summary>
    /// Checks for an existing instance and destroys this object if one exists, otherwise sets this object as the instance and persists it between scenes.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    /// <summary>
    /// Subscribe to the sceneLoaded and sceneUnloaded events.
    /// </summary>
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }
    /// <summary>
    /// Handles initialization of the GameControls asset and subscribing to input events when a scene is loaded.
    /// </summary>
    /// <param name="scene">The scene that was loaded.</param>
    /// <param name="mode">The mode in which the scene was loaded.</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        gameControls = new GameControls();

        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneMap.TryGetValue(sceneName, out var currentScene))
        {
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
                    gameControls.Menus.OpenInventory.performed += OnInventory;
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
    /// <summary>
    /// Handles unsubscribing from input events when a scene is unloaded.
    /// </summary>
    /// <param name="scene">The scene that was unloaded.</param>
    private void OnSceneUnloaded(Scene scene)
    {
        if (gameControls == null) return;
        gameControls.Default.Walk.performed -= OnMove;
        gameControls.Default.Walk.canceled -= OnMoveRelease;
        gameControls.Default.Run.performed -= OnRun;
        gameControls.Default.Run.canceled -= OnRun;
        gameControls.Default.Interact.performed -= OnInteract;
        gameControls.Default.Interact.canceled -= OnInteract;
        gameControls.Menus.OpenInventory.performed -= OnInventory;
        gameControls.Disable();
    }
    /// <summary>
    /// Unsubscribe from the sceneLoaded and sceneUnloaded events and disable the GameControls asset.
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        gameControls?.Disable();
    }

    #endregion

    #region === Methods =================================================================

    /// <summary>
    /// Is called when the "Walk" input of the "GameControls" Input Actions is performed.
    /// </summary>
    /// <param name="ctx">The context of the input action callback.</param>
    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveValue.Value = ctx.ReadValue<Vector2>();
    }
    /// <summary>
    /// Is called when the "Walk" input of the "GameControls" Input Actions is released.
    /// </summary>
    /// <param name="ctx">The context of the input action callback.</param>
    private void OnMoveRelease(InputAction.CallbackContext ctx)
    {
        moveValue.Value = Vector2.zero;
    }
    /// <summary>
    /// Is called when the "Run" input of the "GameControls" Input Actions is performed.
    /// </summary>
    /// <param name="ctx">The context of the input action callback.</param>
    private void OnRun(InputAction.CallbackContext ctx)
    {
        isRunning.Value = ctx.ReadValueAsButton();
    }
    /// <summary>
    /// Is called when the "Interact" input of the "GameControls" Input Actions is performed or canceled.
    /// </summary>
    /// <param name="ctx">The context of the input action callback.</param>
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            interacted.Value = true;
        else if (ctx.canceled)
            interacted.Value = false;
    }
    /// <summary>
    /// Is called when the "Open Inventory" input of the "GameControls" Input Actions is performed.
    /// </summary>
    /// <param name="ctx">The context of the input action callback.</param>
    public void OnInventory(InputAction.CallbackContext ctx)
    {
        openInventory.Value = !openInventory.Value;
    }
    /// <summary>
    /// Toggles the default controls of the game.
    /// </summary>
    /// <param name="enable">Whether to enable or disable the default controls.</param>
    public void ToggleDefaultControls(bool enable)
    {
        if (enable)
            gameControls.Default.Enable();
        else
            gameControls.Default.Disable();
    }

    #endregion
}