// Created by Sérgio Murillo da Costa Faria
// Date: 19/02/2023

using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Responsible for calculating player position based on the <see cref="Movement"/> input
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [ShowInInspector] private static bool _canMove;
    [SerializeField] private float speed;
    [SerializeField, ReadOnly] private Vector3 movement;
    [SerializeField, ReadOnly] private Rigidbody rb;

    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Movement();
    }

    /// <summary>
    /// Responsible for reading the "Movement" input from <see cref="PlayerControls"/> and moving
    /// the player accordingly.
    /// </summary>
    public void Movement()
    {
        if (!_canMove) return;
        var direction = PlayerControls.Instance.MoveValue;
        movement = Vector3.zero;
        movement = new Vector3(direction.x, 0, direction.y).normalized;

        if (movement == Vector3.zero) return;
        rb.MovePosition(transform.position + movement * speed * Time.fixedDeltaTime);
    }
}