using System.Collections;
using System.Collections.Generic;
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
            pressButtonImg.gameObject.SetActive(false);
            buttonsContainer.SetActive(true);
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
