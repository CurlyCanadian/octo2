using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour {
  public enum MovementState {
    Walking,
    Sprinting,
    Crouching,
    Air,
  }

  [Header("Movement")]
  [SerializeField] [Range(1f, 10f)] [Tooltip("Normal walking speed")]
  private float _walkSpeed = 5f;
  [SerializeField] [Range(1f, 15f)] [Tooltip("Sprint speed")]
  private float _sprintSpeed = 8f;
  [SerializeField] [Range(0f, 10f)] [Tooltip("Drag applied when grounded")]
  private float _groundDrag = 5f;

  [Header("Jumping")]
  [SerializeField] [Range(1f, 20f)] [Tooltip("Upward force applied on jump")]
  private float _jumpForce = 10f;
  [SerializeField] [Range(0.1f, 2f)] [Tooltip("Cooldown between jumps")]
  private float _jumpCooldown = 0.25f;
  [SerializeField] [Range(0f, 1f)] [Tooltip("Movement force multiplier in air")]
  private float _airMultiplier = 0.4f;

  [Header("Crouching")]
  [SerializeField] [Range(1f, 10f)] [Tooltip("Movement speed while crouching")]
  private float _crouchSpeed = 3f;
  [SerializeField] [Range(0.3f, 0.9f)] [Tooltip("Collider height multiplier when crouched")]
  private float _crouchHeightMultiplier = 0.5f;
  [SerializeField] [Range(0f, 10f)] [Tooltip("Downward force on crouch start")]
  private float _crouchDownForce = 5f;

  [Header("Keybinds")]
  [SerializeField] private KeyCode _jumpKey = KeyCode.Space;
  [SerializeField] private KeyCode _sprintKey = KeyCode.LeftShift;
  [SerializeField] private KeyCode _crouchKey = KeyCode.LeftControl;

  [Header("Ground Check")]
  [SerializeField] [Tooltip("Layer mask for ground detection")]
  private LayerMask _groundLayer;
  [SerializeField] [Range(0f, 0.5f)] [Tooltip("Extra distance for ground check")]
  private float _groundCheckMargin = 0.2f;

  [Header("Slope Handling")]
  [SerializeField] [Range(0f, 60f)] [Tooltip("Maximum walkable slope angle")]
  private float _maxSlopeAngle = 45f;
  [SerializeField] [Range(0f, 100f)] [Tooltip("Downward force on slopes")]
  private float _slopeDownForce = 80f;

  [Header("References")]
  [SerializeField] [Tooltip("Transform used for forward/right direction")]
  private Transform _orientation;

  [Header("Debug")]
  public bool Grounded;
  public MovementState State;

  private const float MovementForceMultiplier = 10f;
  private const float MinSlopeAngle = 0.1f;

  private float _currentSpeed;
  private float _standHeight;
  private float _crouchHeight;
  private Vector3 _standCenter;
  private Vector3 _crouchCenter;
  private bool _readyToJump;
  private bool _exitingSlope;
  private bool _isOnSlope;
  private bool _isCrouching;
  private RaycastHit _slopeHit;
  private Vector3 _moveDirection;
  private Vector2 _inputAxis;
  private Rigidbody _rb;
  private CapsuleCollider _collider;

  private void Start() {
    _rb = GetComponent<Rigidbody>();
    _rb.freezeRotation = true;
    _readyToJump = true;
    
    _collider = GetComponent<CapsuleCollider>();
    _standHeight = _collider.height;
    _standCenter = _collider.center;
    _crouchHeight = _standHeight * _crouchHeightMultiplier;
    _crouchCenter = new Vector3(_standCenter.x, _standCenter.y - (_standHeight - _crouchHeight) * 0.5f, _standCenter.z);
    
    _currentSpeed = _walkSpeed;
  }

  private void Update() {
    CheckGround();
    CacheInput();
    HandleInput();
    UpdateMovementState();
    
    _rb.drag = Grounded ? _groundDrag : 0f;
  }

  private void FixedUpdate() {
    MovePlayer();
    ClampSpeed();
  }

  private void CheckGround() {
    float checkDistance = (_collider.height * 0.5f) + _groundCheckMargin;
    Grounded = Physics.Raycast(transform.position, Vector3.down, checkDistance, _groundLayer);
  }

  private void CacheInput() {
    _inputAxis.x = Input.GetAxisRaw("Horizontal");
    _inputAxis.y = Input.GetAxisRaw("Vertical");
  }

  private void HandleInput() {
    if (Input.GetKey(_jumpKey) && _readyToJump && Grounded) {
      _readyToJump = false;
      Jump();
      Invoke(nameof(ResetJump), _jumpCooldown);
    }

    if (Input.GetKeyDown(_crouchKey)) {
      StartCrouch();
    } else if (Input.GetKeyUp(_crouchKey)) {
      TryStandUp();
    }
  }

  private void StartCrouch() {
    _isCrouching = true;
    _collider.height = _crouchHeight;
    _collider.center = _crouchCenter;
    _rb.AddForce(Vector3.down * _crouchDownForce, ForceMode.Impulse);
  }

  private void TryStandUp() {
    float standCheckDistance = _standHeight - _crouchHeight + 0.1f;
    Vector3 checkPosition = transform.position + Vector3.up * (_crouchHeight * 0.5f);
    
    if (!Physics.Raycast(checkPosition, Vector3.up, standCheckDistance, _groundLayer)) {
      _isCrouching = false;
      _collider.height = _standHeight;
      _collider.center = _standCenter;
    }
  }

  private void UpdateMovementState() {
    if (_isCrouching) {
      State = MovementState.Crouching;
      _currentSpeed = _crouchSpeed;
    } else if (Grounded && Input.GetKey(_sprintKey)) {
      State = MovementState.Sprinting;
      _currentSpeed = _sprintSpeed;
    } else if (Grounded) {
      State = MovementState.Walking;
      _currentSpeed = _walkSpeed;
    } else {
      State = MovementState.Air;
    }
  }

  private void MovePlayer() {
    _moveDirection = _orientation.forward * _inputAxis.y + _orientation.right * _inputAxis.x;
    _isOnSlope = CheckIfOnSlope();

    if (_isOnSlope && !_exitingSlope) {
      Vector3 slopeDirection = GetSlopeMoveDirection();
      _rb.AddForce(slopeDirection * _currentSpeed * MovementForceMultiplier, ForceMode.Force);
      
      if (_rb.velocity.y > 0) {
        _rb.AddForce(Vector3.down * _slopeDownForce, ForceMode.Force);
      }
    } else if (Grounded) {
      _rb.AddForce(_moveDirection * _currentSpeed * MovementForceMultiplier, ForceMode.Force);
    } else {
      _rb.AddForce(_moveDirection * _currentSpeed * MovementForceMultiplier * _airMultiplier, 
          ForceMode.Force);
    }

    _rb.useGravity = !_isOnSlope;
  }

  private void ClampSpeed() {
    Vector3 currentVelocity = _rb.velocity;
    float horizontalSpeed = Mathf.Sqrt(currentVelocity.x * currentVelocity.x + 
        currentVelocity.z * currentVelocity.z);
    
    if (horizontalSpeed > _currentSpeed) {
      float scale = _currentSpeed / horizontalSpeed;
      _rb.velocity = new Vector3(currentVelocity.x * scale, currentVelocity.y, 
          currentVelocity.z * scale);
    }
  }

  private void Jump() {
    _exitingSlope = true;
    
    Vector3 velocity = _rb.velocity;
    velocity.y = 0f;
    _rb.velocity = velocity;
    
    _rb.AddForce(transform.up * _jumpForce, ForceMode.Impulse);
  }

  private void ResetJump() {
    _readyToJump = true;
    _exitingSlope = false;
  }

  private bool CheckIfOnSlope() {
    float checkDistance = (_collider.height * 0.5f) + 0.3f;
    if (Physics.Raycast(transform.position, Vector3.down, out _slopeHit, checkDistance)) {
      float slopeAngle = Vector3.Angle(Vector3.up, _slopeHit.normal);
      return slopeAngle > MinSlopeAngle && slopeAngle < _maxSlopeAngle;
    }
    return false;
  }

  private Vector3 GetSlopeMoveDirection() {
    return Vector3.ProjectOnPlane(_moveDirection, _slopeHit.normal).normalized;
  }

  private void OnDrawGizmosSelected() {
    if (_collider == null) return;
    
    float groundCheckDist = (_collider.height * 0.5f) + _groundCheckMargin;
    float slopeCheckDist = (_collider.height * 0.5f) + 0.3f;
    
    Gizmos.color = Grounded ? Color.green : Color.red;
    Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDist);
    
    Gizmos.color = _isOnSlope ? Color.yellow : Color.blue;
    Gizmos.DrawLine(transform.position, transform.position + Vector3.down * slopeCheckDist);
    
    if (_isCrouching) {
      float standCheckDist = _standHeight - _crouchHeight + 0.1f;
      Vector3 checkPos = transform.position + Vector3.up * (_crouchHeight * 0.5f);
      Gizmos.color = Color.cyan;
      Gizmos.DrawLine(checkPos, checkPos + Vector3.up * standCheckDist);
    }
  }
}
