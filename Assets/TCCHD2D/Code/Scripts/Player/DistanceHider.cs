using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class DistanceHider : SerializedMonoBehaviour
{
    public Transform player;
    public LayerMask layerMask;
    public Camera mainCam;
    public float alphaStrength = 0.1f;

    [SerializeField]
    private List<GameObject> hiddenObjects = new();

    private void Awake()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        var maxHits = 10;
        var hits = new RaycastHit[maxHits];
        var camTransform = mainCam.transform;
        var camTransformPosition = camTransform.position;
        var direction = player.position - camTransformPosition;
        var hitCount = Physics.RaycastNonAlloc(camTransformPosition, direction, hits, direction.magnitude, layerMask);

        var newlyHiddenObjects = new List<GameObject>(); // List to store newly hidden objects

        for (int i = 0; i < hitCount; i++)
        {
            var hit = hits[i];
            GameObject hitObject = hit.collider.gameObject;
            if (!hiddenObjects.Contains(hitObject))
            {
                newlyHiddenObjects.Add(hitObject); // Add object to the list of newly hidden objects
                var hitRender = hitObject.GetComponent<Renderer>();
                hitRender.material.SetFloat("_AlphaValue", alphaStrength);
            }
        }

        // Add newly hidden objects to the list of hidden objects
        foreach (var obj in newlyHiddenObjects)
        {
            hiddenObjects.Add(obj);
        }

        // Show objects that are no longer being hit
        for (int i = hiddenObjects.Count - 1; i >= 0; i--)
        {
            var obj = hiddenObjects[i];
            if (obj != null && !Array.Exists(hits, hit => hit.collider != null && hit.collider.gameObject == obj))
            {
                var objRender = obj.GetComponent<Renderer>();
                if (objRender != null) // Check if objRender is not null
                {
                    objRender.material.SetFloat("_AlphaValue", 1.0f); // Show object again by setting alpha value to 1.0f
                }
                hiddenObjects.RemoveAt(i); // Remove object from the list of hidden objects
            }
        }
    }
}