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
[HideMonoScript]
public class PlayerMovement : MonoBehaviour
{
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

    [TitleGroup("Debug Info", Alignment = TitleAlignments.Centered)]
    [SerializeField]
    [Required]
    [Tooltip("Animator component of the player.")]
    private Animator animator;

    [SerializeField] 
    [Tooltip("The spawn controller of the game.")]
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
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");

    private Ray _rightRay, _leftRay, _forwardRay, _backRay;

    private readonly Vector3 _dirRight = Vector3.right,
        _dirLeft = Vector3.left,
        _dirForward = Vector3.forward,
        _dirBack = Vector3.back;

    #endregion

    #region === Unity Methods ===========================================================

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the Rigidbody component if it doesn't exist and loads the player's position from the last save file.
    /// </summary>
    private void Awake()
    {
        if (!gameObject.TryGetComponent(out rigidBody))
            rigidBody = gameObject.AddComponent<Rigidbody>();
        var reader = QuickSaveReader.Create("GameInfo");
        if (!reader.Exists("ChangingScene"))
        {
            var writer = QuickSaveWriter.Create("GameInfo");
            writer.Write("ChangingScene", false);
            writer.Commit();
            var saveReader = QuickSaveReader.Create("GameSave");
            if (saveReader.Exists("PlayerPosition"))
                transform.position = saveReader.Read<Vector3>("PlayerPosition");
        }
        
        if (reader.Exists("ChangingScene"))
        {
            if (reader.Exists("SpawnStart") ||
                reader.Exists("SpawnEnd") && reader.Read<bool>("ChangingScene").Equals(true))
            {
                var writer = QuickSaveWriter.Create("GameInfo");
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
                    gameObject.transform.position = reader.Read<Vector3>("PlayerPosition");
                }
        }

        if (reader.Exists("ChangingScene") && reader.Read<bool>("ChangingScene").Equals(false))
        {
            if (PlayerControls.Instance.SceneMap.TryGetValue(SceneManager.GetActiveScene().name, out var value))
            {
                if (value == SceneType.Game)
                {
                    gameObject.transform.position = reader.Read<Vector3>("PlayerPosition");
                }
            }
        }

        var save = QuickSaveWriter.Create("GameInfo");
        save.Write("CurrentScene", SceneManager.GetActiveScene().name);
        save.Commit();
    }
    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Resets player movement values and sets the IsWalking parameter in the animator to false.
    /// </summary>
    private void OnEnable()
    {
        direction.Value = Vector2.zero;
        movementValue = Vector3.zero;
        animator.SetBool(IsWalking, false);
    }

    private void OnDisable()
    {
        var writer = QuickSaveWriter.Create("GameInfo");
        writer.Write("PlayerPosition", transform.position);
        writer.Commit();
    }

    /// <summary>
    /// Called every fixed framerate frame.
    /// Calculates player movement based on input and updates the Rigidbody component.
    /// </summary>
    private void FixedUpdate()
    {
        Movement();
    }
    /// <summary>
    /// Draws gizmos in the scene view.
    /// Draws four diagonal rays from the player position to help visualize the ground detection rays.
    /// </summary>
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
            animator.SetBool(IsRunning, false);
            return;
        }

        movementValue = new Vector3(direction.Value.x, 0, direction.Value.y).normalized;

        var rayPosition = transform.position;
        rayPosition.y += 0.2f;
        const float angle = 45 * Mathf.Deg2Rad;
        var dir = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));

        _rightRay = new Ray(rayPosition, new Vector3(_dirRight.x, -1, _dirRight.z));
        _leftRay = new Ray(rayPosition, new Vector3(_dirLeft.x, -1, _dirLeft.z));
        _forwardRay = new Ray(rayPosition, new Vector3(_dirForward.x, -1, _dirForward.z));
        _backRay = new Ray(rayPosition, new Vector3(_dirBack.x, -1, _dirBack.z));

        if ((Physics.Raycast(rayPosition, dir, rayDistance, groundLayer) && direction.Value.x > 0)
            || (!Physics.Raycast(_rightRay, rayDistanceDiagonal, groundLayer) && direction.Value.x > 0))
        {
            movementValue = new Vector3(0, 0, direction.Value.y);
        }
        else if ((Physics.Raycast(rayPosition, -dir, rayDistance, groundLayer) && direction.Value.x < 0)
                 || (!Physics.Raycast(_leftRay, rayDistanceDiagonal, groundLayer) && direction.Value.x < 0))
        {
            movementValue = new Vector3(0, 0, direction.Value.y);
        }
        else if ((Physics.Raycast(rayPosition, _dirForward, rayDistance, groundLayer) && direction.Value.y > 0)
                 || (!Physics.Raycast(_forwardRay, rayDistanceDiagonal, groundLayer) && direction.Value.y > 0))
        {
            movementValue = new Vector3(direction.Value.x, 0, 0);
        }
        else if ((Physics.Raycast(rayPosition, _dirBack, rayDistance, groundLayer) && direction.Value.y < 0)
                 || (!Physics.Raycast(_backRay, rayDistanceDiagonal, groundLayer) && direction.Value.y < 0))
        {
            movementValue = new Vector3(direction.Value.x, 0, 0);
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

        if (isRunning.Value)
        {
            movementValue *= runSpeedMultiplier;
            animator.SetBool(IsRunning, true);
        }
        else
        {
            animator.SetBool(IsWalking, true);
            animator.SetBool(IsRunning, false);
        }


        NewPosition = transform.position + (movementValue * (speed * Time.fixedDeltaTime));
        rigidBody.MovePosition(NewPosition);
    }

    #endregion
}