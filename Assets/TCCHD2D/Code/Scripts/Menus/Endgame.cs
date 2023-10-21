using UnityEngine;

public class Endgame : MonoBehaviour
{
    public string engameScene;
    private SceneTransitioner _transitioner;

    void Awake()
    {
        _transitioner = FindObjectOfType<SceneTransitioner>();
    }

    public void GameComplete()
    {
        StartCoroutine(_transitioner.TransitionTo(engameScene));
    }
}
