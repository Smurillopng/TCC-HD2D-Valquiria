using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneTransition : MonoBehaviour
{
    public Material mat;

    public float min, max, current, speedTransition, acelerationValue;
    
    float aceleration = 1f;

    bool pressed = false;

    private void Start()
    {
        pressed = true;
        current = max;
    }

    private void Update()
    {
        if(current != max && current != min)
        {
            aceleration = aceleration * acelerationValue;
        }
        
        // Aumentar o valor quando a tecla de espaço for pressionada
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            pressed = true;
        }
        
        if (pressed == true)
        {
            current += speedTransition * Time.deltaTime * aceleration;
            
        }
        
        // Diminuir o valor quando outra tecla for pressionada
        if (Input.GetKeyDown(KeyCode.LeftArrow)) // Por exemplo, o botão Shift esquerdo
        {
            pressed = false;
        }
        
        if (pressed == false)
        {
            current -= speedTransition * Time.deltaTime * aceleration;
        }

        if(current > max)
        {
            current = max;
            aceleration = 1;
        }
        if(current < min)
        {
            current = min;
            aceleration = 1;
        }


        // Aplique o valor ao material
        mat.SetFloat("_Cutoff_Height", current);
    }
    
    
}
