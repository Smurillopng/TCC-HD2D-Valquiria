using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateOBJ : MonoBehaviour
{
    public Vector3 velocidadeRotacao = new Vector3(0, 45, 0); // Rotação em graus por segundo

    void Update()
    {
        // Rotaciona o objeto constantemente
        transform.Rotate(velocidadeRotacao * Time.deltaTime);
    }
}
