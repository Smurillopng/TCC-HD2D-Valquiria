using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstatiateVFX : MonoBehaviour
{
    public GameObject VisualEffect1; // Reference to your VFX Prefab.
    public GameObject VisualEffect2;
    public GameObject VisualEffect3;

    // Update is called once per frame
    void Update()
    {
        // Check if the spacebar key is pressed.
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // Instantiate the VFX Prefab at the current position.
            Instantiate(VisualEffect1, transform.position, Quaternion.identity);
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            // Instantiate the VFX Prefab at the current position.
            Instantiate(VisualEffect2, transform.position, Quaternion.identity);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            // Instantiate the VFX Prefab at the current position.
            Instantiate(VisualEffect3, transform.position, Quaternion.identity);
        }
    }
}
