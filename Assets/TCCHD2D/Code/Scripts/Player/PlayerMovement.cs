using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This class is responsible for calculating player position based on the <see cref="Movement"/> input
/// </summary>
/// <remarks>
/// Created by Sérgio Murillo da Costa Faria on 19/02/2023.
/// </remarks>
public class PlayerMovement : MonoBehaviour
{
    //TODO: Add running animation

    #region === Variables ===============================================================

    [TitleGroup("Input Variables", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    [Required]
    [InlineEditor]
    [Tooltip("Bool variable that tells if the player can move or not.")]
    private BoolVariable canMove;

    [SerializeField]
    [Required]
    [InlineEditor]
    [Tooltip("Bool variable that tells if the player is running or not.")]
    private BoolVariable isRunning;

    [SerializeField]
    [Required]
    [InlineEditor]
    [Tooltip("Vector2 variable that tells the player's movement direction values.")]
    private Vector2Variable direction;

    [TitleGroup("Movement Variables", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    [MinValue(0)]
    [Tooltip("Player's movement speed.")]
    private float speed;

    [SerializeField]
    [MinValue(1)]
    [Tooltip("Player's movement speed multiplier when running.")]
    private float runSpeedMultiplier = 1;
    
    [TitleGroup("Detection", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    [Tooltip("The layer that represents the ground.")]
    private LayerMask groundLayer;
    [SerializeField]
    [Range(0,1)]
    [Tooltip("The distance of the raycast that detects the horizontal and forward collisions.")]
    private float rayDistance;
    [SerializeField]
    [Range(0,1)]
    [Tooltip("The distance of the raycast that detects the diagonal collisions.")]
    private float rayDistanceDiagonal;
    [SerializeField]
    [Range(0,1)]
    [Tooltip("How much the player will be slowed down when near the edge of a tile.")]
    private float slowFactor;

    [TitleGroup("Debug Info", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    [Required]
    [Tooltip("Animator component of the player.")]
    private Animator animator;

    [SerializeField] 
    private SpawnController spawnController;

    [SerializeField]
    [ReadOnly]
    [Tooltip("Rigidbody component of the player. If it doesn't exist, it will be added automatically.")]
    private Rigidbody rigidBody;

    [SerializeField]
    [ReadOnly]
    [Tooltip("The value that will be used to calculate the player's movement.")]
    private Vector3 movementValue;

    public BoolVariable CanMove => canMove;
    public Vector3 NewPosition { get; private set; }
    public Vector3 MovementValue
    {
        get => movementValue;
        set => movementValue = value;
    }

    public Vector2 Direction
    {
        get => direction.Value;
        set => direction.Value = value;
    }

    private static readonly int SpeedX = Animator.StringToHash("SpeedX");
    private static readonly int SpeedY = Animator.StringToHash("SpeedY");
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");

    #endregion

    #region === Unity Methods ===========================================================

    private void Awake()
    {
        if (!gameObject.TryGetComponent(out rigidBody))
            rigidBody = gameObject.AddComponent<Rigidbody>();

        var reader = QuickSaveReader.Create("GameSave");
        if (reader.Exists("ChangingScene"))
        {
            if (reader.Exists("SpawnStart") ||
                reader.Exists("SpawnEnd") && reader.Read<bool>("ChangingScene").Equals(true))
            {
                var writer = QuickSaveWriter.Create("GameSave");
                if (reader.Read<bool>("SpawnStart").Equals(true))
                {
                    transform.position = spawnController.SpawnStart.position;
                    writer.Write("ChangingScene", false);
                    writer.Commit();
                }
                else if (reader.Read<bool>("SpawnEnd").Equals(true))
                {
                    transform.position = spawnController.SpawnEnd.position;
                    writer.Write("ChangingScene", false);
                    writer.Commit();
                }
            }
        }

        if (reader.Exists("LastScene"))
        {
            if (reader.Exists("ChangingScene") && reader.Read<bool>("ChangingScene").Equals(false))
                if (SceneManager.GetActiveScene().name != reader.Read<string>("LastScene"))
                {
                    print("chegou aqui");
                    gameObject.transform.position = reader.Read<Vector3>("PlayerPosition");
                }
        }

        var save = QuickSaveWriter.Create("GameSave");
        save.Write("CurrentScene", SceneManager.GetActiveScene().name);
        save.Commit();
    }

    private void OnEnable()
    {
        direction.Value = Vector2.zero;
        movementValue = Vector3.zero;
        animator.SetBool(IsWalking, false);
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        var position = transform.position;
        position.y += 0.2f;
        Gizmos.DrawRay(position, new Vector3(Vector3.right.x, -1, Vector3.right.z) * rayDistanceDiagonal);
        Gizmos.DrawRay(position, new Vector3(Vector3.left.x, -1, Vector3.left.z) * rayDistanceDiagonal);
        Gizmos.DrawRay(position, new Vector3(Vector3.forward.x, -1, Vector3.forward.z) * rayDistanceDiagonal);
        Gizmos.DrawRay(position, new Vector3(Vector3.back.x, -1, Vector3.back.z) * rayDistanceDiagonal);
    }

    #endregion

    #region === Methods =================================================================

    /// <summary>
    /// Responsible for reading the "Movement" input from <see cref="PlayerControls"/> and moving
    /// the player accordingly.
    /// </summary>
    public void Movement()
    {
        if (!canMove.Value || direction.Value == Vector2.zero)
        {
            animator.SetBool(IsWalking, false);
            return;
        }

        movementValue = new Vector3(direction.Value.x, 0, direction.Value.y).normalized;

        var rayPosition = transform.position;
        rayPosition.y += 0.2f;
        const float angle = 45 * Mathf.Deg2Rad;
        var dir = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));

        var dirRight = Vector3.right;
        var dirLeft = Vector3.left;
        var dirForward = Vector3.forward;
        var dirBack = Vector3.back;

        Ray rightRay = new Ray(rayPosition, new Vector3(dirRight.x, -1, dirRight.z));
        Ray leftRay = new Ray(rayPosition, new Vector3(dirLeft.x, -1, dirLeft.z));
        Ray forwardRay = new Ray(rayPosition, new Vector3(dirForward.x, -1, dirForward.z));
        Ray backRay = new Ray(rayPosition, new Vector3(dirBack.x, -1, dirBack.z));

        if ((Physics.Raycast(rayPosition, dir, rayDistance, groundLayer) && direction.Value.x > 0)
            || (!Physics.Raycast(rightRay, rayDistanceDiagonal, groundLayer) && direction.Value.x > 0))
        {
            movementValue *= slowFactor;
        }
        else if ((Physics.Raycast(rayPosition, -dir, rayDistance, groundLayer) && direction.Value.x < 0)
                 || (!Physics.Raycast(leftRay, rayDistanceDiagonal, groundLayer) && direction.Value.x < 0))
        {
            movementValue *= slowFactor;
        }
        else if ((Physics.Raycast(rayPosition, dirForward, rayDistance, groundLayer) && direction.Value.y > 0)
                 || (!Physics.Raycast(forwardRay, rayDistanceDiagonal, groundLayer) && direction.Value.y > 0))
        {
            movementValue *= slowFactor;
        }
        else if ((Physics.Raycast(rayPosition, dirBack, rayDistance, groundLayer) && direction.Value.y < 0)
                 || (!Physics.Raycast(backRay, rayDistanceDiagonal, groundLayer) && direction.Value.y < 0))
        {
            movementValue *= slowFactor;
        }

        if (direction.Value.x != 0 && direction.Value.y != 0)
        {
            animator.SetFloat(SpeedX, 0);
            animator.SetFloat(SpeedY, direction.Value.y > 0 ? 1 : -1);
        }
        else
        {
            animator.SetFloat(SpeedX, movementValue.x);
            animator.SetFloat(SpeedY, movementValue.z);
        }

        animator.SetBool(IsWalking, true);

        if (isRunning.Value)
            movementValue *= runSpeedMultiplier;

        NewPosition = transform.position + (movementValue * (speed * Time.fixedDeltaTime));
        rigidBody.MovePosition(NewPosition);
    }

    #endregion
}