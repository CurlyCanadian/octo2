using UnityEngine;

/// <summary>
/// Detects interactable objects (like cubes) and triggers climb if player is airborne.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Interactable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Climbing climbing;
    [SerializeField] private Rigidbody rb;

    [Header("Interactable Settings")]
    [SerializeField] [Tooltip("Layer mask for interactable objects (e.g., cubes, crates)")]
    private LayerMask interactableLayer;
    [SerializeField] [Range(0.5f, 2f)] [Tooltip("Detection distance for interactables")]
    private float detectionDistance = 1.2f;
    [SerializeField] [Range(0.1f, 1f)] [Tooltip("Radius for interaction detection")]
    private float detectionRadius = 0.4f;

    private RaycastHit interactHit;
    private bool interactableInFront;

    private void Update()
    {
        DetectInteractable();

        // If airborne and touching an interactable, start climbing
        if (!playerMovement.Grounded && interactableInFront)
        {
            climbing.StartManualClimbFromGround(interactHit);
        }
    }

    /// <summary>
    /// Detects interactable objects (like cubes) in front of the player using a spherecast.
    /// </summary>
    private void DetectInteractable()
    {
        interactableInFront = Physics.SphereCast(
            transform.position,
            detectionRadius,
            orientation.forward,
            out interactHit,
            detectionDistance,
            interactableLayer);

        // Debug line
        Color rayColor = interactableInFront ? Color.green : Color.red;
        Debug.DrawRay(transform.position, orientation.forward * detectionDistance, rayColor);
    }
}
