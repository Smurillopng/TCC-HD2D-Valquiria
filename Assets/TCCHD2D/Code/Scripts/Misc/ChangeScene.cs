// Created by Sérgio Murillo da Costa Faria
// Date: 09/03/2023

using UnityEngine;

public class ChangeScene : MonoBehaviour
{
    public void ChangeSceneTo(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
