using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SceneTransition : MonoBehaviour
{
    public RectTransform playerBar, minimap;
    public CanvasGroup playerGroup, minimapGroup;

    public Material mat;

    public float min, max, current, speedTransition, acelerationValue;
    
    float aceleration = 1f;

    bool pressed = false;

    private void Start()
    {
        playerBar.DOMoveX(-196.0001f, 0).SetEase(Ease.OutQuad);

        minimap.DOAnchorPosX(180.0001f, 0).SetEase(Ease.OutQuad);

        pressed = true;
        current = max;
    }

    private void Update()
    {
        if(current != max && current != min)
        {
            aceleration = aceleration * acelerationValue;
        }
        
        // Aumentar o valor quando a tecla de espa�o for pressionada
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            pressed = true;
        }
        
        if (pressed == true)
        {
            
            TiraUI();
            current += speedTransition * Time.deltaTime * aceleration;
            
        }
        
        // Diminuir o valor quando outra tecla for pressionada
        if (Input.GetKeyDown(KeyCode.LeftArrow)) // Por exemplo, o bot�o Shift esquerdo
        {
            pressed = false;
        }
        
        if (pressed == false)
        {
            BotaUI();
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
    
    void TiraUI()
    {
        playerBar.DOMoveX(-196.0001f, 1).SetEase(Ease.OutQuad);

        minimap.DOAnchorPosX(180.0001f, 1).SetEase(Ease.OutQuad);
    }

    void BotaUI()
    {
        playerBar.DOMoveX(196.0001f, 1).SetEase(Ease.OutQuad);

        minimap.DOAnchorPosX(-180.0001f, 1).SetEase(Ease.OutQuad);

        
    }
}
