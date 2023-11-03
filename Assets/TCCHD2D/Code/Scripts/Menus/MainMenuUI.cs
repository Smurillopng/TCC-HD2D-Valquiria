// Created by Sérgio Murillo da Costa Faria

using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

[HideMonoScript]
public class MainMenuUI : MonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Main Menu UI")]
    [SerializeField, HideLabel, Required, PreviewField("pressButtonImg", 128, ObjectFieldAlignment.Center, FilterMode.Point)]

    private Image pressButtonImg;
    [BoxGroup("Main Menu UI/Objects", true)]
    [SerializeField, Required, Tooltip("The container that holds the buttons")]

    private GameObject buttonsContainer;
    [BoxGroup("Main Menu UI/Objects", true)]
    [SerializeField, Required, Tooltip("The continue button")]

    private GameObject continueButton;
    [BoxGroup("Main Menu UI/Transition Settings", true)]
    [SerializeField, Min(1), Tooltip("The time to fade in and out")]

    private float time = 1f;
    [BoxGroup("Main Menu UI/Transition Settings", true)]
    [SerializeField, Min(1), Tooltip("The length of the fade in and out")]

    private float length = 1f;
    [BoxGroup("Main Menu UI/Debug", true)]
    [SerializeField, ReadOnly, Tooltip("If any button was pressed")]

    private bool anyButtonPressed;
    #endregion ==========================================================================

    #region === Unity Methods ===========================================================
    private void Update()
    {
        if (anyButtonPressed)
        {
            if (!pressButtonImg.gameObject.activeSelf) return;
            pressButtonImg.gameObject.SetActive(false);
            buttonsContainer.SetActive(true);
            var writer = QuickSaveWriter.Create("GameSave");
            writer.Commit();
            var reader = QuickSaveReader.Create("GameSave");
            continueButton.SetActive(reader.Exists("CurrentScene"));
            return;
        }
        WaitingForInput();
    }
    #endregion ==========================================================================

    #region === Methods =================================================================
    private void WaitingForInput()
    {
        if (!anyButtonPressed)
        {
            var color = pressButtonImg.color;
            color.a = Mathf.Lerp(0, 1, Mathf.PingPong(Time.time / time, length));
            pressButtonImg.color = color;
        }
        if (Input.anyKeyDown)
        {
            anyButtonPressed = true;
        }
    }

    public void HideButtons()
    {
        pressButtonImg.gameObject.SetActive(false);
        buttonsContainer.SetActive(false);
    }

    public void ShowButtons()
    {
        buttonsContainer.SetActive(true);
    }
    #endregion ==========================================================================
}