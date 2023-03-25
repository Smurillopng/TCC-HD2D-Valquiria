// Created by Sérgio Murillo da Costa Faria
// Date: 08/03/2023

using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Responsible for handling the console window and commands.
/// </summary>
public class DevConsole : MonoBehaviour
{
    public static DevConsole Instance { get; private set; }

    [SerializeField, Tooltip("Whether to show the console window on start.")]
    private bool openOnStart;

    [SerializeField, Tooltip("X position of the console window.")]
    private float consoleX = 10;

    [SerializeField, Tooltip("Y position of the console window.")]
    private float consoleY = 10;

    [SerializeField, Tooltip("Width of the console window.")]
    private float consoleWidth = 400;

    [SerializeField, Tooltip("Height of the console window.")]
    private float consoleHeight = 600;

    [SerializeField, Tooltip("Font size to display log entries with.")]
    private int fontSize = 12;

    [SerializeField, Tooltip("Amount to scale UI by.")]
    private float scaleFactor = 1f;

    [SerializeField, Required, Tooltip("Whether to show the console window or not.")]
    private BoolVariable showConsole;

    [SerializeField, Required, Tooltip("Reference to the debug console")]
    private DebugConsole debugConsole;

    private GameControls _gameControls;

    private readonly List<string> _commandHistory = new();
    private int _currentCommandIndex = -1;
    private Rect _consoleRect;

    private Vector2 _consoleScrollPosition = Vector2.zero;
    private string _inputString = "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;



        _consoleRect = new Rect(consoleX, consoleY, consoleWidth, consoleHeight);
        _consoleScrollPosition = Vector2.zero;
    }

    private void OnEnable()
    {
        _gameControls = new GameControls();
        _gameControls.Console.ShowConsole.started += CallConsole;
        _gameControls.Console.CommandHistory.started += ConsoleHistory;
        _gameControls.Enable();
    }

    private void OnDisable()
    {
        _gameControls.Console.ShowConsole.started -= CallConsole;
        _gameControls.Console.CommandHistory.started -= ConsoleHistory;
        _gameControls.Disable();
    }

    private void Start()
    {
        if (openOnStart)
            showConsole.Value = true;
    }

    private void OnGUI()
    {
        if (!showConsole.Value)
            return;

        var matrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scaleFactor, scaleFactor, 1f));
        _consoleRect = GUILayout.Window(0, _consoleRect, ConsoleWindow, "Console");
        GUI.matrix = matrix;
    }

    private void ConsoleWindow(int id)
    {
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scaleFactor);
        var consoleStyle = GUI.skin.label;
        try { consoleStyle.fontSize = (int)(fontSize * scaleFactor); }
        catch (Exception e) { print(e.ToString()); }

        GUILayout.BeginVertical();

        _consoleScrollPosition = GUILayout.BeginScrollView(_consoleScrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        GUILayout.Label("--- Console Mode ---");

        for (var i = _commandHistory.Count - 1; i >= 0; i--)
            GUILayout.Label(_commandHistory[i]);

        GUILayout.EndScrollView();
        GUILayout.BeginHorizontal();

        GUI.SetNextControlName("inputField");
        _inputString = GUILayout.TextField(_inputString);

        if (!string.IsNullOrEmpty(_inputString))
        {
            var matchingCommands = GetMatchingCommands(_inputString);

            if (matchingCommands.Count > 0)
            {
                if (matchingCommands[0] != _inputString)
                {
                    GUILayout.BeginVertical(GUI.skin.box);

                    foreach (var command in matchingCommands.Where(command => GUILayout.Button(command)))
                        _inputString = command;

                    GUILayout.EndVertical();
                }
            }
        }

        if (_gameControls.Console.AutoComplete.triggered)
        {
            var matchingCommands = GetMatchingCommands(_inputString);

            if (matchingCommands.Count > 0)
                _inputString = matchingCommands[0];
        }

        if (GUILayout.Button("Submit") || (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return))
        {
            OnSubmit(_inputString);
            _inputString = "";
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (Event.current.type == EventType.Repaint)
            GUI.FocusControl("inputField");
    }

    /// <summary>
    /// Enable or disable the console window when the user presses the console button.
    /// </summary>
    /// <param name="ctx"></param>
    private void CallConsole(InputAction.CallbackContext ctx)
    {
        showConsole.Value = !showConsole.Value;
        Time.timeScale = showConsole.Value ? 0 : 1;
    }

    /// <summary>
    /// Show the previous command in the console history.
    /// </summary>
    /// <param name="ctx"></param>
    private void ConsoleHistory(InputAction.CallbackContext ctx)
    {
        if (!showConsole.Value) return;
        if (_currentCommandIndex < _commandHistory.Count - 1)
            _currentCommandIndex++;
        if (_currentCommandIndex >= 0 && _currentCommandIndex < _commandHistory.Count)
            _inputString = _commandHistory[_currentCommandIndex];
    }

    private List<string> GetMatchingCommands(string input)
    {
        return _availableCommands.Where(command => command.StartsWith(input, StringComparison.InvariantCultureIgnoreCase)).ToList();
    }

    private void OnSubmit(string input)
    {
        GUILayout.Label(input);
        _inputString = "";
        ExecuteCommand(input);

        _commandHistory.Insert(0, input);
        _currentCommandIndex = -1;
    }

    /// <summary>
    /// Split the input string into a method name and parameters and invoke the method.
    /// </summary>
    /// <param name="input"></param>
    private void ExecuteCommand(string input)
    {
        var parts = input.Split(' ');
        var methodName = parts[0];
        var parameters = new object[parts.Length - 1];

        for (var i = 1; i < parts.Length; i++)
        {
            var parameter = parts[i];
            if (float.TryParse(parameter, out var floatVal))
                parameters[i - 1] = floatVal;
            else
                parameters[i - 1] = parameter;
        }
        var methodInfo = typeof(DevConsole).GetMethod(methodName);
        if (methodInfo != null)
            methodInfo.Invoke(this, parameters);
        else
            _inputString = $"Method '{methodName}' not found.\n{_inputString}";
    }

    // --- Cheat commands ----------------------------------------------------------

    private readonly List<string> _availableCommands = new()
    {
        "Clear",
        "Print",
        "SetTimeScale",
        "Heal",
        "DebugMode"
        // Add more commands here
    };

    public void Clear()
    {
        _commandHistory.Clear();
    }

    public void Print(string message)
    {
        _commandHistory.Insert(0, message);
    }

    public void SetTimeScale(float timeScale)
    {
        Time.timeScale = timeScale;
        _commandHistory.Insert(0, $"{_inputString}\nTime scale set to {timeScale}.");
    }

    public void Heal(float healAmount)
    {
        var player = GameObject.FindWithTag("Player").GetComponent<UnitController>();
        player.Unit.CurrentHp += (int)healAmount;
        if (SceneManager.GetActiveScene().name == "scn_combat")
        {
            var combatCanvas = GameObject.FindWithTag("CombatUI").GetComponent<PlayerCombatHUD>();
            combatCanvas.UpdatePlayerHealth();
        }
        _commandHistory.Insert(0, $"{_inputString}\nPlayer healed for {healAmount}.");
    }

    public void DebugMode()
    {
        debugConsole.TurnOnOff();
        showConsole.Value = !showConsole.Value;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(consoleX + consoleWidth / 2, consoleY + consoleHeight / 2, 0), new Vector3(consoleWidth, consoleHeight, 0));
    }
}