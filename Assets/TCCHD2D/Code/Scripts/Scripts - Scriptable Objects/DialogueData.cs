// Created by Sérgio Murillo da Costa Faria
// Date: 17/03/2023

using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "RPG/New Dialogue")]
public class DialogueData : ScriptableObject
{
    [SerializeField] private string characterName;
    [SerializeField] private string[] dialogueLines;

    public string CharacterName => characterName;
    public string[] DialogueLines => dialogueLines;
}