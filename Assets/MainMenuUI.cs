using System.Collections;
using System.Collections.Generic;
using CI.QuickSave;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public Image pressButtonImg;
    public GameObject buttonsContainer;
    public GameObject continueButton;
    public float time = 1f;
    public float length = 1f;
    public bool anyButtonPressed;
    
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
            if (reader.Exists("ChangingScene"))
            {
                continueButton.SetActive(true);
            }
            else
            {
                continueButton.SetActive(false);
            }
            return;
        }
        WaitingForInput();
    }

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
}
