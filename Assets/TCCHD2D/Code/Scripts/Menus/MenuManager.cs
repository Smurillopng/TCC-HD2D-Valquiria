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
        var saveWriter = QuickSaveWriter.Create("GameSave");
        var keys = saveWriter.GetAllKeys();
        foreach (var key in keys)
            saveWriter.Delete(key);
        saveWriter.Commit();
        var inventoryWriter = QuickSaveWriter.Create("Inventory");
        keys = inventoryWriter.GetAllKeys();
        foreach (var key in keys)
            inventoryWriter.Delete(key);
        inventoryWriter.Commit();
        var equipmentsWriter = QuickSaveWriter.Create("EquipmentSlots");
        keys = equipmentsWriter.GetAllKeys();
        foreach (var key in keys)
            equipmentsWriter.Delete(key);
        equipmentsWriter.Commit();
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
