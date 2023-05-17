using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// This class is responsible for detecting objects that are between the player and the camera and hiding them.
/// </summary>
/// <remarks>
/// Created by Sérgio Murillo da Costa Faria on 23/04/2023.
/// </remarks>
[HideMonoScript]
public class DistanceHider : MonoBehaviour
{
    #region === Variables ===============================================================

    [SerializeField, Tooltip("The transform of the player.")]
    private Transform player;
    
    [SerializeField, Tooltip("The layers to be considered for object detection.")]
    private LayerMask layerMask;
    
    [SerializeField, Tooltip("The camera that will be used to calculate the distance to the objects.")]
    private Camera mainCam;
    
    [SerializeField, Tooltip("The strength of the alpha applied to the materials of the hidden objects.")]
    private float alphaStrength = 0.1f;

    // Objects that are currently hidden
    private readonly HashSet<GameObject> _hiddenObjects = new();
    // Renderers that were hidden in the current frame
    private readonly List<Renderer> _newlyHiddenRenderers = new();
    // Shader property used to set the alpha value of the material
    private static readonly int AlphaValue = Shader.PropertyToID("_AlphaValue");

    #endregion

    #region === Unity Methods ===========================================================

    /// <summary>
    /// This method sets the reference to the main camera of the scene.
    /// </summary>
    private void Awake()
    {
        mainCam = Camera.main;
    }
    /// <summary>
    /// This method detects objects that are between the player and the camera and hides them.
    /// </summary>
    private void Update()
    {
        const int maxHits = 10;
        var hits = new RaycastHit[maxHits];
        var camTransform = mainCam.transform;
        var camTransformPosition = camTransform.position;
        var direction = player.position - camTransformPosition;
        var hitCount = Physics.RaycastNonAlloc(camTransformPosition, direction, hits, direction.magnitude, layerMask);

        var newlyHiddenObjects = new List<GameObject>(); // List to store newly hidden objects

        _newlyHiddenRenderers.Clear();

        for (var i = 0; i < hitCount; i++)
        {
            var hit = hits[i];
            var hitObject = hit.collider.gameObject;
            if (_hiddenObjects.Contains(hitObject)) continue;
            newlyHiddenObjects.Add(hitObject); // Add object to the list of newly hidden objects
            if (!hitObject.TryGetComponent<Renderer>(out var hitRender)) continue;
            foreach (var mat in hitRender.materials)
            {
                mat.SetFloat(AlphaValue, alphaStrength);

            }
            _newlyHiddenRenderers.Add(hitRender);
        }

        // Add newly hidden objects to the list of hidden objects
        _hiddenObjects.UnionWith(newlyHiddenObjects);

        // Show objects that are no longer being hit
        foreach (var matRenderer in _newlyHiddenRenderers.Where(matRenderer => !_hiddenObjects.Contains(matRenderer.gameObject)))
        {
            foreach (var mat in matRenderer.materials)
            {
                mat.SetFloat(AlphaValue, 1.0f);
            }
        }

        _hiddenObjects.RemoveWhere(obj =>
        {
            if (obj == null) return true;
            
            var hit = Array.Exists(hits, hit => hit.collider != null && hit.collider.gameObject == obj);
            if (hit) return false;
            if (obj.TryGetComponent<Renderer>(out var objRender))
            {
                foreach (var mat in objRender.materials)
                {
                    mat.SetFloat(AlphaValue, 1.0f);
                }
            }
            return true;
        });
    }
    
    #endregion
}