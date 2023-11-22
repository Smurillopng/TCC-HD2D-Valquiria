// Created by Sérgio Murillo da Costa Faria
// Date: 17/03/2023

using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "RPG/New Dialogue"), InlineEditor]
public class DialogueData : ScriptableObject
{
    [InfoBox("File name to be assign on creation"), BoxGroup("!", showLabel: false)]
    [SerializeField]
    private string id;

    [TitleGroup("Dialogue Lines", Alignment = TitleAlignments.Centered)]
    [SerializeField, GUIColor("cyan")]
    private DialogueLine[] dialogueLines;

    [TitleGroup("Additional Options", Alignment = TitleAlignments.Centered)]
    [SerializeField, GUIColor("yellow")]
    private bool isTutorial;

    [SerializeField, GUIColor("yellow")]
    private bool hasPlayed;

    [ShowIf("isTutorial")]
    [SerializeField, GUIColor("yellow")]
    private bool resetOnExit;

    public string ID
    {
        get => id;
        set => id = value;
    }
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
    [SerializeField] private string speakerName;
    [SerializeField, TextArea] private string text;

    public string SpeakerName => speakerName;
    public string Text => text;

    public DialogueLine(string speakerName, string text)
    {
        this.speakerName = speakerName;
        this.text = text;
    }
}