// Created by Sérgio Murillo da Costa Faria
// Date: 08/03/2023

using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Responsible for handling the console window and commands.
/// </summary>
public class DevConsole : MonoBehaviour
{
    public static DevConsole Instance { get; private set; }

    [SerializeField] private GameObject console;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text textField;
    [SerializeField] private BoolVariable showConsole;
    [SerializeField] private string defaultSymbol = ">";

    private GameControls gameControls;

    private List<string> commandHistory = new();
    private int currentCommandIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        gameControls = new GameControls();
        gameControls.Console.ShowConsole.started += CallConsole;
        gameControls.Console.CommandHistory.started += ConsoleHistory;
        gameControls.Enable();

        inputField.onSubmit.AddListener(OnSubmit);
        textField.text = "--- Console Mode ---";
        console.SetActive(false);
    }

    /// <summary>
    /// Enable or disable the console window when the user presses the console button.
    /// </summary>
    /// <param name="ctx"></param>
    private void CallConsole(InputAction.CallbackContext ctx)
    {
        showConsole.Value = !showConsole.Value;
        console.SetActive(showConsole.Value);
        Time.timeScale = showConsole.Value ? 0 : 1;
    }
    /// <summary>
    /// Show the previous command in the console history.
    /// </summary>
    /// <param name="ctx"></param>
    private void ConsoleHistory(InputAction.CallbackContext ctx)
    {
        if (!showConsole.Value) return;
        if (currentCommandIndex < commandHistory.Count - 1)
            currentCommandIndex++;
        if (currentCommandIndex >= 0 && currentCommandIndex < commandHistory.Count)
            inputField.text = commandHistory[currentCommandIndex];
    }

    private void Update()
    {
        switch (showConsole.Value)
        {
            case false:
                return;
            case true:
                inputField.ActivateInputField();
                break;
        }
        console.SetActive(showConsole.Value);
    }

    private void OnSubmit(string input)
    {
        textField.text = $"{defaultSymbol} {input}\n{textField.text}";
        inputField.text = "";
        ExecuteCommand(input);

        // Add the command to the history
        commandHistory.Insert(0, input);
        currentCommandIndex = -1;
    }

    /// <summary>
    /// Split the input string into a method name and parameters and invoke the method.
    /// </summary>
    /// <param name="input"></param>
    private void ExecuteCommand(string input)
    {
        // Check the input and execute the corresponding cheat command
        var parts = input.Split(' ');
        var methodName = parts[0];
        var parameters = new object[parts.Length - 1];

        for (int i = 1; i < parts.Length; i++)
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
            textField.text = $"Method '{methodName}' not found.\n{textField.text}";
    }

    // --- Cheat commands ----------------------------------------------------------

    public void Clear()
    {
        textField.text = "";
    }

    public void Print(string message)
    {
        textField.text = $"{message}\n{textField.text}";
    }

    public void SetTimeScale(float timeScale)
    {
        Time.timeScale = timeScale;
        textField.text = $"Time scale set to {timeScale}.\n{textField.text}";
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
        textField.text = $"Player healed by {healAmount}.\n{textField.text}";
    }
}