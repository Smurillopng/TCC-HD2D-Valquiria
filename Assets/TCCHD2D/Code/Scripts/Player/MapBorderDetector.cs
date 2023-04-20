using System;
using UnityEngine;

public class MapBorderDetector : MonoBehaviour
{
    public float raycastDistance = 10.0f;
    public LayerMask borderLayer;

    void Update()
    {
        var playerTransform = transform.position;
        playerTransform.y += 0.2f;
        // Raycast in all six directions to detect borders
        RaycastHit hitUp;
        RaycastHit hitDown;
        RaycastHit hitLeft;
        RaycastHit hitRight;
        RaycastHit hitForward;
        RaycastHit hitBackward;

        Physics.Raycast(playerTransform, Vector3.up, out hitUp, raycastDistance, borderLayer);
        Physics.Raycast(playerTransform, Vector3.down, out hitDown, raycastDistance, borderLayer);
        Physics.Raycast(playerTransform, Vector3.left, out hitLeft, raycastDistance, borderLayer);
        Physics.Raycast(playerTransform, Vector3.right, out hitRight, raycastDistance, borderLayer);
        Physics.Raycast(playerTransform, Vector3.forward, out hitForward, raycastDistance, borderLayer);
        Physics.Raycast(playerTransform, Vector3.back, out hitBackward, raycastDistance, borderLayer);

        // Check if any of the raycasts hit a border
        if (hitUp.collider != null)
        {
            Debug.Log("Border hit: Up");
        }
        if (hitDown.collider != null)
        {
            Debug.Log("Border hit: Down");
        }
        if (hitLeft.collider != null)
        {
            Debug.Log("Border hit: Left");
        }
        if (hitRight.collider != null)
        {
            Debug.Log("Border hit: Right");
        }
        if (hitForward.collider != null)
        {
            Debug.Log("Border hit: Forward");
        }
        if (hitBackward.collider != null)
        {
            Debug.Log("Border hit: Backward");
        }
    }
    
    private void OnDrawGizmos()
    {
        var playerTransform = transform.position;
        playerTransform.y += 0.2f;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(playerTransform, Vector3.up * raycastDistance);
        Gizmos.DrawLine(playerTransform, Vector3.down * raycastDistance);
        Gizmos.DrawLine(playerTransform, Vector3.left * raycastDistance);
        Gizmos.DrawLine(playerTransform, Vector3.right * raycastDistance);
        Gizmos.DrawLine(playerTransform, Vector3.forward * raycastDistance);
        Gizmos.DrawLine(playerTransform, Vector3.back * raycastDistance);
    }
}