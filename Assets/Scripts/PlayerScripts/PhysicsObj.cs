using UnityEngine;


// TODO rename everthing to PhysicsObj instead
// this will make it so the player can grab and move our "physicsObj" 
// we can still have interactable buttons and animations
// largely for cleanliness reasons


// PlayerMovement:
// ├── Orientation: [Player's orientation transform] :D
// ├── Ground Layer: "Ground"

// Climbing:
// ├── Orientation: [Player's orientation transform]
// ├── Rigid Body: [Players Rigidbody]
// ├── Player Movement: [PlayerMovement script]
// ├── Wall Layer: "Wall" :D

// PhysicsObj (this script):
// ├── Orientation: [Player's orientation transform] :D
// ├── Player Movement: [PlayerMovement script]
// ├── Climbing: [Climbing script]
// ├── PhysicsObj Layer: -Interactable- "PhysicsObj" 
// it ONLY cares about rigidbody objects you can yeet

// InteractionSelector (second script):
// ├── Interactable Layer: -Interactable- "Interactable" 
// it ONLY cares about doors/buttons/animations/etc.


// Layers
// Edit > Project Settings > Tags and Layers :D
// Create: -Interactable- PhysicsObj, Interactable, Wall and Ground layers
// Layers^^^^^^^^^


// errrrmmm if you cant grab objects then 
// Ensure objects have Rigidbody
// Check object mass < 2kg (default limit) google told me so :D
// changed mass to < 4kg (played around with player weight)


// make me this you foolish mortal peasent
// Test Objects that I need make me them ^-^
// Create a Cube
// Add Rigidbody (Mass: 1.0)
// Set Layer to "PhysicsObj" 



// important!!!!!!!!!!!!!!!!!!!!!!! you must do this!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
//  Things to verify in Unity Editor:

// Layer Setup - Make sure you've created the (PhysicsObj) layer in Unity (Edit > Project Settings > Tags and Layers)
// References - Assign the required references in the Inspector:
// Orientation (player's orientation transform)
// PlayerMovement script
// Climbing script
// Player Rigidbody
// Test Objects - Objects you want to interact with need:
// Rigidbody component
// Layer set to (PhysicsObj)
//
// Mass Rules:
// Mass <= maxGrabMass = full grab/lift
// Mass <= maxPushPullMass = push/pull using player momentum
// Mass > maxPushPullMass = foolish mortal, this object cannot be moved
//
// 🎮 Default Controls:
// Hold Left Click - Grab / Push-Pull
// Release Left Click - Release object
// F - Punch objects
// Q - Throw grabbed light objects / release push-pull objects


[RequireComponent(typeof(Rigidbody))]
public class PhysicsObj : MonoBehaviour
{
    public enum InteractionState
    {
        None,
        Detected,
        Grabbing,
        Dragging,
        Punching,
        Cooldown
    }

    public enum InteractionType
    {
        Climb,
        Grab,
        Punch,
        Throw,
        Push,
        Pull,
        PushPull,
        NotMovable
    }

    public enum ObjectMoveMode
    {
        None,
        FullGrab,
        MomentumPushPull,
        Immovable
    }

    [Header("Core References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Climbing climbing;
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Player-Based Detection Settings")]
    [SerializeField] private LayerMask physicsObjLayer = -1;

    [Tooltip("How far from the player we can detect PhysicsObj objects.")]
    [SerializeField] [Range(0.5f, 5f)] private float detectionDistance = 2f;

    [Tooltip("How wide the detection spherecast is.")]
    [SerializeField] [Range(0.1f, 0.8f)] private float detectionRadius = 0.3f;

    [Tooltip("How high above the player's feet the detection starts. Think chest/hand height.")]
    [SerializeField] [Range(0f, 2f)] private float detectionHeightOffset = 0.8f;

    [Tooltip("Small forward offset so the ray does not start inside the player.")]
    [SerializeField] [Range(0f, 1f)] private float detectionForwardOffset = 0.15f;

    [Header("Player-Based Hold Position")]
    [Tooltip("How high the object is held from the player's position.")]
    [SerializeField] [Range(0f, 2f)] private float holdHeightOffset = 0.8f;

    [Tooltip("How far to the side the object is held. Positive = right, negative = left.")]
    [SerializeField] [Range(-1f, 1f)] private float holdSideOffset = 0.15f;

    [Header("Grab / Move Weight Rules")]
    [Tooltip("Objects at or below this mass can be fully lifted.")]
    [SerializeField] [Range(0.1f, 20f)] private float maxGrabMass = 2f;

    [Tooltip("Objects at or below this mass can be pushed/pulled by player momentum. You asked for this to be 8.")]
    [SerializeField] [Range(0.1f, 20f)] private float maxPushPullMass = 8f;

    [Header("Full Grab Settings")]
    [SerializeField] [Range(0.5f, 3f)] private float grabDistance = 1.5f;
    [SerializeField] [Range(1f, 20f)] private float grabForce = 10f;
    [SerializeField] [Range(0.1f, 2f)] private float grabDamping = 0.5f;
    [SerializeField] [Range(1f, 10f)] private float throwForce = 5f;

    [Header("Momentum Push / Pull Settings")]
    [Tooltip("How strongly push/pull objects follow the player's movement target.")]
    [SerializeField] [Range(1f, 30f)] private float momentumFollowStrength = 9f;

    [Tooltip("How much of the player's Rigidbody movement transfers into the object.")]
    [SerializeField] [Range(0f, 3f)] private float momentumTransferMultiplier = 1.1f;

    [Tooltip("Max speed for push/pull objects so they do not freak out.")]
    [SerializeField] [Range(1f, 15f)] private float maxPushPullObjectSpeed = 5f;

    [Tooltip("How close the push/pull object stays in front of the player.")]
    [SerializeField] [Range(0.5f, 3f)] private float pushPullHoldDistance = 1.35f;

    [Tooltip("Tiny movement deadzone for debug push/pull direction.")]
    [SerializeField] [Range(0.01f, 0.5f)] private float momentumDeadZone = 0.08f;

    [Header("Punch Settings")]
    [SerializeField] [Range(5f, 50f)] private float punchForce = 20f;
    [SerializeField] [Range(0.1f, 1f)] private float punchRange = 0.8f;
    [SerializeField] [Range(0.1f, 1f)] private float punchCooldown = 0.5f;
    [SerializeField] [Range(0.1f, 0.5f)] private float punchDuration = 0.2f;

    [Header("Input Settings")]
    [SerializeField] private bool useLeftClickForGrab = true;
    [SerializeField] private KeyCode grabKey = KeyCode.E;
    [SerializeField] private KeyCode punchKey = KeyCode.F;
    [SerializeField] private KeyCode throwKey = KeyCode.Q;

    [Header("Physics Settings")]
    [SerializeField] [Range(0f, 90f)] private float maxAngle = 60f;
    [SerializeField] [Range(0.1f, 2f)] private float objCooldown = 0.3f;
    [SerializeField] [Range(0.1f, 1f)] private float minAirborneTime = 0.2f;
    [SerializeField] [Range(0.5f, 3f)] private float minimumObjectSize = 1f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip grabSound;
    [SerializeField] private AudioClip punchSound;
    [SerializeField] private AudioClip throwSound;
    [SerializeField] private AudioClip detectionSound;

    [Header("Visual Feedback")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private GameObject punchEffect;
    [SerializeField] private LineRenderer dragLineRenderer;

    [Header("Debug")]
    public InteractionState CurrentState;
    public InteractionType LastInteractionType;
    public ObjectMoveMode CurrentMoveMode;

    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDebugRays = true;

    [Header("Debug Logs")]
    [SerializeField] private bool debugDetectionClassification = true;
    [SerializeField] private bool debugGrabDecision = true;
    [SerializeField] private bool debugMomentumPushPull = true;
    [SerializeField] [Range(0.1f, 2f)] private float momentumDebugInterval = 0.35f;

    private float lastInteractionTime;
    private float currentAirborneTime;
    private float detectionStabilityTimer;
    private float punchTimer;
    private float nextMomentumDebugTime;
    private bool wasGroundedLastFrame;
    private bool triedGrabDuringThisHold;

    private Transform currentPhysicsObj;
    private Vector3 currentInteractionPoint;
    private Vector3 currentInteractionNormal;

    private Rigidbody grabbedRigidbody;
    private Material originalMaterial;
    private Renderer physicsObjRenderer;

    private Highlightable currentHighlightable;

    private RaycastHit primaryHit;
    private bool hasValidPhysicsObj;
    private bool hasStableDetection;
    private bool isPunching;

    private float dragPlaneY;
    private Transform lastDebugClassifiedObject;

    private const float KMaxDistanceFromHitPoint = 3f;
    private const float KDetectionStabilityTime = 0.1f;

    private void OnValidate()
    {
        if (maxPushPullMass < maxGrabMass)
            maxPushPullMass = maxGrabMass;
    }

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        ResetInteractionState();
        lastInteractionTime = -objCooldown;

        if (dragLineRenderer != null)
            dragLineRenderer.enabled = false;
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (orientation == null)
        {
            Debug.LogError("PhysicsObj: Orientation reference is null!");
            isValid = false;
        }

        if (playerMovement == null)
        {
            Debug.LogError("PhysicsObj: PlayerMovement reference is null!");
            isValid = false;
        }

        if (climbing == null)
        {
            Debug.LogError("PhysicsObj: Climbing reference is null!");
            isValid = false;
        }

        if (playerRigidbody == null)
            playerRigidbody = GetComponent<Rigidbody>();

        if (playerRigidbody == null)
        {
            Debug.LogError("PhysicsObj: Player Rigidbody is missing!");
            isValid = false;
        }

        return isValid;
    }

    private void Update()
    {
        UpdateAirborneTracking();
        PerformSmartDetection();
        UpdateInteractionState();
        HandleInput();
        UpdateGrabbing();
        UpdatePunching();
        UpdateVisualFeedback();
        UpdateDebugVisualization();
    }

    private bool GrabPressedDown()
    {
        return useLeftClickForGrab ? Input.GetMouseButtonDown(0) : Input.GetKeyDown(grabKey);
    }

    private bool GrabHeld()
    {
        return useLeftClickForGrab ? Input.GetMouseButton(0) : Input.GetKey(grabKey);
    }

    private bool GrabReleased()
    {
        return useLeftClickForGrab ? Input.GetMouseButtonUp(0) : Input.GetKeyUp(grabKey);
    }

    private Transform GetPlayerBasis()
    {
        if (orientation != null)
            return orientation;

        return transform;
    }

    private Vector3 GetPlayerForward()
    {
        Transform basis = GetPlayerBasis();

        Vector3 forward = basis.forward;

        if (forward.sqrMagnitude < 0.001f)
            forward = transform.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = transform.forward;

        return forward.normalized;
    }

    private Vector3 GetPlayerRight()
    {
        Transform basis = GetPlayerBasis();

        Vector3 right = basis.right;

        if (right.sqrMagnitude < 0.001f)
            right = transform.right;

        right.y = 0f;

        if (right.sqrMagnitude < 0.001f)
            right = transform.right;

        return right.normalized;
    }

    private Vector3 GetPlayerHorizontalVelocity()
    {
        if (playerRigidbody == null)
            return Vector3.zero;

        Vector3 velocity = playerRigidbody.linearVelocity;
        velocity.y = 0f;

        return velocity;
    }

    private void UpdateAirborneTracking()
    {
        if (playerMovement.Grounded)
        {
            if (!wasGroundedLastFrame && CurrentState == InteractionState.Dragging)
                ReleaseGrabbedObject();

            currentAirborneTime = 0f;
        }
        else
        {
            currentAirborneTime += Time.deltaTime;
        }

        wasGroundedLastFrame = playerMovement.Grounded;
    }

    private Ray GetPlayerAimRay()
    {
        Vector3 origin =
            transform.position
            + Vector3.up * detectionHeightOffset
            + GetPlayerForward() * detectionForwardOffset;

        Vector3 direction = GetPlayerForward();

        return new Ray(origin, direction);
    }

    private void PerformSmartDetection()
    {
        if (CurrentState == InteractionState.Dragging && grabbedRigidbody != null)
            return;

        Ray aimRay = GetPlayerAimRay();

        bool primaryDetection = Physics.SphereCast(
            aimRay.origin,
            detectionRadius,
            aimRay.direction,
            out primaryHit,
            detectionDistance,
            physicsObjLayer,
            QueryTriggerInteraction.Ignore
        );

        if (primaryDetection && ValidateDetectedObject(primaryHit))
        {
            if (currentPhysicsObj == primaryHit.transform)
            {
                detectionStabilityTimer += Time.deltaTime;
                hasStableDetection = detectionStabilityTimer >= KDetectionStabilityTime;
            }
            else
            {
                currentPhysicsObj = primaryHit.transform;
                detectionStabilityTimer = 0f;
                hasStableDetection = false;
                UpdateHighlight();
            }

            if (hasStableDetection)
            {
                currentInteractionPoint = primaryHit.point;
                currentInteractionNormal = primaryHit.normal;
                hasValidPhysicsObj = true;

                DebugDetectedObjectCapability(currentPhysicsObj);
                PlayDetectionFeedback();
            }
        }
        else
        {
            ResetDetectionState();
        }
    }

    private bool ValidateDetectedObject(RaycastHit hit)
    {
        if (Vector3.Distance(transform.position, hit.point) > KMaxDistanceFromHitPoint)
            return false;

        float angle = Vector3.Angle(GetPlayerForward(), -hit.normal);

        if (angle > maxAngle)
            return false;

        if (hit.collider != null)
        {
            float objectSize = hit.collider.bounds.size.magnitude;

            if (objectSize < minimumObjectSize)
                return false;
        }

        return true;
    }

    private ObjectMoveMode GetMoveModeForRigidbody(Rigidbody rb)
    {
        if (rb == null)
            return ObjectMoveMode.Immovable;

        if (rb.mass <= maxGrabMass)
            return ObjectMoveMode.FullGrab;

        if (rb.mass <= maxPushPullMass)
            return ObjectMoveMode.MomentumPushPull;

        return ObjectMoveMode.Immovable;
    }

    private string GetMoveModeDebugName(ObjectMoveMode mode)
    {
        switch (mode)
        {
            case ObjectMoveMode.FullGrab:
                return "FULL GRAB / LIFT";

            case ObjectMoveMode.MomentumPushPull:
                return "MOMENTUM PUSH/PULL";

            case ObjectMoveMode.Immovable:
                return "IMMOVABLE / TOO HEAVY";

            default:
                return "NONE";
        }
    }

    private void DebugDetectedObjectCapability(Transform obj)
    {
        if (!debugDetectionClassification)
            return;

        if (obj == null)
            return;

        if (lastDebugClassifiedObject == obj)
            return;

        Rigidbody rb = obj.GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogWarning($"PhysicsObj Detection: {obj.name} has no Rigidbody. It cannot be moved.");
            lastDebugClassifiedObject = obj;
            return;
        }

        ObjectMoveMode mode = GetMoveModeForRigidbody(rb);

        Debug.Log(
            $"PhysicsObj Detection: {obj.name} | Mass: {rb.mass:F2} | Move Mode: {GetMoveModeDebugName(mode)} | " +
            $"Full Grab <= {maxGrabMass:F2}, Push/Pull <= {maxPushPullMass:F2}"
        );

        lastDebugClassifiedObject = obj;
    }

    private void UpdateInteractionState()
    {
        switch (CurrentState)
        {
            case InteractionState.None:
                if (hasValidPhysicsObj && CanStartInteraction())
                    CurrentState = InteractionState.Detected;
                break;

            case InteractionState.Detected:
                if (!hasValidPhysicsObj)
                    CurrentState = InteractionState.None;
                break;

            case InteractionState.Grabbing:
                if (grabbedRigidbody != null)
                    CurrentState = InteractionState.Dragging;
                else
                {
                    CurrentState = InteractionState.Cooldown;
                    lastInteractionTime = Time.time;
                }
                break;

            case InteractionState.Dragging:
                if (grabbedRigidbody == null)
                {
                    CurrentState = InteractionState.Cooldown;
                    lastInteractionTime = Time.time;
                }
                break;

            case InteractionState.Punching:
                if (!isPunching)
                {
                    CurrentState = InteractionState.Cooldown;
                    lastInteractionTime = Time.time;
                }
                break;

            case InteractionState.Cooldown:
                if (Time.time - lastInteractionTime >= objCooldown)
                    CurrentState = InteractionState.None;
                break;
        }
    }

    private bool CanStartInteraction()
    {
        if (Time.time - lastInteractionTime < objCooldown)
            return false;

        if (CurrentState == InteractionState.Dragging || CurrentState == InteractionState.Punching)
            return false;

        return true;
    }

    private void HandleInput()
    {
        // Reset the "already tried" safety when the player lets go.
        // This prevents immovable objects from spamming debug logs every frame.
        if (GrabReleased())
        {
            triedGrabDuringThisHold = false;
        }

        if (CurrentState == InteractionState.Detected && hasValidPhysicsObj)
        {
            // PRESS AND HOLD VERSION
            // If the player is holding grab, the object can activate even if detection happens after the initial click.
            if (GrabHeld() && !triedGrabDuringThisHold)
            {
                triedGrabDuringThisHold = true;
                TryGrabOrPushPullObject();
            }

            if (Input.GetKeyDown(punchKey))
                TryPunchObject();
        }

        if (CurrentState == InteractionState.Dragging)
        {
            if (Input.GetKeyDown(throwKey))
            {
                triedGrabDuringThisHold = false;
                ThrowObject();
            }
            else if (!GrabHeld())
            {
                // Letting go now releases the object.
                triedGrabDuringThisHold = false;
                ReleaseGrabbedObject();
                CurrentState = InteractionState.Cooldown;
                lastInteractionTime = Time.time;
            }
        }

        if (!playerMovement.Grounded && hasValidPhysicsObj && currentAirborneTime > minAirborneTime)
            TriggerClimbInteraction();
    }

    private void TryGrabOrPushPullObject()
    {
        if (currentPhysicsObj == null)
            return;

        Rigidbody rb = currentPhysicsObj.GetComponent<Rigidbody>();

        if (rb == null)
        {
            if (debugGrabDecision)
                Debug.LogWarning($"PhysicsObj Grab Decision: {currentPhysicsObj.name} cannot move because it has no Rigidbody.");

            CurrentMoveMode = ObjectMoveMode.Immovable;
            LastInteractionType = InteractionType.NotMovable;
            return;
        }

        ObjectMoveMode moveMode = GetMoveModeForRigidbody(rb);

        if (debugGrabDecision)
        {
            Debug.Log(
                $"PhysicsObj Grab Decision: {currentPhysicsObj.name} | Mass: {rb.mass:F2} | Chosen Mode: {GetMoveModeDebugName(moveMode)}"
            );
        }

        switch (moveMode)
        {
            case ObjectMoveMode.FullGrab:
                BeginFullGrab(rb);
                break;

            case ObjectMoveMode.MomentumPushPull:
                BeginMomentumPushPull(rb);
                break;

            case ObjectMoveMode.Immovable:
                Debug.LogWarning(
                    $"PhysicsObj Grab Decision: {currentPhysicsObj.name} is too heavy to move. " +
                    $"Mass: {rb.mass:F2}, Max Push/Pull Mass: {maxPushPullMass:F2}"
                );

                CurrentMoveMode = ObjectMoveMode.Immovable;
                LastInteractionType = InteractionType.NotMovable;
                CurrentState = InteractionState.Cooldown;
                lastInteractionTime = Time.time;
                break;
        }
    }

    private void BeginFullGrab(Rigidbody rb)
    {
        grabbedRigidbody = rb;
        CurrentMoveMode = ObjectMoveMode.FullGrab;
        CurrentState = InteractionState.Grabbing;
        LastInteractionType = InteractionType.Grab;

        grabbedRigidbody.useGravity = false;
        grabbedRigidbody.linearDamping = grabDamping * 5f;

        PlaySound(grabSound);

        if (debugGrabDecision)
            Debug.Log($"PhysicsObj: FULL GRAB started on {grabbedRigidbody.name}. Object will be lifted.");
    }

    private void BeginMomentumPushPull(Rigidbody rb)
    {
        grabbedRigidbody = rb;
        CurrentMoveMode = ObjectMoveMode.MomentumPushPull;
        CurrentState = InteractionState.Grabbing;
        LastInteractionType = InteractionType.PushPull;

        dragPlaneY = grabbedRigidbody.position.y;

        grabbedRigidbody.useGravity = true;
        grabbedRigidbody.linearDamping = grabDamping * 5f;

        PlaySound(grabSound);

        if (debugGrabDecision)
        {
            Debug.Log(
                $"PhysicsObj: MOMENTUM PUSH/PULL started on {grabbedRigidbody.name}. " +
                "Move your player forward/backward/sideways while holding grab to move it."
            );
        }
    }

    private Vector3 GetFullGrabWorldPos()
    {
        Vector3 targetPosition =
            transform.position
            + Vector3.up * holdHeightOffset
            + GetPlayerForward() * grabDistance
            + GetPlayerRight() * holdSideOffset;

        return targetPosition;
    }

    private Vector3 GetMomentumPushPullWorldPos()
    {
        Vector3 targetPosition =
            transform.position
            + Vector3.up * holdHeightOffset
            + GetPlayerForward() * pushPullHoldDistance
            + GetPlayerRight() * holdSideOffset;

        targetPosition.y = dragPlaneY;

        return targetPosition;
    }

    private void UpdateGrabbing()
    {
        if (CurrentState != InteractionState.Dragging || grabbedRigidbody == null)
            return;

        switch (CurrentMoveMode)
        {
            case ObjectMoveMode.FullGrab:
                UpdateFullGrabMovement();
                break;

            case ObjectMoveMode.MomentumPushPull:
                UpdateMomentumPushPullMovement();
                break;
        }
    }

    private void UpdateFullGrabMovement()
    {
        Vector3 targetPosition = GetFullGrabWorldPos();
        Vector3 direction = targetPosition - grabbedRigidbody.position;

        grabbedRigidbody.linearVelocity = direction * grabForce;

        if (dragLineRenderer != null)
        {
            dragLineRenderer.enabled = true;
            dragLineRenderer.SetPosition(0, targetPosition);
            dragLineRenderer.SetPosition(1, grabbedRigidbody.transform.position);
        }
    }

    private void UpdateMomentumPushPullMovement()
    {
        Vector3 targetPosition = GetMomentumPushPullWorldPos();

        Vector3 toTarget = targetPosition - grabbedRigidbody.position;
        toTarget.y = 0f;

        Vector3 playerHorizontalVelocity = GetPlayerHorizontalVelocity();

        Vector3 followVelocity = toTarget * momentumFollowStrength;
        Vector3 transferredMomentum = playerHorizontalVelocity * momentumTransferMultiplier;

        Vector3 desiredVelocity = followVelocity + transferredMomentum;
        desiredVelocity.y = 0f;
        desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, maxPushPullObjectSpeed);

        grabbedRigidbody.linearVelocity = new Vector3(
            desiredVelocity.x,
            grabbedRigidbody.linearVelocity.y,
            desiredVelocity.z
        );

        UpdateMomentumDebug(playerHorizontalVelocity);

        if (dragLineRenderer != null)
        {
            dragLineRenderer.enabled = true;
            dragLineRenderer.SetPosition(0, targetPosition);
            dragLineRenderer.SetPosition(1, grabbedRigidbody.transform.position);
        }
    }

    private void UpdateMomentumDebug(Vector3 playerHorizontalVelocity)
    {
        if (!debugMomentumPushPull)
            return;

        if (Time.time < nextMomentumDebugTime)
            return;

        nextMomentumDebugTime = Time.time + momentumDebugInterval;

        float forwardMomentum = Vector3.Dot(playerHorizontalVelocity, GetPlayerForward());

        string momentumState = "HOLDING / LOW MOMENTUM";

        if (forwardMomentum > momentumDeadZone)
        {
            momentumState = "PUSHING";
            LastInteractionType = InteractionType.Push;
        }
        else if (forwardMomentum < -momentumDeadZone)
        {
            momentumState = "PULLING";
            LastInteractionType = InteractionType.Pull;
        }
        else
        {
            LastInteractionType = InteractionType.PushPull;
        }

        Debug.Log(
            $"PhysicsObj Momentum: {grabbedRigidbody.name} | State: {momentumState} | " +
            $"Player Horizontal Speed: {playerHorizontalVelocity.magnitude:F2} | " +
            $"Forward Momentum: {forwardMomentum:F2} | Object Velocity: {grabbedRigidbody.linearVelocity.magnitude:F2}"
        );
    }

    private void ThrowObject()
    {
        if (grabbedRigidbody != null)
        {
            if (CurrentMoveMode == ObjectMoveMode.FullGrab)
            {
                Vector3 throwDirection = GetPlayerForward();

                grabbedRigidbody.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);

                PlaySound(throwSound);
                LastInteractionType = InteractionType.Throw;

                if (debugGrabDecision)
                    Debug.Log($"PhysicsObj: Threw {grabbedRigidbody.name}.");
            }
            else if (CurrentMoveMode == ObjectMoveMode.MomentumPushPull)
            {
                if (debugGrabDecision)
                {
                    Debug.Log(
                        $"PhysicsObj: {grabbedRigidbody.name} is in Momentum Push/Pull mode, so Q releases instead of throwing."
                    );
                }
            }
        }

        ReleaseGrabbedObject();

        CurrentState = InteractionState.Cooldown;
        lastInteractionTime = Time.time;
    }

    private void ReleaseGrabbedObject()
    {
        if (grabbedRigidbody != null)
        {
            grabbedRigidbody.useGravity = true;
            grabbedRigidbody.linearDamping = 0f;

            if (debugGrabDecision)
                Debug.Log($"PhysicsObj: Released {grabbedRigidbody.name}. Last Mode: {GetMoveModeDebugName(CurrentMoveMode)}");
        }

        grabbedRigidbody = null;
        CurrentMoveMode = ObjectMoveMode.None;
        triedGrabDuringThisHold = false;

        if (dragLineRenderer != null)
            dragLineRenderer.enabled = false;
    }

    private void TryPunchObject()
    {
        if (currentPhysicsObj == null)
            return;

        if (Vector3.Distance(transform.position, currentPhysicsObj.position) > punchRange)
            return;

        if (Time.time - lastInteractionTime < punchCooldown)
            return;

        CurrentState = InteractionState.Punching;
        LastInteractionType = InteractionType.Punch;
        isPunching = true;
        punchTimer = punchDuration;

        Rigidbody targetRb = currentPhysicsObj.GetComponent<Rigidbody>();

        if (targetRb != null)
        {
            Vector3 punchDirection = (currentPhysicsObj.position - transform.position).normalized;
            targetRb.AddForce(punchDirection * punchForce, ForceMode.Impulse);

            if (debugGrabDecision)
                Debug.Log($"PhysicsObj: Punched {currentPhysicsObj.name} with force {punchForce}.");
        }

        PlaySound(punchSound);
        ShowPunchEffect();
    }

    private void UpdatePunching()
    {
        if (!isPunching)
            return;

        punchTimer -= Time.deltaTime;

        if (punchTimer <= 0)
            isPunching = false;
    }

    private void ShowPunchEffect()
    {
        if (punchEffect != null)
        {
            GameObject effect = Instantiate(
                punchEffect,
                currentInteractionPoint,
                Quaternion.LookRotation(currentInteractionNormal)
            );

            Destroy(effect, 1f);
        }
    }

    private void TriggerClimbInteraction()
    {
        if (currentPhysicsObj == null)
            return;

        LastInteractionType = InteractionType.Climb;
        Debug.Log($"PhysicsObj: Attempting to climb {currentPhysicsObj.name}");
    }

    private void UpdateHighlight()
    {
        ClearHighlight();

        if (currentPhysicsObj != null)
        {
            currentHighlightable = currentPhysicsObj.GetComponent<Highlightable>();

            if (currentHighlightable != null)
            {
                currentHighlightable.SetHighlight(true);
                return;
            }

            physicsObjRenderer = currentPhysicsObj.GetComponent<Renderer>();

            if (physicsObjRenderer != null && highlightMaterial != null)
            {
                originalMaterial = physicsObjRenderer.material;
                physicsObjRenderer.material = highlightMaterial;
            }
        }
    }

    private void ClearHighlight()
    {
        if (currentHighlightable != null)
        {
            currentHighlightable.SetHighlight(false);
            currentHighlightable = null;
        }

        if (physicsObjRenderer != null && originalMaterial != null)
        {
            physicsObjRenderer.material = originalMaterial;
            physicsObjRenderer = null;
            originalMaterial = null;
        }
    }

    private void UpdateVisualFeedback()
    {
        if (CurrentState != InteractionState.Detected && CurrentState != InteractionState.Dragging)
        {
            ClearHighlight();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void PlayDetectionFeedback()
    {
        if (CurrentState == InteractionState.None)
            PlaySound(detectionSound);
    }

    private void ResetInteractionState()
    {
        CurrentState = InteractionState.None;
        CurrentMoveMode = ObjectMoveMode.None;
        ResetDetectionState();
        ReleaseGrabbedObject();
    }

    private void ResetDetectionState()
    {
        hasValidPhysicsObj = false;
        hasStableDetection = false;
        detectionStabilityTimer = 0f;
        currentPhysicsObj = null;
        currentInteractionPoint = Vector3.zero;
        currentInteractionNormal = Vector3.up;
        lastDebugClassifiedObject = null;
    }

    private void UpdateDebugVisualization()
    {
        if (!showDebugRays)
            return;

        Ray aimRay = GetPlayerAimRay();

        Color rayColor = CurrentState switch
        {
            InteractionState.None => hasValidPhysicsObj ? Color.yellow : Color.red,
            InteractionState.Detected => Color.green,
            InteractionState.Grabbing => Color.blue,
            InteractionState.Dragging => CurrentMoveMode == ObjectMoveMode.MomentumPushPull ? Color.yellow : Color.cyan,
            InteractionState.Punching => Color.magenta,
            InteractionState.Cooldown => Color.gray,
            _ => Color.white
        };

        Debug.DrawRay(aimRay.origin, aimRay.direction * detectionDistance, rayColor);

        if (hasValidPhysicsObj)
        {
            Debug.DrawLine(aimRay.origin, currentInteractionPoint, Color.magenta);
            Debug.DrawRay(currentInteractionPoint, currentInteractionNormal * 0.5f, Color.blue);
        }

        if (grabbedRigidbody != null)
        {
            Vector3 targetPosition = CurrentMoveMode == ObjectMoveMode.MomentumPushPull
                ? GetMomentumPushPullWorldPos()
                : GetFullGrabWorldPos();

            Debug.DrawLine(transform.position + Vector3.up * holdHeightOffset, targetPosition, Color.cyan);
            Debug.DrawLine(targetPosition, grabbedRigidbody.position, Color.yellow);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos)
            return;

        Ray aimRay = GetPlayerAimRay();

        Vector3 origin = aimRay.origin;
        Vector3 endPoint = origin + aimRay.direction * detectionDistance;

        Gizmos.color = hasValidPhysicsObj ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin, detectionRadius);
        Gizmos.DrawWireSphere(endPoint, detectionRadius);
        Gizmos.DrawLine(origin, endPoint);

        if (hasValidPhysicsObj)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(currentInteractionPoint, 0.1f);
            Gizmos.DrawRay(currentInteractionPoint, currentInteractionNormal * 0.5f);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grabDistance);
        Gizmos.DrawWireSphere(transform.position, punchRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(GetFullGrabWorldPos(), 0.15f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(GetMomentumPushPullWorldPos(), 0.15f);
    }

    private void OnDestroy()
    {
        ReleaseGrabbedObject();
        ClearHighlight();
    }
}