// Created by Sérgio Murillo da Costa Faria
// Date: 19/02/2023

using Sirenix.OdinInspector;
using UnityEngine;

//TODO: Add walking/running/idle animations

/// <summary>
/// Responsible for calculating player position based on the <see cref="Movement"/> input
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    // Private variables
    [TitleGroup("Input Variables", Alignment = TitleAlignments.Centered)]
    [SerializeField, InlineEditor] 
    private BoolVariable canMove;
    
    [SerializeField, InlineEditor] 
    private BoolVariable isRunning;
    
    [SerializeField, InlineEditor] 
    private Vector2Variable direction;
    
    [TitleGroup("Movement Variables", Alignment = TitleAlignments.Centered)]
    [SerializeField, MinValue(0)] 
    private float speed;
    
    [SerializeField, MinValue(1)] 
    private float runSpeedMultiplier = 1;
    
    [TitleGroup("Debug Info", Alignment = TitleAlignments.Centered)]
    [SerializeField, ReadOnly] 
    private Rigidbody rigidBody;

    [SerializeField, ReadOnly] 
    private Vector3 movementValue;

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
        if (!canMove.Value) return;
        if (direction.Value == Vector2.zero) {return;}
        movementValue = new Vector3(direction.Value.x, 0, direction.Value.y).normalized;
        if (isRunning.Value)
            movementValue *= runSpeedMultiplier;
        var newPosition = transform.position + movementValue * speed * Time.fixedDeltaTime;
        rigidBody.MovePosition(newPosition);
    }
}