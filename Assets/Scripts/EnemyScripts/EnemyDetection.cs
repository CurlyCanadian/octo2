using UnityEngine;

// EnemyDetection:
// ├── Finds the player
// ├── Checks detection range
// ├── Checks field of view
// ├── Checks if walls are blocking sight
// └── Does NOT chase or attack by itself

public class EnemyDetection : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool autoFindTarget = true;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float fieldOfViewAngle = 120f;
    [SerializeField] private bool useFieldOfView = true;
    [SerializeField] private bool requireLineOfSight = true;

    [Header("Layers")]
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private LayerMask obstructionLayer;

    [Header("Eye Settings")]
    [SerializeField] private float eyeHeight = 0.4f;
    [SerializeField] private float targetHeightOffset = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugs = false;
    [SerializeField] private bool showGizmos = true;

    public Transform CurrentTarget { get; private set; }

    private void Start()
    {
        TryFindTarget();
    }

    public bool CanSeeTarget()
    {
        CurrentTarget = null;

        if (target == null)
            TryFindTarget();

        if (target == null)
            return false;

        if (!TargetIsOnCorrectLayer())
            return false;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget > detectionRange)
            return false;

        Vector3 directionToTarget = (target.position - transform.position).normalized;

        if (useFieldOfView)
        {
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

            if (angleToTarget > fieldOfViewAngle / 2f)
                return false;
        }

        if (requireLineOfSight && IsSightBlocked())
        {
            if (showDebugs)
                Debug.Log("[EnemyDetection] Target is blocked by an obstruction.", this);

            return false;
        }

        CurrentTarget = target;

        if (showDebugs)
            Debug.Log("[EnemyDetection] Target detected!", this);

        return true;
    }

    private void TryFindTarget()
    {
        if (!autoFindTarget)
            return;

        GameObject foundTarget = GameObject.FindGameObjectWithTag(targetTag);

        if (foundTarget != null)
            target = foundTarget.transform;
    }

    private bool TargetIsOnCorrectLayer()
    {
        if (targetLayer.value == 0)
            return true;

        return (targetLayer.value & (1 << target.gameObject.layer)) != 0;
    }

    private bool IsSightBlocked()
    {
        Vector3 rayStart = transform.position + Vector3.up * eyeHeight;
        Vector3 rayEnd = target.position + Vector3.up * targetHeightOffset;
        Vector3 rayDirection = rayEnd - rayStart;

        return Physics.Raycast(
            rayStart,
            rayDirection.normalized,
            rayDirection.magnitude,
            obstructionLayer
        );
    }

    private Vector3 DirectionFromAngle(float angleInDegrees)
    {
        angleInDegrees += transform.eulerAngles.y;

        return new Vector3(
            Mathf.Sin(angleInDegrees * Mathf.Deg2Rad),
            0f,
            Mathf.Cos(angleInDegrees * Mathf.Deg2Rad)
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Vector3 leftViewAngle = DirectionFromAngle(-fieldOfViewAngle / 2f);
        Vector3 rightViewAngle = DirectionFromAngle(fieldOfViewAngle / 2f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + leftViewAngle * detectionRange);
        Gizmos.DrawLine(transform.position, transform.position + rightViewAngle * detectionRange);

        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position + Vector3.up * eyeHeight, target.position + Vector3.up * targetHeightOffset);
        }
    }
}