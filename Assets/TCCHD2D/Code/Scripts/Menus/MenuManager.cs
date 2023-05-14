// Created by Sérgio Murillo da Costa Faria
// Date: 09/03/2023

using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Responsible for handling everything related to the main menu.
/// </summary>
public class MenuManager : MonoBehaviour
{
    [SerializeField, Required] private StringVariable previousScene;

    /// <summary>
    /// Loads the scene with the given name and saves the previous scene name if the scene to be loaded is the options menu.
    /// </summary>
    /// <param name="sceneName"></param>
    public void LoadScene(string sceneName)
    {
        if (sceneName == "scn_optionsMenu")
            previousScene.Value = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }

    public void NewGame()
    {
        SceneManager.LoadScene("scn_game");
        var writer = QuickSaveWriter.Create("GameSave");
        var keys = writer.GetAllKeys();
        foreach (var key in keys)
            writer.Delete(key);
        writer.Commit();
    }

    public void ContinueGame()
    {
        var reader = QuickSaveReader.Create("GameSave");
        if (reader.Exists("CurrentScene"))
        {
            SceneManager.LoadScene(reader.Read<string>("CurrentScene"));
        }
    }

    public void ExitMessage(GameObject exitPanel)
    {
        exitPanel.SetActive(!exitPanel.activeSelf);
    }

    /// <summary>
    /// Closes the game.
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
