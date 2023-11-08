// Created by Sérgio Murillo da Costa Faria

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class DistanceHider : MonoBehaviour
{
    #region === Variables ===============================================================

    [FoldoutGroup("Distance Hider")]
    [BoxGroup("Distance Hider/Settings")]
    [SerializeField, Tooltip("The transform of the player.")]
    private Transform player;
    
    [BoxGroup("Distance Hider/Settings")]
    [SerializeField, Tooltip("The layers to be considered for object detection.")]
    private LayerMask layerMask;
    
    [BoxGroup("Distance Hider/Settings")]
    [SerializeField, Tooltip("The camera that will be used to calculate the distance to the objects.")]
    private Camera mainCam;
    
    [BoxGroup("Distance Hider/Settings")]
    [SerializeField, Tooltip("The strength of the alpha applied to the materials of the hidden objects.")]
    private float alphaStrength = 0.1f;

    // Objects that are currently hidden
    private readonly HashSet<GameObject> _hiddenObjects = new();
    private readonly List<Renderer> _newlyHiddenRenderers = new();
    private readonly List<GameObject> _newlyHiddenObjects = new();
    private readonly List<Renderer> _toRemove = new();
    private static readonly int AlphaValue = Shader.PropertyToID("_AlphaValue");

    #endregion ==========================================================================

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

        _newlyHiddenObjects.Clear();

        for (var i = 0; i < hitCount; i++)
        {
            var hit = hits[i];
            var hitObject = hit.collider.gameObject;
            if (_hiddenObjects.Contains(hitObject)) continue;
            _newlyHiddenObjects.Add(hitObject);
            if (!hitObject.TryGetComponent<Renderer>(out var hitRender)) continue;
            foreach (var mat in hitRender.materials)
            {
                mat.SetFloat(AlphaValue, alphaStrength);
            }
            _newlyHiddenRenderers.Add(hitRender);
        }

        _hiddenObjects.UnionWith(_newlyHiddenObjects);

        _toRemove.Clear();
        foreach (var matRenderer in _newlyHiddenRenderers)
        {
            if (!_hiddenObjects.Contains(matRenderer.gameObject))
            {
                foreach (var mat in matRenderer.materials)
                {
                    mat.SetFloat(AlphaValue, 1.0f);
                }
                _toRemove.Add(matRenderer);
            }
        }
        foreach (var itemRenderer in _toRemove)
        {
            _newlyHiddenRenderers.Remove(itemRenderer);
        }

        _hiddenObjects.RemoveWhere(obj =>
        {
            if (obj.Equals(null)) return true;

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
    #endregion ==========================================================================
}