using System.Collections.Generic;
using UnityEngine;

public class DistanceHider : MonoBehaviour
{
    public Transform player;
    public LayerMask layerMask;
    public Camera mainCam;
    public float alphaStrength = 0.1f;

    private HashSet<GameObject> hiddenObjects = new();

    private void Awake()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        RaycastHit[] hits;
        Vector3 direction = player.position - mainCam.transform.position;
        hits = Physics.RaycastAll(mainCam.transform.position, direction, direction.magnitude, layerMask);

        // Hide newly hit objects
        foreach (RaycastHit hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;
            var hitRender = hitObject.GetComponent<Renderer>();
            var alphaValue = hitRender.material.GetFloat("_AlphaValue");
            if (!hiddenObjects.Contains(hitObject))
            {
                hitRender.material.SetFloat("_AlphaValue", alphaStrength);
                hiddenObjects.Add(hitObject);
            }
        }

        // Show objects that are no longer being hit
        hiddenObjects.RemoveWhere(obj =>
        {
            if (!Physics.Raycast(mainCam.transform.position, direction, out RaycastHit hitInfo, direction.magnitude,
                    layerMask) || hitInfo.collider.gameObject != obj)
            {
                var hitRender = obj.GetComponent<Renderer>();
                var alphaValue = hitRender.material.GetFloat("_AlphaValue");
                hitRender.material.SetFloat("_AlphaValue", 1);
                return true;
            }

            return false;
        });
    }
}