// Created by Sérgio Murillo da Costa Faria
// Date: 17/03/2023

using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "RPG/New Dialogue"), InlineEditor]
public class DialogueData : ScriptableObject
{
    [SerializeField] private string characterName;
    [SerializeField] private DialogueLine[] dialogueLines;
    [SerializeField] private bool isTutorial;
    [SerializeField] private bool hasPlayed;
    [ShowIf("isTutorial")]
    [SerializeField] private bool resetOnExit;

    public string CharacterName => characterName;
    public DialogueLine[] DialogueLines => dialogueLines;
    public bool IsTutorial => isTutorial;
    public bool HasPlayed
    {
        get => hasPlayed;
        set => hasPlayed = value;
    }
    public bool ResetOnExit => resetOnExit;
    
#if UNITY_EDITOR
    protected void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    protected void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && resetOnExit)
        {
            hasPlayed = false;
        }
    }
#else
    protected void OnDisable()
    {
        if (resetOnExit)
        {
            hasPlayed = false;
        }
    }
#endif
}

[System.Serializable]
public class DialogueLine
{
    [SerializeField, TextArea] private string text;

    public string Text => text;

    public DialogueLine(string text)
    {
        this.text = text;
    }
}