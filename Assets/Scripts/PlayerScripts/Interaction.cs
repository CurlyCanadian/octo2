using UnityEngine;
using UnityEngine.Events;


// Interaction.cs  (aka: the NON-physics interaction gremlin)

// This script is basically the "selector brain":
// - It looks at stuff Yeah TufF!
// - Figures out what you're aiming at
// - Lets you press E to do *non-physics* interactions
//
// PhysicsObj.cs stays CLEAN and ONLY handles:
//   grab / drag / punch / throw / climb triggers

// InteractionSelector.cs handles:
//   buttons, doors, animation triggers, NPC talk, UI, etc.
//   (things that don't need Rigidbody yeeting)

// They work together like:
//   Selector: "hey you're looking at a door"
//   Door: "press E to open me"
//   PhysicsObj: "cool, not my job, I'll nap"

// IMPORTANT LAYERS!!!!!!!!!!!!
// Make a layer for NON-physics interactables
// Example:
//   -Interactable-  Interactable   (doors/buttons/etc.)
//   -Interactable-  PhysicsObj     (rigidbody stuff)

// Then:
//   PhysicsObj.cs    uses PhysicsObj layer
//   Selector.cs      uses Interactable layer

// if you break it:
// 1. check the layer masks
// 2. check colliders exist
// 3. check your interactable has a script that implements IInteractable
// 4. accept that you've angered the unity gods

// Test Objects you should make right NOW you foolish mortal peasant:
// 1) Door / Button / Lever prefab
//    - Add Collider
//    - Set Layer to "Interactable"
//    - Add a script implementing IInteractable (example later)

// Default Controls:
// E - Interact with NON-physics interactables

// Extra spicy option:
// If disablePhysicsWhileHovering = true
// then when you look at a button/door etc,
// PhysicsObj.cs turns OFF so it doesn't steal your E key.
// (no more grabbing a cube when you wanted to open a door)

// verifications in editor or else sadness:
// - playerCamera assigned (auto Main Camera if missing)
// - interactableLayer set to your NON-physics layer
// - PhysicsObj script reference optionally assigned
// - objects have colliders + right layer


/// <summary>
/// Anything you want THIS selector to talk to must implement IInteractable.
/// (not physics objects — those go to PhysicsObj.cs)
/// </summary>
public interface IInteractable
{
    // optional UI prompt like "Press E to Open"
    string GetPrompt();

    // return false if locked/cooldown/etc.
    bool CanInteract(Interaction selector);

    // do the thing (open door, play anim, push button, etc.)
    void Interact(Interaction selector);
}

public class Interaction : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PhysicsObj physicsObjInteractor; 
    // ties into Script A so we can politely tell it to shut up when needed

    [Header("Detection Settings (Non-Physics)")]
    [SerializeField] private LayerMask interactableLayer = -1; 
    [SerializeField] [Range(0.5f, 6f)] private float detectionDistance = 3f;
    [SerializeField] [Range(0.05f, 0.8f)] private float detectionRadius = 0.25f;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Events (for UI/feedback)")]
    public UnityEvent<GameObject> OnHoverEnter;
    public UnityEvent<GameObject> OnHoverExit;
    public UnityEvent<GameObject> OnInteract;

    [Header("Behavior")]
    [Tooltip("If true, PhysicsObj.cs is disabled while hovering a non-physics interactable.")]
    [SerializeField] private bool disablePhysicsWhileHovering = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;

    private IInteractable currentInteractable;
    private Transform currentTransform;
    private RaycastHit hit;

    private float stabilityTimer;
    private bool hasStableTarget;

    private const float KStabilityTime = 0.05f;

    private void Start()
    {
        // if you forgot the camera I will save you (begrudgingly)
        if (playerCamera == null)
            playerCamera = Camera.main;

        // if you forgot to plug PhysicsObj, also fine
        if (physicsObjInteractor == null)
            physicsObjInteractor = GetComponent<PhysicsObj>();
    }

    private void Update()
    {
        DetectInteractable();
        HandleInput();
        UpdatePhysicsScriptEnable();
        DrawDebug();
    }

    private void DetectInteractable()
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 dir = playerCamera.transform.forward;

        bool found = Physics.SphereCast(
            origin,
            detectionRadius,
            dir,
            out hit,
            detectionDistance,
            interactableLayer,
            QueryTriggerInteraction.Collide
        );

        if (!found)
        {
            ClearCurrent();
            return;
        }

        // new target? reset the brain
        if (hit.transform != currentTransform)
        {
            ClearCurrent();

            currentTransform = hit.transform;
            currentInteractable = currentTransform.GetComponent<IInteractable>();

            stabilityTimer = 0f;
            hasStableTarget = false;
        }

        if (currentInteractable != null)
        {
            stabilityTimer += Time.deltaTime;

            if (!hasStableTarget && stabilityTimer >= KStabilityTime)
            {
                hasStableTarget = true;
                OnHoverEnter?.Invoke(currentTransform.gameObject);
            }
        }
    }

    private void HandleInput()
    {
        if (!hasStableTarget || currentInteractable == null) return;

        if (Input.GetKeyDown(interactKey) && currentInteractable.CanInteract(this))
        {
            currentInteractable.Interact(this);
            OnInteract?.Invoke(currentTransform.gameObject);
        }
    }

    private void UpdatePhysicsScriptEnable()
    {
        if (!disablePhysicsWhileHovering || physicsObjInteractor == null) return;

        // if we are hovering a NON-physics interactable
        // we disable PhysicsObj.cs so it doesn't hog the E key.
        physicsObjInteractor.enabled = !(hasStableTarget && currentInteractable != null);
    }

    private void ClearCurrent()
    {
        if (currentTransform != null && hasStableTarget)
            OnHoverExit?.Invoke(currentTransform.gameObject);

        currentTransform = null;
        currentInteractable = null;
        stabilityTimer = 0f;
        hasStableTarget = false;
    }

    public bool HasTarget => hasStableTarget && currentInteractable != null;
    public IInteractable Current => currentInteractable;

    private void DrawDebug()
    {
        if (!showDebugRay || playerCamera == null) return;

        Debug.DrawRay(
            playerCamera.transform.position,
            playerCamera.transform.forward * detectionDistance,
            HasTarget ? Color.green : Color.red
        );
    }
}
