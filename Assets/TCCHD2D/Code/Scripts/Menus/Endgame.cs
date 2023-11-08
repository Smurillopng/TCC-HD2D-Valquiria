// Created by Sérgio Murillo da Costa Faria

using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class Endgame : MonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Endgame")]
    public string endgameScene;
    private SceneTransitioner _transitioner;
    #endregion ==========================================================================

    #region === Unity Methods ===========================================================
    private void Awake()
    {
        _transitioner = FindObjectOfType<SceneTransitioner>();
    }
    #endregion ==========================================================================

    #region === Methods =================================================================
    public void GameComplete()
    {
        StartCoroutine(_transitioner.TransitionTo(endgameScene));
    }
    #endregion ==========================================================================
}
