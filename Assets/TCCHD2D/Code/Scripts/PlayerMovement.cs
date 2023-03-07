// Created by Sérgio Murillo da Costa Faria
// Date: 19/02/2023

using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Responsible for calculating player position based on the <see cref="Movement"/> input
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    // Private variables
    [ShowInInspector] private static bool canMove = true;
    [SerializeField, InlineEditor] private BoolVariable isRunning;
    [SerializeField] private FloatVariable speed;
    [SerializeField] private float runSpeedMultiplier;
    [SerializeField, ReadOnly] private Vector3 movementValue;
    [SerializeField, ReadOnly] private Rigidbody rigidBody;

    private void Start()
    {
        if (!gameObject.TryGetComponent(out rigidBody))
            rigidBody = gameObject.AddComponent<Rigidbody>();
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
        if (!canMove) return;
        var direction = PlayerControls.Instance.MoveValue;
        if (direction == Vector2.zero) return;
        movementValue = new Vector3(direction.x, 0, direction.y).normalized;
        if (isRunning.Value)
            movementValue *= runSpeedMultiplier;
        var newPosition = transform.position + movementValue * speed.Value * Time.fixedDeltaTime;
        rigidBody.MovePosition(newPosition);
    }
}