using UnityEngine;

// renamed everthing to PhysicsObj instead
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
// ├── Player Camera: [Auto assigns to Main Camera]
// ├── PhysicsObj Layer: -Interactable- "PhysicsObj" 
// it ONLY cares about rigidbody objects you can yeet

// InteractionSelector (second script):
// ├── Player Camera: [Auto assigns Main Camera]
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
// Main Camera (auto assigns if not set)
// Test Objects - Objects you want to interact with need:
// Rigidbody component
// Layer set to (PhysicsObj)
// Mass ≤ maxGrabMass (or adjust maxGrabMass in Inspector)
// 🎮 Default Controls:

// Left Click - Grab / Release objects (if enabled below)
// F - Punch objects
// Q - Throw grabbed objects


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
        Throw
    }

    [Header("Core References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Climbing climbing;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform grabPoint;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask physicsObjLayer = -1;
    [SerializeField] [Range(0.5f, 5f)] private float detectionDistance = 2f;
    [SerializeField] [Range(0.1f, 0.8f)] private float detectionRadius = 0.3f;
    [SerializeField] [Range(0f, 1.5f)] private float detectionHeightOffset = 0.8f;

    [Header("Grab & Drag Settings")]
    [SerializeField] [Range(0.5f, 3f)] private float grabDistance = 1.5f;
    [SerializeField] [Range(1f, 20f)] private float grabForce = 10f;
    [SerializeField] [Range(0.1f, 2f)] private float grabDamping = 0.5f;
    [SerializeField] [Range(1f, 10f)] private float throwForce = 5f;
    [SerializeField] [Range(0.1f, 5f)] private float maxGrabMass = 2f;

    [Header("Punch Settings")]
    [SerializeField] [Range(5f, 50f)] private float punchForce = 20f;
    [SerializeField] [Range(0.1f, 1f)] private float punchRange = 0.8f;
    [SerializeField] [Range(0.1f, 1f)] private float punchCooldown = 0.5f;
    [SerializeField] [Range(0.1f, 0.5f)] private float punchDuration = 0.2f;

    [Header("Input Settings")]
    [SerializeField] private bool useLeftClickForGrab = true; // 👈 YOU WANT THIS TRUE
    [SerializeField] private KeyCode grabKey = KeyCode.E;     // fallback if you turn mouse off
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
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDebugRays = true;

    private float lastInteractionTime;
    private float currentAirborneTime;
    private float detectionStabilityTimer;
    private float punchTimer;
    private bool wasGroundedLastFrame;

    private Transform currentPhysicsObj;
    private Vector3 currentInteractionPoint;
    private Vector3 currentInteractionNormal;

    private Rigidbody grabbedRigidbody;
    private Material originalMaterial;
    private Renderer physicsObjRenderer;

    private RaycastHit primaryHit;
    private bool hasValidPhysicsObj;
    private bool hasStableDetection;
    private bool isPunching;

    private const float KMaxDistanceFromHitPoint = 3f;
    private const float KDetectionStabilityTime = 0.1f;

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

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (grabPoint == null && playerCamera != null)
        {
            GameObject grabPointObj = new GameObject("GrabPoint");
            grabPointObj.transform.SetParent(playerCamera.transform);
            grabPointObj.transform.localPosition = Vector3.forward * grabDistance;
            grabPoint = grabPointObj.transform;
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

    // mouse vs key helpers so you don't rewrite input everywhere
    private bool GrabPressedDown()
        => useLeftClickForGrab ? Input.GetMouseButtonDown(0) : Input.GetKeyDown(grabKey);

    private bool GrabReleased()
        => useLeftClickForGrab ? Input.GetMouseButtonUp(0) : Input.GetKeyUp(grabKey);

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

    private void PerformSmartDetection()
    {
        Vector3 detectionOrigin = GetDetectionOrigin();
        Vector3 detectionDirection = orientation.forward;

        bool primaryDetection = Physics.SphereCast(
            detectionOrigin,
            detectionRadius,
            detectionDirection,
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
                PlayDetectionFeedback();
            }
        }
        else
        {
            ResetDetectionState();
        }
    }

    private Vector3 GetDetectionOrigin()
    {
        Vector3 basePosition = orientation != null ? orientation.position : transform.position;
        return basePosition + Vector3.up * detectionHeightOffset;
    }

    private bool ValidateDetectedObject(RaycastHit hit)
    {
        if (Vector3.Distance(transform.position, hit.point) > KMaxDistanceFromHitPoint)
            return false;

        float angle = Vector3.Angle(orientation.forward, -hit.normal);
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
        if (Time.time - lastInteractionTime < objCooldown) return false;
        if (CurrentState == InteractionState.Dragging || CurrentState == InteractionState.Punching) return false;
        return true;
    }

    private void HandleInput()
    {
        if (CurrentState == InteractionState.Detected && hasValidPhysicsObj)
        {
            if (GrabPressedDown())
                TryGrabObject();

            if (Input.GetKeyDown(punchKey))
                TryPunchObject();
        }

        if (CurrentState == InteractionState.Dragging)
        {
            if (Input.GetKeyDown(throwKey))
            {
                ThrowObject();
            }
            else if (GrabReleased())
            {
                // release grab = DROP (not throw)
                ReleaseGrabbedObject();
                CurrentState = InteractionState.Cooldown;
                lastInteractionTime = Time.time;
            }
        }

        if (!playerMovement.Grounded && hasValidPhysicsObj && currentAirborneTime > minAirborneTime)
            TriggerClimbInteraction();
    }

    private void TryGrabObject()
    {
        if (!CanGrabObject()) return;

        CurrentState = InteractionState.Grabbing;
        LastInteractionType = InteractionType.Grab;

        grabbedRigidbody = currentPhysicsObj.GetComponent<Rigidbody>();
        if (grabbedRigidbody != null)
        {
            CreateGrabJoint();
            PlaySound(grabSound);
        }
    }

    private bool CanGrabObject()
    {
        Rigidbody rb = currentPhysicsObj.GetComponent<Rigidbody>();
        return rb != null &&
               rb.mass <= maxGrabMass &&
               Vector3.Distance(transform.position, currentPhysicsObj.position) <= grabDistance;
    }

    private void CreateGrabJoint()
    {
        if (grabbedRigidbody != null)
        {
            grabbedRigidbody.useGravity = false;
            grabbedRigidbody.linearDamping = grabDamping * 5f;
        }
    }

    private void UpdateGrabbing()
    {
        if (CurrentState == InteractionState.Dragging && grabbedRigidbody != null)
        {
            Vector3 targetPosition = grabPoint.position;
            Vector3 direction = targetPosition - grabbedRigidbody.position;
            grabbedRigidbody.linearVelocity = direction * grabForce;

            if (dragLineRenderer != null)
            {
                dragLineRenderer.enabled = true;
                dragLineRenderer.SetPosition(0, grabPoint.position);
                dragLineRenderer.SetPosition(1, grabbedRigidbody.transform.position);
            }
        }
    }

    private void ThrowObject()
    {
        if (grabbedRigidbody != null)
        {
            Vector3 throwDirection = playerCamera.transform.forward;
            grabbedRigidbody.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);
            PlaySound(throwSound);
            LastInteractionType = InteractionType.Throw;
        }

        ReleaseGrabbedObject();
    }

    private void ReleaseGrabbedObject()
    {
        if (grabbedRigidbody != null)
        {
            grabbedRigidbody.useGravity = true;
            grabbedRigidbody.linearDamping = 0f;
        }

        grabbedRigidbody = null;

        if (dragLineRenderer != null)
            dragLineRenderer.enabled = false;
    }

    private void TryPunchObject()
    {
        if (Vector3.Distance(transform.position, currentPhysicsObj.position) > punchRange) return;

        CurrentState = InteractionState.Punching;
        LastInteractionType = InteractionType.Punch;
        isPunching = true;
        punchTimer = punchDuration;

        Rigidbody targetRb = currentPhysicsObj.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            Vector3 punchDirection = (currentPhysicsObj.position - transform.position).normalized;
            targetRb.AddForce(punchDirection * punchForce, ForceMode.Impulse);
        }

        PlaySound(punchSound);
        ShowPunchEffect();
    }

    private void UpdatePunching()
    {
        if (!isPunching) return;

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
        LastInteractionType = InteractionType.Climb;
        Debug.Log($"PhysicsObj: Attempting to climb {currentPhysicsObj.name}");
    }

    private void UpdateHighlight()
    {
        if (physicsObjRenderer != null && originalMaterial != null)
            physicsObjRenderer.material = originalMaterial;

        if (currentPhysicsObj != null)
        {
            physicsObjRenderer = currentPhysicsObj.GetComponent<Renderer>();
            if (physicsObjRenderer != null && highlightMaterial != null)
            {
                originalMaterial = physicsObjRenderer.material;
                physicsObjRenderer.material = highlightMaterial;
            }
        }
    }

    private void UpdateVisualFeedback()
    {
        if (CurrentState != InteractionState.Detected && physicsObjRenderer != null && originalMaterial != null)
        {
            physicsObjRenderer.material = originalMaterial;
            physicsObjRenderer = null;
            originalMaterial = null;
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
    }

    private void UpdateDebugVisualization()
    {
        if (!showDebugRays || orientation == null) return;

        Vector3 origin = GetDetectionOrigin();
        Vector3 direction = orientation.forward * detectionDistance;

        Color rayColor = CurrentState switch
        {
            InteractionState.None => hasValidPhysicsObj ? Color.yellow : Color.red,
            InteractionState.Detected => Color.green,
            InteractionState.Grabbing => Color.blue,
            InteractionState.Dragging => Color.cyan,
            InteractionState.Punching => Color.magenta,
            InteractionState.Cooldown => Color.gray,
            _ => Color.white
        };

        Debug.DrawRay(origin, direction, rayColor);

        if (hasValidPhysicsObj)
        {
            Debug.DrawLine(origin, currentInteractionPoint, Color.magenta);
            Debug.DrawRay(currentInteractionPoint, currentInteractionNormal * 0.5f, Color.blue);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || orientation == null) return;

        Vector3 origin = GetDetectionOrigin();
        Vector3 endPoint = origin + orientation.forward * detectionDistance;

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
    }

    private void OnDestroy() => ReleaseGrabbedObject();
}
