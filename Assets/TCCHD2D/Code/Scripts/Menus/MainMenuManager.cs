// Created by Sérgio Murillo da Costa Faria
// Date: 09/03/2023

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Responsible for handling everything related to the main menu.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private StringVariable previousScene;
    
    /// <summary>
    /// Loads the scene with the given name and saves the previous scene name if the scene to be loaded is the options menu.
    /// </summary>
    /// <param name="sceneName"></param>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        if (sceneName == "Options Menu")
            previousScene.Value = SceneManager.GetActiveScene().name;
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
