using System;
using UnityEngine;
using DG.Tweening;

public class SceneTransition : MonoBehaviour
{
    public RectTransform playerBar, minimap;
    public Material mat;
    public float min, max, current, speedTransition, acelerationValue;
    
    private float _aceleration = 1f;
    private bool _pressed;
    private static readonly int CutoffHeight = Shader.PropertyToID("_Cutoff_Height");

    private void Start()
    {
        playerBar.DOAnchorPosX(-196.0001f, 0).SetEase(Ease.OutQuad);
        minimap.DOAnchorPosX(180.0001f, 0).SetEase(Ease.OutQuad);

        _pressed = true;
        current = max;
    }

    private void Update()
    {
        if(current != max && current != min)
        {
            _aceleration *= acelerationValue;
        }
        
        // Aumentar o valor quando a tecla de espaço for pressionada
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            _pressed = true;
        }
        
        if (_pressed)
        {
            TiraUI();
            current += speedTransition * Time.deltaTime * _aceleration;
        }
        
        // Diminuir o valor quando outra tecla for pressionada
        if (Input.GetKeyDown(KeyCode.LeftArrow)) // Por exemplo, o bot�o Shift esquerdo
        {
            _pressed = false;
        }
        
        if (_pressed == false)
        {
            if(Math.Abs(current - min) < 0.01)
            {
                BotaUI();
            }
            current -= speedTransition * Time.deltaTime * _aceleration;
        }

        if(current > max)
        {
            current = max;
            _aceleration = 1;
        }
        if(current < min)
        {
            current = min;
            _aceleration = 1;
        }
        // Aplique o valor ao material
        mat.SetFloat(CutoffHeight, current);
    }

    private void TiraUI()
    {
        playerBar.DOAnchorPosX(-196.0001f, 1).SetEase(Ease.OutQuad);
        minimap.DOAnchorPosX(180.0001f, 1).SetEase(Ease.OutQuad);
    }

    private void BotaUI()
    {
        playerBar.DOAnchorPosX(196.0001f, 1).SetEase(Ease.OutQuad);
        minimap.DOAnchorPosX(-180.0001f, 1).SetEase(Ease.OutQuad);
    }
}
