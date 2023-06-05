// Created by Sérgio Murillo da Costa Faria
// Date: 17/03/2023

using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "RPG/New Dialogue")]
public class DialogueData : ScriptableObject
{
    [SerializeField] private string characterName;
    [SerializeField] private DialogueLine[] dialogueLines;
    [SerializeField] private bool isTutorial;
    [SerializeField] private bool hasPlayed;

    public string CharacterName => characterName;
    public DialogueLine[] DialogueLines => dialogueLines;
    public bool IsTutorial => isTutorial;
    public bool HasPlayed
    {
        get => hasPlayed;
        set => hasPlayed = value;
    }
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