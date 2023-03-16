// Created by Sérgio Murillo da Costa Faria
// Date: 16/03/2023

using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Responsible for controlling the in game UI, dialogues and menus.
/// </summary>
public class PlayerIngameHUD : MonoBehaviour
{
    public static PlayerIngameHUD Instance { get; private set; }

    [SerializeField]
    private GameObject dialoguePanel;

    [SerializeField]
    private Image dialogueBoxBorder;

    [SerializeField]
    private TMP_Text speakerName;

    [SerializeField]
    private TMP_Text dialogueText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public void ChangeSceneTo(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}