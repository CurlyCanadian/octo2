using UnityEngine;
//TO DO delete this

/// <summary>
/// Handles player movement including walking, sprinting, crouching, jumping, and slope handling.
/// Uses physics-based movement with Rigidbody for realistic character control.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour {
  /// <summary>
  /// Represents the current movement state of the player.
  /// </summary>
  public enum MovementState {
    Walking,
    Sprinting,
    Crouching,
    Air,
  }

  [Header("Movement")]
  [SerializeField] [Range(1f, 10f)] [Tooltip("Normal walking speed")]
  private float walkSpeed = 5f;
  [SerializeField] [Range(1f, 15f)] [Tooltip("Sprint speed")]
  private float sprintSpeed = 8f;
  [SerializeField] [Range(0f, 10f)] [Tooltip("Drag applied when grounded")]
  private float groundDrag = 5f;

  [Header("Jumping")]
  [SerializeField] [Range(1f, 20f)] [Tooltip("Upward force applied on jump")]
  private float jumpForce = 10f;
  [SerializeField] [Range(0.1f, 2f)] [Tooltip("Cooldown between jumps")]
  private float jumpCooldown = 0.25f;
  [SerializeField] [Range(0f, 1f)] [Tooltip("Movement force multiplier in air")]
  private float airMultiplier = 0.4f;

  [Header("Crouching")]
  [SerializeField] [Range(1f, 10f)] [Tooltip("Movement speed while crouching")]
  private float crouchSpeed = 3f;
  [SerializeField] [Range(0.3f, 0.9f)] [Tooltip("Collider height multiplier when crouched")]
  private float crouchHeightMultiplier = 0.5f;
  [SerializeField] [Range(0f, 10f)] [Tooltip("Downward force on crouch start")]
  private float crouchDownForce = 5f;

  [Header("Keybinds")]
  [SerializeField] private KeyCode jumpKey = KeyCode.Space;
  [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
  [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

  [Header("Ground Check")]
  [SerializeField] [Tooltip("Layer mask for ground detection")]
  private LayerMask groundLayer;
  [SerializeField] [Range(0f, 0.5f)] [Tooltip("Extra distance for ground check")]
  private float groundCheckMargin = 0.2f;

  [Header("Slope Handling")]
  [SerializeField] [Range(0f, 60f)] [Tooltip("Maximum walkable slope angle")]
  private float maxSlopeAngle = 45f;
  [SerializeField] [Range(0f, 100f)] [Tooltip("Downward force on slopes")]
  private float slopeDownForce = 80f;
  [SerializeField] [Range(0f, 1f)] [Tooltip("Extra distance for slope checking")]
  private float slopeCheckExtraDistance = 0.3f;

  [Header("References")]
  [SerializeField] [Tooltip("Transform used for forward/right direction")]
  private Transform orientation;

  [Header("Debug")]
  public bool Grounded;
  public MovementState State;

  // Constants following Google style guide (PascalCase for public constants)
  private const float kMovementForceMultiplier = 10f;
  private const float kMinSlopeAngle = 0.1f;

  // Private fields using camelCase
  private float currentSpeed;
  private float standHeight;
  private float crouchHeight;
  private Vector3 standCenter;
  private Vector3 crouchCenter;
  private bool readyToJump;
  private bool exitingSlope;
  private bool isOnSlope;
  private bool isCrouching;
  private RaycastHit slopeHit;
  private Vector3 moveDirection;
  private Vector2 inputAxis;
  private Rigidbody rigidBody;
  private CapsuleCollider capsuleCollider;

  /// <summary>
  /// Initializes the player movement component and caches required references.
  /// Sets up initial state and calculates crouch dimensions.
  /// </summary>
  private void Start() {
    if (orientation == null) {
      Debug.LogError("PlayerMovement: Orientation reference is null! Please assign in inspector.", this);
      enabled = false;
      return;
    }

    rigidBody = GetComponent<Rigidbody>();
    if (rigidBody == null) {
      Debug.LogError("PlayerMovement: Rigidbody component is required!", this);
      enabled = false;
      return;
    }
    
    rigidBody.freezeRotation = true;
    readyToJump = true;
    
    capsuleCollider = GetComponent<CapsuleCollider>();
    if (capsuleCollider == null) {
      Debug.LogError("PlayerMovement: CapsuleCollider component is required!", this);
      enabled = false;
      return;
    }
    
    standHeight = capsuleCollider.height;
    standCenter = capsuleCollider.center;
    crouchHeight = standHeight * crouchHeightMultiplier;
    crouchCenter = new Vector3(standCenter.x, standCenter.y - (standHeight - crouchHeight) * 0.5f, standCenter.z);
    
    currentSpeed = walkSpeed;
  }

  /// <summary>
  /// Updates movement logic every frame. Handles input, state updates, and drag.
  /// </summary>
  private void Update() {
    CheckGround();
    CacheInput();
    HandleInput();
    UpdateMovementState();
    
    rigidBody.linearDamping = Grounded ? groundDrag : 0f;
  }

  /// <summary>
  /// Physics-based movement updates. Called at fixed intervals for consistent physics.
  /// </summary>
  private void FixedUpdate() {
    MovePlayer();
    ClampSpeed();
  }

  /// <summary>
  /// Performs ground detection using raycast from player center downward.
  /// </summary>
  private void CheckGround() {
    float checkDistance = (capsuleCollider.height * 0.5f) + groundCheckMargin;
    Grounded = Physics.Raycast(transform.position, Vector3.down, checkDistance, groundLayer);
  }

  /// <summary>
  /// Caches input values for the current frame to avoid multiple Input calls.
  /// </summary>
  private void CacheInput() {
    inputAxis.x = Input.GetAxisRaw("Horizontal");
    inputAxis.y = Input.GetAxisRaw("Vertical");
  }

  /// <summary>
  /// Handles all input processing including jump and crouch actions.
  /// </summary>
  private void HandleInput() {
    if (Input.GetKey(jumpKey) && readyToJump && Grounded)
    {
      readyToJump = false;
      Jump();
      Invoke(nameof(ResetJump), jumpCooldown);
    }

    if (Input.GetKeyDown(crouchKey)) {
      StartCrouch();
    } else if (Input.GetKeyUp(crouchKey)) {
      TryStandUp();
    }
  }

  /// <summary>
  /// Initiates crouching by adjusting collider dimensions and applying downward force.
  /// </summary>
  private void StartCrouch() {
    isCrouching = true;
    capsuleCollider.height = crouchHeight;
    capsuleCollider.center = crouchCenter;
    rigidBody.AddForce(Vector3.down * crouchDownForce, ForceMode.Impulse);
  }

  /// <summary>
  /// Checks if there's enough headroom above the player to stand up.
  /// </summary>
  /// <returns>True if player can stand up without collision.</returns>
  private bool HasHeadroomToStand() {
    float standHalf = standHeight * 0.5f;
    float radius = capsuleCollider.radius;
    Vector3 bottom = transform.position + Vector3.up * (standCenter.y - standHalf + radius);
    Vector3 top = transform.position + Vector3.up * (standCenter.y + standHalf - radius);
    return !Physics.CheckCapsule(bottom, top, radius, groundLayer, QueryTriggerInteraction.Ignore);
  }

  /// <summary>
  /// Attempts to transition from crouching to standing if there's sufficient headroom.
  /// </summary>
  private void TryStandUp() {
    if (HasHeadroomToStand()) {
      isCrouching = false;
      capsuleCollider.height = standHeight;
      capsuleCollider.center = standCenter;
    }
  }

  /// <summary>
  /// Updates the current movement state based on player input and ground status.
  /// </summary>
  private void UpdateMovementState() {
    if (isCrouching) {
      State = MovementState.Crouching;
      currentSpeed = crouchSpeed;
    } else if (Grounded && Input.GetKey(sprintKey)) {
      State = MovementState.Sprinting;
      currentSpeed = sprintSpeed;
    } else if (Grounded) {
      State = MovementState.Walking;
      currentSpeed = walkSpeed;
    } else {
      State = MovementState.Air;
    }
  }

  /// <summary>
  /// Applies movement forces to the rigidbody based on input and current state.
  /// Handles different movement behaviors for slopes, ground, and air.
  /// </summary>
  private void MovePlayer() {
    moveDirection = orientation.forward * inputAxis.y + orientation.right * inputAxis.x;
    isOnSlope = CheckIfOnSlope();

    if (isOnSlope && !exitingSlope) {
      Vector3 slopeDirection = GetSlopeMoveDirection();
      rigidBody.AddForce(slopeDirection * currentSpeed * kMovementForceMultiplier, ForceMode.Force);
      
      if (rigidBody.linearVelocity.y > 0) {
        rigidBody.AddForce(Vector3.down * slopeDownForce, ForceMode.Force);
      }
    } else if (Grounded) {
      rigidBody.AddForce(moveDirection * currentSpeed * kMovementForceMultiplier, ForceMode.Force);
    } else {
      rigidBody.AddForce(moveDirection * currentSpeed * kMovementForceMultiplier * airMultiplier, 
          ForceMode.Force);
    }

    rigidBody.useGravity = !isOnSlope;
  }

  /// <summary>
  /// Limits horizontal movement speed to prevent exceeding maximum allowed speed.
  /// </summary>
  private void ClampSpeed() {
    Vector3 currentVelocity = rigidBody.linearVelocity;
    float horizontalSpeed = Mathf.Sqrt(currentVelocity.x * currentVelocity.x + 
        currentVelocity.z * currentVelocity.z);
    
    if (horizontalSpeed > currentSpeed) {
      float scale = currentSpeed / horizontalSpeed;
      rigidBody.linearVelocity = new Vector3(currentVelocity.x * scale, currentVelocity.y, 
          currentVelocity.z * scale);
    }
  }

  /// <summary>
  /// Executes jump mechanics by resetting vertical velocity and applying upward impulse.
  /// </summary>
  private void Jump() {
    exitingSlope = true;
    
    Vector3 velocity = rigidBody.linearVelocity;
    velocity.y = 0f;
    rigidBody.linearVelocity = velocity;
    
    rigidBody.AddForce(transform.up * jumpForce, ForceMode.Impulse);
  }

  /// <summary>
  /// Resets jump state after cooldown period. Called via Invoke.
  /// </summary>
  private void ResetJump()
  {
    readyToJump = true;
    exitingSlope = false;
  }

  /// <summary>
  /// Determines if the player is currently on a walkable slope.
  /// </summary>
  /// <returns>True if on a slope within maximum angle limit.</returns>
  private bool CheckIfOnSlope() {
    float checkDistance = (capsuleCollider.height * 0.5f) + slopeCheckExtraDistance;
    if (Physics.Raycast(
          transform.position, Vector3.down, out slopeHit, checkDistance,
          groundLayer, QueryTriggerInteraction.Ignore)) {
      float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
      return slopeAngle > kMinSlopeAngle && slopeAngle < maxSlopeAngle;
    }
    return false;
  }

  /// <summary>
  /// Calculates movement direction projected onto the slope surface.
  /// </summary>
  /// <returns>Normalized direction vector for slope movement.</returns>
  private Vector3 GetSlopeMoveDirection() {
    return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
  }

  /// <summary>
  /// Draws debug gizmos in the scene view for ground checking, slope detection, and crouch headroom.
  /// Only visible when this GameObject is selected in the hierarchy.
  /// </summary>
  private void OnDrawGizmosSelected() {
    if (capsuleCollider == null) return;
    
    float groundCheckDistance = (capsuleCollider.height * 0.5f) + groundCheckMargin;
    float slopeCheckDistance = (capsuleCollider.height * 0.5f) + slopeCheckExtraDistance;
    
    // Ground check visualization
    Gizmos.color = Grounded ? Color.green : Color.red;
    Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    
    // Slope check visualization
    Gizmos.color = isOnSlope ? Color.yellow : Color.blue;
    Gizmos.DrawLine(transform.position, transform.position + Vector3.down * slopeCheckDistance);
    
    // Crouch headroom check visualization
    if (isCrouching) {
      float standCheckDistance = standHeight - crouchHeight + 0.1f;
      Vector3 checkPosition = transform.position + Vector3.up * (crouchHeight * 0.5f);
      Gizmos.color = Color.cyan;
      Gizmos.DrawLine(checkPosition, checkPosition + Vector3.up * standCheckDistance);
    }
  }
}
