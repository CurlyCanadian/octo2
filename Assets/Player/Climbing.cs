using UnityEngine;

/// <summary>
/// Handles wall climbing mechanics including wall detection, climbing movement, 
/// wall jumping, and wall sticking functionality for the player character.
/// </summary>
public class Climbing : MonoBehaviour {
  /// <summary>
  /// Represents the current climbing state of the player.
  /// </summary>
  public enum ClimbingState {
    None,
    Climbing,
    Sticking,
  }

  [Header("References")]
  [SerializeField] [Tooltip("Transform used for forward direction")]
  private Transform orientation;
  [SerializeField] [Tooltip("Player rigidbody component")]
  private Rigidbody rigidBody;
  [SerializeField] [Tooltip("Player movement script reference")]
  private PlayerMovement playerMovement;
  [SerializeField] [Tooltip("Layer mask for wall detection")]
  private LayerMask wallLayer;

  [Header("Climbing Settings")]
  [SerializeField] [Range(1f, 15f)] [Tooltip("Upward climbing speed")]
  private float climbSpeed = 5f;
  [SerializeField] [Range(1f, 10f)] [Tooltip("Maximum time player can climb continuously")]
  private float maxClimbTime = 3f;
  [SerializeField] [Tooltip("Key used to initiate climbing")]
  private KeyCode climbKey = KeyCode.W;

  [Header("Climb Jumping")]
  [SerializeField] [Range(5f, 20f)] [Tooltip("Upward force for climb jump")]
  private float climbJumpUpForce = 10f;
  [SerializeField] [Range(5f, 20f)] [Tooltip("Backward force for climb jump")]
  private float climbJumpBackForce = 8f;
  [SerializeField] [Tooltip("Key used for jump actions")]
  private KeyCode jumpKey = KeyCode.Space;
  [SerializeField] [Range(1, 5)] [Tooltip("Number of wall jumps allowed")]
  private int maxClimbJumps = 2;

  [Header("Wall Sticking")]
  [SerializeField] [Range(0.5f, 5f)] [Tooltip("Maximum time player can stick to wall")]
  private float maxStickTime = 2f;

  [Header("Wall Detection")]
  [SerializeField] [Range(0.5f, 3f)] [Tooltip("Distance for wall detection")]
  private float detectionLength = 1f;
  [SerializeField] [Range(0.1f, 1f)] [Tooltip("Radius for sphere cast detection")]
  private float sphereCastRadius = 0.3f;
  [SerializeField] [Range(15f, 90f)] [Tooltip("Maximum angle to look at wall for climbing")]
  private float maxWallLookAngle = 45f;
  [SerializeField] [Range(5f, 45f)] [Tooltip("Minimum angle change to detect new wall")]
  private float minWallNormalAngleChange = 15f;

  [Header("Debug")]
  public ClimbingState CurrentState;

  // Private state variables
  private float climbTimer;
  private int climbJumpsLeft;
  private float stickTimer;
  private float wallLookAngle;
  
  // Wall detection variables
  private RaycastHit frontWallHit;
  private bool wallInFront;
  private Transform lastWall;
  private Vector3 lastWallNormal;
  
  // Constants
  private const float KClimbInputThreshold = 0.1f;
//beans
  /// <summary>
  /// Initializes climbing system and validates required references.
  /// </summary>
  private void Start() {
    if (orientation == null) {
      Debug.LogError("Climbing: Orientation reference is null! Please assign in inspector.", this);
      enabled = false;
      return;
    }

    if (rigidBody == null) {
      Debug.LogError("Climbing: Rigidbody reference is null! Please assign in inspector.", this);
      enabled = false;
      return;
    }

    if (playerMovement == null) {
      Debug.LogError("Climbing: PlayerMovement reference is null! Please assign in inspector.", this);
      enabled = false;
      return;
    }

    // Initialize climbing state
    ResetClimbingState();
  }

  /// <summary>
  /// Updates climbing logic every frame including wall detection and state management.
  /// </summary>
  private void Update() {
    CheckForWall();
    UpdateClimbingStateMachine();
    HandleClimbingMovement();
    HandleStickingBehavior();
  }

  /// <summary>
  /// Resets climbing state to default values. Called when grounded or starting new climb.
  /// </summary>
  private void ResetClimbingState() {
    climbTimer = maxClimbTime;
    climbJumpsLeft = maxClimbJumps;
    CurrentState = ClimbingState.None;
  }

  /// <summary>
  /// Main state machine for climbing behavior. Handles transitions between climbing states.
  /// </summary>
  private void UpdateClimbingStateMachine() {
    // Reset climbing abilities when grounded
    if (playerMovement.Grounded) {
      ResetClimbingState();
      return;
    }

    bool wantsToClimb = Input.GetKey(climbKey);
    bool canClimb = wallInFront && wallLookAngle < maxWallLookAngle && climbTimer > 0;

    // State 1: Climbing
    if (wantsToClimb && canClimb) {
      if (CurrentState != ClimbingState.Climbing) {
        StartClimbing();
      }
      
      // Decrease climb timer while climbing
      climbTimer -= Time.deltaTime;
      
      // Stop climbing when timer runs out
      if (climbTimer <= 0) {
        StopClimbing();
        StartSticking();
      }
    }
    // State 2: Transition to sticking when not climbing
    else if (CurrentState == ClimbingState.Climbing) {
      StopClimbing();
      if (wallInFront) {
        StartSticking();
      }
    }

    // Handle wall jump input
    if (wallInFront && Input.GetKeyDown(jumpKey) && climbJumpsLeft > 0) {
      PerformClimbJump();
    }
  }

  /// <summary>
  /// Performs wall detection using sphere casting to find walls in front of the player.
  /// </summary>
  private void CheckForWall() {
    wallInFront = Physics.SphereCast(
        transform.position,
        sphereCastRadius,
        orientation.forward,
        out frontWallHit,
        detectionLength,
        wallLayer
    );

    if (wallInFront) {
      wallLookAngle = Vector3.Angle(orientation.forward, -frontWallHit.normal);
      
      // Check if this is a new wall
      bool isNewWall = frontWallHit.transform != lastWall ||
                       Mathf.Abs(Vector3.Angle(lastWallNormal, frontWallHit.normal)) > minWallNormalAngleChange;

      // Reset climbing abilities on new wall or when grounded
      if (isNewWall || playerMovement.Grounded) {
        ResetClimbingState();
      }
    }
  }

  /// <summary>
  /// Initiates climbing state and disables gravity.
  /// </summary>
  private void StartClimbing() {
    CurrentState = ClimbingState.Climbing;
    lastWall = frontWallHit.transform;
    lastWallNormal = frontWallHit.normal;
    rigidBody.useGravity = false;
  }

  /// <summary>
  /// Handles climbing movement by setting upward velocity.
  /// </summary>
  private void HandleClimbingMovement()
  {
    if (CurrentState == ClimbingState.Climbing)
    {
      Vector3 velocity = rigidBody.linearVelocity;
      velocity.y = climbSpeed;
      rigidBody.linearVelocity = velocity;
    }
  }

  /// <summary>
  /// Stops climbing and re-enables gravity.
  /// </summary>
  private void StopClimbing() {
    if (CurrentState == ClimbingState.Climbing) {
      CurrentState = ClimbingState.None;
      rigidBody.useGravity = true;
    }
  }

  /// <summary>
  /// Executes a wall jump with upward and backward forces.
  /// </summary>
  private void PerformClimbJump() {
    Vector3 forceToApply = transform.up * climbJumpUpForce + frontWallHit.normal * climbJumpBackForce;

    // Reset vertical velocity before applying jump force
    Vector3 velocity = rigidBody.linearVelocity;
    velocity.y = 0f;
    rigidBody.linearVelocity = velocity;
    
    rigidBody.AddForce(forceToApply, ForceMode.Impulse);

    climbJumpsLeft--;
    ReleaseFromWall();
  }

  /// <summary>
  /// Initiates wall sticking by freezing horizontal movement and disabling gravity.
  /// </summary>
  private void StartSticking() {
    CurrentState = ClimbingState.Sticking;
    stickTimer = maxStickTime;

    rigidBody.useGravity = false;
    rigidBody.linearVelocity = Vector3.zero;
    rigidBody.constraints = RigidbodyConstraints.FreezePositionX | 
                           RigidbodyConstraints.FreezePositionZ | 
                           RigidbodyConstraints.FreezeRotation;
  }

  /// <summary>
  /// Handles wall sticking behavior and automatic release conditions.
  /// </summary>
  private void HandleStickingBehavior() {
    if (CurrentState != ClimbingState.Sticking) return;

    stickTimer -= Time.deltaTime;

    // Check for release conditions
    bool timerExpired = stickTimer <= 0f;
    bool playerInputDetected = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > KClimbInputThreshold ||
                              Mathf.Abs(Input.GetAxisRaw("Vertical")) > KClimbInputThreshold;
    bool jumpPressed = Input.GetKeyDown(jumpKey);

    if (timerExpired || playerInputDetected || jumpPressed) {
      ReleaseFromWall();
    }
  }

  /// <summary>
  /// Releases player from wall by restoring normal physics constraints.
  /// </summary>
  private void ReleaseFromWall() {
    CurrentState = ClimbingState.None;
    rigidBody.useGravity = true;
    rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
  }

  /// <summary>
  /// Draws debug gizmos for wall detection visualization.
  /// Only visible when this GameObject is selected in the hierarchy.
  /// </summary>
  private void OnDrawGizmosSelected() {
    if (orientation == null) return;

    // Wall detection sphere cast visualization
    Gizmos.color = wallInFront ? Color.red : Color.green;
    Vector3 castStart = transform.position;
    Vector3 castEnd = castStart + orientation.forward * detectionLength;
    
    Gizmos.DrawWireSphere(castStart, sphereCastRadius);
    Gizmos.DrawWireSphere(castEnd, sphereCastRadius);
    Gizmos.DrawLine(castStart, castEnd);

    // Current state visualization
    if (wallInFront) {
      Gizmos.color = CurrentState switch {
        ClimbingState.Climbing => Color.blue,
        ClimbingState.Sticking => Color.yellow,
        _ => Color.white
      };
      Gizmos.DrawSphere(frontWallHit.point, 0.1f);
      Gizmos.DrawRay(frontWallHit.point, frontWallHit.normal * 0.5f);
    }
  }
}
