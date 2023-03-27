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
    [SerializeField, Required, InlineEditor, Tooltip("Bool variable that tells if the player can move or not.")]
    private BoolVariable canMove;

    [SerializeField, Required, InlineEditor, Tooltip("Bool variable that tells if the player is running or not.")]
    private BoolVariable isRunning;

    [SerializeField, Required, InlineEditor, Tooltip("Vector2 variable that tells the player's movement direction values.")]
    private Vector2Variable direction;

    [TitleGroup("Movement Variables", Alignment = TitleAlignments.Centered)]
    [SerializeField, MinValue(0), Tooltip("Player's movement speed.")]
    private float speed;

    [SerializeField, MinValue(1), Tooltip("Player's movement speed multiplier when running.")]
    private float runSpeedMultiplier = 1;

    [TitleGroup("Debug Info", Alignment = TitleAlignments.Centered)]
    [SerializeField, Required, Tooltip("Animator component of the player.")]
    private Animator animator;

    [SerializeField, ReadOnly, Tooltip("Rigidbody component of the player. If it doesn't exist, it will be added automatically.")]
    private Rigidbody rigidBody;

    [SerializeField, ReadOnly, Tooltip("The value that will be used to calculate the player's movement.")]
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