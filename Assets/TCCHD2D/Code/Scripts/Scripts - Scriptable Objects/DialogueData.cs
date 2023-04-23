// Created by Sérgio Murillo da Costa Faria
// Date: 17/03/2023

using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "RPG/New Dialogue")]
public class DialogueData : ScriptableObject
{
    [SerializeField] private string characterName;
    [SerializeField] private DialogueLine[] dialogueLines;

    public string CharacterName => characterName;
    public DialogueLine[] DialogueLines => dialogueLines;
}

[System.Serializable]
public class DialogueLine
{
    [SerializeField, TextArea] private string text;

    public string Text => text;

    public DialogueLine(string id, string text, bool playAudio, AudioClip audioClip = null)
    {
        this.text = text;
    }
}