using UnityEngine;
using UnityEngine.Events;

//
// InteractionSelector.cs  (aka: the NON-physics interaction gremlin)
//
// This script is basically the "selector brain":
// - It looks at stuff
// - Figures out what you're aiming at
// - Lets you press E to do *non-physics* interactions
//
// PhysicsObj.cs stays CLEAN and ONLY handles:
//   grab / drag / punch / throw / climb triggers
//
// InteractionSelector.cs handles:
//   buttons, doors, animation triggers, NPC talk, UI, etc.
//   (things that don't need Rigidbody yeeting)
//
// IMPORTANT LAYERS!!!!!!!!!!!!
// Make a layer for NON-physics interactables
// Example:
//   -Interactable-  Interactable   (doors/buttons/etc.)
//   -Interactable-  PhysicsObj     (rigidbody stuff)
//
// Then:
//   PhysicsObj.cs    uses PhysicsObj layer
//   Selector.cs      uses Interactable layer
//
// Default Controls:
// E - Interact with NON-physics interactables
//
// Extra spicy option:
// If disablePhysicsWhileHovering = true
// then when you look at a button/door etc,
// PhysicsObj.cs turns OFF so it doesn't steal your E key.
//
// verifications in editor or else sadness:
// - playerCamera assigned (auto Main Camera if missing)
// - interactableLayer set to your NON-physics layer
// - PhysicsObj script reference optionally assigned
// - objects have colliders + right layer
// - objects have IInteractable script
//

public interface IInteractable
{
    string GetPrompt();
    bool CanInteract(Interaction selector);
    void Interact(Interaction selector);
}

public class Interaction : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PhysicsObj physicsObjInteractor;

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
    private Highlightable currentHighlightable;
    private RaycastHit hit;

    private float stabilityTimer;
    private bool hasStableTarget;

    private const float KStabilityTime = 0.05f;

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

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

                currentHighlightable = currentTransform.GetComponent<Highlightable>();
                if (currentHighlightable != null)
                    currentHighlightable.SetHighlight(true);

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

        physicsObjInteractor.enabled = !(hasStableTarget && currentInteractable != null);
    }

    private void ClearCurrent()
    {
        if (currentHighlightable != null)
        {
            currentHighlightable.SetHighlight(false);
            currentHighlightable = null;
        }

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
