using UnityEngine;

// PlayerMovement:
// ├── Orientation: [Player's orientation transform] :D
// ├── Ground Layer: "Ground"

// Climbing:
// ├── Orientation: [Player's orientation transform]
// ├── Rigid Body: [Players Rigidbody]
// ├── Player Movement: [PlayerMovement script]
// ├── Wall Layer: "Wall" :D

// Interactable:
// ├── Orientation: [Player's orientation transform]
// ├── Player Movement: [PlayerMovement script]
// ├── Climbing: [Climbing script]
// ├── Player Camera: [Auto assigns to Main Camera]
// ├── Interactable Layer: "Interactable"


// Layers
// Edit > Project Settings > Tags and Layers :D
// Create: Interactable, Wall and Ground layers
// Layers^^^^^^^^^

// errrrmmm if you cant grab objects then 
// Ensure objects have Rigidbody
// Check object mass < 2kg (default limit) google told me so :D


// make me this you foolish mortal peasent
// Test Objects that I need make me them ^-^
// Create a Cube
// Add Rigidbody (Mass: 1.0)
// Set Layer to "Interactable" 











// important!!!!!!!!!!!!!!!!!!!!!!! you must do this!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
//  Things to verify in Unity Editor:

// Layer Setup - Make sure you've created the (Interactable) layer in Unity (Edit > Project Settings > Tags and Layers)
// References - Assign the required references in the Inspector:
// Orientation (player's orientation transform)
// PlayerMovement script
// Climbing script
// Main Camera (auto assigns if not set)
// Test Objects - Objects you want to interact with need:
// Rigidbody component
// Layer set to (Interactable"q
// Mass ≤ 2kg (or adjust maxGrabMass in Inspector)
// 🎮 Default Controls:

// E - Grab Release objects
// F - Punch objects
// Q - Throw grabbed objects


[RequireComponent(typeof(Rigidbody))]
public class Interactable : MonoBehaviour
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
    [SerializeField] private LayerMask interactableLayer = -1;
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
    [SerializeField] private KeyCode grabKey = KeyCode.E;
    [SerializeField] private KeyCode punchKey = KeyCode.F;
    [SerializeField] private KeyCode throwKey = KeyCode.Q;

    [Header("Physics Settings")]
    [SerializeField] [Range(0f, 90f)] private float maxInteractionAngle = 60f;
    [SerializeField] [Range(0.1f, 2f)] private float interactionCooldown = 0.3f;
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
    private Transform currentInteractable;
    private Vector3 currentInteractionPoint;
    private Vector3 currentInteractionNormal;
    private Rigidbody grabbedRigidbody;
    private Material originalMaterial;
    private Renderer interactableRenderer;
    
    private RaycastHit primaryHit;
    private bool hasValidInteractable;
    private bool hasStableDetection;
    private bool isPunching;
    
    private const float KMinVelocityForInteraction = 0.5f;
    private const float KMaxDistanceFromHitPoint = 3f;
    private const float KDetectionStabilityTime = 0.1f;
    private const float KGrabSmoothTime = 0.1f;

    private void Start()
    {
        if (!ValidateReferences()) {
            enabled = false;
            return;
        }

        ResetInteractionState();
        lastInteractionTime = -interactionCooldown;
        
        if (dragLineRenderer != null) {
            dragLineRenderer.enabled = false;
        }
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (orientation == null) {
            Debug.LogError("Interactable: Orientation reference is null!");
            isValid = false;
        }

        if (playerMovement == null) {
            Debug.LogError("Interactable: PlayerMovement reference is null!");
            isValid = false;
        }

        if (climbing == null) {
            Debug.LogError("Interactable: Climbing reference is null!");
            isValid = false;
        }

        if (playerCamera == null) {
            playerCamera = Camera.main;
        }

        if (grabPoint == null) {
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

    private void UpdateAirborneTracking()
    {
        if (playerMovement.Grounded) {
            if (!wasGroundedLastFrame) {
                if (CurrentState == InteractionState.Dragging) {
                    ReleaseGrabbedObject();
                }
            }
            currentAirborneTime = 0f;
        } else {
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
            interactableLayer,
            QueryTriggerInteraction.Ignore
        );

        if (primaryDetection && ValidateDetectedObject(primaryHit)) {
            if (IsSameInteractable(primaryHit.transform)) {
                detectionStabilityTimer += Time.deltaTime;
                hasStableDetection = detectionStabilityTimer >= KDetectionStabilityTime;
            } else {
                currentInteractable = primaryHit.transform;
                detectionStabilityTimer = 0f;
                hasStableDetection = false;
                UpdateHighlight();
            }

            if (hasStableDetection) {
                currentInteractionPoint = primaryHit.point;
                currentInteractionNormal = primaryHit.normal;
                hasValidInteractable = true;
                PlayDetectionFeedback();
            }
        } else {
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
        if (Vector3.Distance(transform.position, hit.point) > KMaxDistanceFromHitPoint) {
            return false;
        }

        float angle = Vector3.Angle(orientation.forward, -hit.normal);
        if (angle > maxInteractionAngle) {
            return false;
        }

        if (hit.collider != null) {
            float objectSize = hit.collider.bounds.size.magnitude;
            if (objectSize < minimumObjectSize) {
                return false;
            }
        }

        return true;
    }

    private bool IsSameInteractable(Transform detectedTransform)
    {
        return currentInteractable == detectedTransform;
    }

    private void UpdateInteractionState()
    {
        switch (CurrentState) {
            case InteractionState.None:
                if (hasValidInteractable && CanStartInteraction()) {
                    CurrentState = InteractionState.Detected;
                }
                break;

            case InteractionState.Detected:
                if (!hasValidInteractable) {
                    CurrentState = InteractionState.None;
                }
                break;

            case InteractionState.Grabbing:
                if (grabbedRigidbody != null) {
                    CurrentState = InteractionState.Dragging;
                } else {
                    CurrentState = InteractionState.Cooldown;
                    lastInteractionTime = Time.time;
                }
                break;

            case InteractionState.Dragging:
                if (grabbedRigidbody == null) {
                    CurrentState = InteractionState.Cooldown;
                    lastInteractionTime = Time.time;
                }
                break;

            case InteractionState.Punching:
                if (!isPunching) {
                    CurrentState = InteractionState.Cooldown;
                    lastInteractionTime = Time.time;
                }
                break;

            case InteractionState.Cooldown:
                if (Time.time - lastInteractionTime >= interactionCooldown) {
                    CurrentState = InteractionState.None;
                }
                break;
        }
    }

    private bool CanStartInteraction()
    {
        if (Time.time - lastInteractionTime < interactionCooldown) {
            return false;
        }

        if (CurrentState == InteractionState.Dragging || CurrentState == InteractionState.Punching) {
            return false;
        }

        return true;
    }

    private void HandleInput()
    {
        if (CurrentState == InteractionState.Detected && hasValidInteractable) {
            if (Input.GetKeyDown(grabKey)) {
                TryGrabObject();
            }
            
            if (Input.GetKeyDown(punchKey)) {
                TryPunchObject();
            }
        }

        if (CurrentState == InteractionState.Dragging) {
            if (Input.GetKeyDown(throwKey) || Input.GetKeyUp(grabKey)) {
                ThrowObject();
            }
        }

        if (!playerMovement.Grounded && hasValidInteractable && currentAirborneTime > minAirborneTime) {
            TriggerClimbInteraction();
        }
    }

    private void TryGrabObject()
    {
        if (!CanGrabObject()) return;

        CurrentState = InteractionState.Grabbing;
        LastInteractionType = InteractionType.Grab;
        
        grabbedRigidbody = currentInteractable.GetComponent<Rigidbody>();
        if (grabbedRigidbody != null) {
            CreateGrabJoint();
            PlaySound(grabSound);
        }
    }

    private bool CanGrabObject()
    {
        Rigidbody rb = currentInteractable.GetComponent<Rigidbody>();
        return rb != null && rb.mass <= maxGrabMass && Vector3.Distance(transform.position, currentInteractable.position) <= grabDistance;
    }

    private void CreateGrabJoint()
    {
        // Simple physics-based grab without joints
        if (grabbedRigidbody != null) {
            grabbedRigidbody.useGravity = false;
            grabbedRigidbody.drag = grabDamping * 5f;
        }
    }

    private void UpdateGrabbing()
    {
        if (CurrentState == InteractionState.Dragging && grabbedRigidbody != null) {
            // Move object toward grab point using physics forces
            Vector3 targetPosition = grabPoint.position;
            Vector3 direction = targetPosition - grabbedRigidbody.position;
            
            // Apply force to move object toward grab point
            grabbedRigidbody.velocity = direction * grabForce;

            if (dragLineRenderer != null) {
                dragLineRenderer.enabled = true;
                dragLineRenderer.SetPosition(0, grabPoint.position);
                dragLineRenderer.SetPosition(1, grabbedRigidbody.transform.position);
            }
        }
    }

    private void ThrowObject()
    {
        if (grabbedRigidbody != null) {
            Vector3 throwDirection = playerCamera.transform.forward;
            grabbedRigidbody.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);
            PlaySound(throwSound);
            LastInteractionType = InteractionType.Throw;
        }
        
        ReleaseGrabbedObject();
    }

    private void ReleaseGrabbedObject()
    {
        // Re-enable gravity and reset drag when releasing
        if (grabbedRigidbody != null) {
            grabbedRigidbody.useGravity = true;
            grabbedRigidbody.drag = 0f;
        }
        
        grabbedRigidbody = null;
        
        if (dragLineRenderer != null) {
            dragLineRenderer.enabled = false;
        }
    }

    private void TryPunchObject()
    {
        if (Vector3.Distance(transform.position, currentInteractable.position) > punchRange) return;

        CurrentState = InteractionState.Punching;
        LastInteractionType = InteractionType.Punch;
        isPunching = true;
        punchTimer = punchDuration;

        Rigidbody targetRb = currentInteractable.GetComponent<Rigidbody>();
        if (targetRb != null) {
            Vector3 punchDirection = (currentInteractable.position - transform.position).normalized;
            targetRb.AddForce(punchDirection * punchForce, ForceMode.Impulse);
        }

        PlaySound(punchSound);
        ShowPunchEffect();
    }

    private void UpdatePunching()
    {
        if (isPunching) {
            punchTimer -= Time.deltaTime;
            if (punchTimer <= 0) {
                isPunching = false;
            }
        }
    }

    private void ShowPunchEffect()
    {
        if (punchEffect != null) {
            GameObject effect = Instantiate(punchEffect, currentInteractionPoint, Quaternion.LookRotation(currentInteractionNormal));
            Destroy(effect, 1f);
        }
    }

    private void TriggerClimbInteraction()
    {
        LastInteractionType = InteractionType.Climb;
        Debug.Log($"Attempting to climb {currentInteractable.name}");
    }

    private void UpdateHighlight()
    {
        if (interactableRenderer != null && originalMaterial != null) {
            interactableRenderer.material = originalMaterial;
        }

        if (currentInteractable != null) {
            interactableRenderer = currentInteractable.GetComponent<Renderer>();
            if (interactableRenderer != null && highlightMaterial != null) {
                originalMaterial = interactableRenderer.material;
                interactableRenderer.material = highlightMaterial;
            }
        }
    }

    private void UpdateVisualFeedback()
    {
        if (CurrentState != InteractionState.Detected && interactableRenderer != null && originalMaterial != null) {
            interactableRenderer.material = originalMaterial;
            interactableRenderer = null;
            originalMaterial = null;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) {
            audioSource.PlayOneShot(clip);
        }
    }

    private void PlayDetectionFeedback()
    {
        if (CurrentState == InteractionState.None) {
            PlaySound(detectionSound);
        }
    }

    private void ResetInteractionState()
    {
        CurrentState = InteractionState.None;
        ResetDetectionState();
        ReleaseGrabbedObject();
    }

    private void ResetDetectionState()
    {
        hasValidInteractable = false;
        hasStableDetection = false;
        detectionStabilityTimer = 0f;
        currentInteractable = null;
        currentInteractionPoint = Vector3.zero;
        currentInteractionNormal = Vector3.up;
    }

    private void UpdateDebugVisualization()
    {
        if (!showDebugRays) return;

        Vector3 origin = GetDetectionOrigin();
        Vector3 direction = orientation.forward * detectionDistance;
        
        Color rayColor = CurrentState switch {
            InteractionState.None => hasValidInteractable ? Color.yellow : Color.red,
            InteractionState.Detected => Color.green,
            InteractionState.Grabbing => Color.blue,
            InteractionState.Dragging => Color.cyan,
            InteractionState.Punching => Color.magenta,
            InteractionState.Cooldown => Color.gray,
            _ => Color.white
        };

        Debug.DrawRay(origin, direction, rayColor);
        
        if (hasValidInteractable) {
            Debug.DrawLine(origin, currentInteractionPoint, Color.magenta);
            Debug.DrawRay(currentInteractionPoint, currentInteractionNormal * 0.5f, Color.blue);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Vector3 origin = GetDetectionOrigin();
        Vector3 endPoint = origin + orientation.forward * detectionDistance;

        Gizmos.color = hasValidInteractable ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin, detectionRadius);
        Gizmos.DrawWireSphere(endPoint, detectionRadius);
        Gizmos.DrawLine(origin, endPoint);

        if (hasValidInteractable) {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(currentInteractionPoint, 0.1f);
            Gizmos.DrawRay(currentInteractionPoint, currentInteractionNormal * 0.5f);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grabDistance);
        Gizmos.DrawWireSphere(transform.position, punchRange);

        Gizmos.color = CurrentState switch {
            InteractionState.None => Color.gray,
            InteractionState.Detected => Color.yellow,
            InteractionState.Grabbing => Color.blue,
            InteractionState.Dragging => Color.cyan,
            InteractionState.Punching => Color.red,
            InteractionState.Cooldown => Color.white,
            _ => Color.black
        };
        
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.2f);
    }

    private void OnDestroy()
    {
        ReleaseGrabbedObject();
    }
}