using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneTransition : MonoBehaviour
{
    public Material mat;

    public float min, max, current;

    bool pressed = false;

    private void Update()
    {
        // Aumentar o valor quando a tecla de espaço for pressionada
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            pressed = true;
        }
        
        if (pressed == true)
        {
            current += 1f * Time.deltaTime;
        }
        
        // Diminuir o valor quando outra tecla for pressionada
        if (Input.GetKeyDown(KeyCode.LeftArrow)) // Por exemplo, o botão Shift esquerdo
        {
            pressed = false;
        }
        
        if (pressed == false)
        {
            current -= 1f * Time.deltaTime;
        }

        if(current > max)
        {
            current = max;
        }
        if(current < min)
        {
            current = min;
        }


        // Aplique o valor ao material
        mat.SetFloat("_Cutoff_Height", current);
    }
    
    
}
