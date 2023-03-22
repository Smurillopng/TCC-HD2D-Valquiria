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
    [SerializeField, Required, InlineEditor]
    private BoolVariable canMove;

    [SerializeField, Required, InlineEditor]
    private BoolVariable isRunning;

    [SerializeField, Required, InlineEditor]
    private Vector2Variable direction;

    [TitleGroup("Movement Variables", Alignment = TitleAlignments.Centered)]
    [SerializeField, MinValue(0)]
    private float speed;

    [SerializeField, MinValue(1)]
    private float runSpeedMultiplier = 1;

    [TitleGroup("Debug Info", Alignment = TitleAlignments.Centered)]
    [SerializeField, Required]
    private Animator animator;

    [SerializeField, ReadOnly]
    private Rigidbody rigidBody;

    [SerializeField, ReadOnly]
    private Vector3 movementValue;

    private static readonly int SpeedX = Animator.StringToHash("SpeedX");
    private static readonly int SpeedY = Animator.StringToHash("SpeedY");
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");

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
        if (direction.Value == Vector2.zero)
        {
            animator.SetBool(IsWalking, false);
            return;
        }
        movementValue = new Vector3(direction.Value.x, 0, direction.Value.y).normalized;
        animator.SetFloat(SpeedX, movementValue.x);
        animator.SetFloat(SpeedY, movementValue.z);
        animator.SetBool(IsWalking, true);
        if (isRunning.Value)
            movementValue *= runSpeedMultiplier;
        var newPosition = transform.position + movementValue * speed * Time.fixedDeltaTime;
        rigidBody.MovePosition(newPosition);
    }
}