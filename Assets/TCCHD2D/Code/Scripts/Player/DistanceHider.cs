using System;
using System.Collections.Generic;
using UnityEngine;

public class DistanceHider : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private Camera mainCam;
    [SerializeField] private float alphaStrength = 0.1f;

    private readonly HashSet<GameObject> hiddenObjects = new HashSet<GameObject>();
    private readonly List<Renderer> newlyHiddenRenderers = new List<Renderer>();

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

        newlyHiddenRenderers.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            var hit = hits[i];
            GameObject hitObject = hit.collider.gameObject;
            if (!hiddenObjects.Contains(hitObject))
            {
                newlyHiddenObjects.Add(hitObject); // Add object to the list of newly hidden objects
                if (hitObject.TryGetComponent<Renderer>(out var hitRender))
                {
                    hitRender.material.SetFloat("_AlphaValue", alphaStrength);
                    newlyHiddenRenderers.Add(hitRender);
                }
            }
        }

        // Add newly hidden objects to the list of hidden objects
        hiddenObjects.UnionWith(newlyHiddenObjects);

        // Show objects that are no longer being hit
        foreach (var renderer in newlyHiddenRenderers)
        {
            if (!hiddenObjects.Contains(renderer.gameObject))
            {
                renderer.material.SetFloat("_AlphaValue", 1.0f);
            }
        }

        hiddenObjects.RemoveWhere(obj =>
        {
            if (obj == null)
            {
                return true;
            }

            var hit = Array.Exists(hits, hit => hit.collider != null && hit.collider.gameObject == obj);
            if (!hit)
            {
                if (obj.TryGetComponent<Renderer>(out var objRender))
                {
                    objRender.material.SetFloat("_AlphaValue", 1.0f);
                }
            }

            return !hit;
        });
    }
}