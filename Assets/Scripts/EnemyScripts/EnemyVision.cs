using UnityEngine;

[DisallowMultipleComponent]
public class EnemyVision : MonoBehaviour
{
    public enum DetectionState
    {
        CannotSeePlayer,
        SeeingPlayerNormally,
        SeeingCamouflagedPlayer,
        CloseReveal
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private CamouflageController playerCamo;
    [SerializeField] private Transform eyePoint;

    [Header("Vision Settings")]
    [SerializeField] private float normalDetectionRange = 8f;
    [SerializeField] [Range(0f, 360f)] private float fieldOfViewAngle = 120f;
    [SerializeField] private float eyeHeight = 0.8f;
    [SerializeField] private float playerTargetHeightOffset = 0.6f;

    [Header("Line Of Sight")]
    [SerializeField] private bool requireLineOfSight = true;

    [Tooltip("Put walls, ground, props, and obstacles here. Ideally exclude Player and Enemy layers.")]
    [SerializeField] private LayerMask lineOfSightBlockers = ~0;

    [Header("Close Reveal")]
    [Tooltip("If true, enemies can detect camouflaged player up close even outside their field of view.")]
    [SerializeField] private bool closeRevealIgnoresFOV = true;

    [Tooltip("If true, walls can still block close reveal. If false, close reveal acts like smell/touch.")]
    [SerializeField] private bool closeRevealNeedsLineOfSight = false;

    [Header("Debug")]
    public bool CanSeePlayer;
    public DetectionState CurrentDetectionState;
    public Vector3 LastKnownPlayerPosition;

    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool debugDetectionLogs = true;
    [SerializeField] [Range(0.1f, 2f)] private float debugLogInterval = 0.5f;

    private float nextDebugLogTime;
    private DetectionState lastLoggedState;

    private void Awake()
    {
        AutoFindReferences();
    }

    private void Update()
    {
        UpdateVision();
        DebugDetectionState();
    }

    private void AutoFindReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (playerCamo == null && player != null)
            playerCamo = player.GetComponent<CamouflageController>();

        if (playerCamo == null && player != null)
            playerCamo = player.GetComponentInChildren<CamouflageController>();
    }

    private void UpdateVision()
    {
        CanSeePlayer = false;
        CurrentDetectionState = DetectionState.CannotSeePlayer;

        if (player == null)
        {
            AutoFindReferences();
            return;
        }

        Vector3 enemyEyePosition = GetEyePosition();
        Vector3 playerTargetPosition = GetPlayerTargetPosition();

        float distanceToPlayer = Vector3.Distance(enemyEyePosition, playerTargetPosition);

        bool playerIsCamouflaged = playerCamo != null && playerCamo.IsCamouflaged;
        bool closeReveal = playerCamo != null && playerIsCamouflaged && playerCamo.CanEnemyDetectAtCloseRange(transform);

        float effectiveDetectionRange = GetEffectiveDetectionRange(closeReveal);

        if (distanceToPlayer > effectiveDetectionRange && !closeReveal)
        {
            DrawDebugRay(enemyEyePosition, playerTargetPosition, Color.red);
            return;
        }

        if (!closeReveal || !closeRevealIgnoresFOV)
        {
            if (!IsPlayerInsideFOV(enemyEyePosition, playerTargetPosition))
            {
                DrawDebugRay(enemyEyePosition, playerTargetPosition, Color.yellow);
                return;
            }
        }

        if (requireLineOfSight)
        {
            bool shouldCheckLineOfSight = !closeReveal || closeRevealNeedsLineOfSight;

            if (shouldCheckLineOfSight && !HasLineOfSight(enemyEyePosition, playerTargetPosition))
            {
                DrawDebugRay(enemyEyePosition, playerTargetPosition, Color.gray);
                return;
            }
        }

        CanSeePlayer = true;
        LastKnownPlayerPosition = player.position;

        if (closeReveal)
        {
            CurrentDetectionState = DetectionState.CloseReveal;
            DrawDebugRay(enemyEyePosition, playerTargetPosition, Color.magenta);
        }
        else if (playerIsCamouflaged)
        {
            CurrentDetectionState = DetectionState.SeeingCamouflagedPlayer;
            DrawDebugRay(enemyEyePosition, playerTargetPosition, Color.cyan);
        }
        else
        {
            CurrentDetectionState = DetectionState.SeeingPlayerNormally;
            DrawDebugRay(enemyEyePosition, playerTargetPosition, Color.green);
        }
    }

    private float GetEffectiveDetectionRange(bool closeReveal)
    {
        if (playerCamo == null)
            return normalDetectionRange;

        if (!playerCamo.IsCamouflaged)
            return normalDetectionRange;

        if (closeReveal)
            return Mathf.Max(normalDetectionRange, playerCamo.CloseRevealDistance);

        return normalDetectionRange * playerCamo.GetDetectionMultiplierForEnemy(transform);
    }

    private Vector3 GetEyePosition()
    {
        if (eyePoint != null)
            return eyePoint.position;

        return transform.position + Vector3.up * eyeHeight;
    }

    private Vector3 GetPlayerTargetPosition()
    {
        return player.position + Vector3.up * playerTargetHeightOffset;
    }

    private bool IsPlayerInsideFOV(Vector3 enemyEyePosition, Vector3 playerTargetPosition)
    {
        Vector3 directionToPlayer = playerTargetPosition - enemyEyePosition;
        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude < 0.001f)
            return true;

        Vector3 enemyForward = transform.forward;
        enemyForward.y = 0f;

        if (enemyForward.sqrMagnitude < 0.001f)
            enemyForward = transform.forward;

        float angleToPlayer = Vector3.Angle(enemyForward.normalized, directionToPlayer.normalized);

        return angleToPlayer <= fieldOfViewAngle * 0.5f;
    }

    private bool HasLineOfSight(Vector3 enemyEyePosition, Vector3 playerTargetPosition)
    {
        Vector3 directionToPlayer = playerTargetPosition - enemyEyePosition;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (Physics.Raycast(
            enemyEyePosition,
            directionToPlayer.normalized,
            out RaycastHit hit,
            distanceToPlayer,
            lineOfSightBlockers,
            QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == player || hit.transform.IsChildOf(player))
                return true;

            return false;
        }

        return true;
    }

    private void DebugDetectionState()
    {
        if (!debugDetectionLogs)
            return;

        if (Time.time < nextDebugLogTime && CurrentDetectionState == lastLoggedState)
            return;

        nextDebugLogTime = Time.time + debugLogInterval;
        lastLoggedState = CurrentDetectionState;

        if (player == null)
        {
            Debug.LogWarning($"{name} EnemyVision: No player assigned/found.");
            return;
        }

        string camoInfo = playerCamo != null
            ? $"Player Camo: {playerCamo.IsCamouflaged}, Camo Multiplier: {playerCamo.CamoDetectionMultiplier}, Close Reveal: {playerCamo.CloseRevealDistance}"
            : "No CamouflageController found.";

        Debug.Log($"{name} EnemyVision: {CurrentDetectionState} | Can See Player: {CanSeePlayer} | {camoInfo}");
    }

    private void DrawDebugRay(Vector3 start, Vector3 end, Color color)
    {
        if (!showDebugRays)
            return;

        Debug.DrawLine(start, end, color);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos)
            return;

        Vector3 eyePosition = Application.isPlaying ? GetEyePosition() : transform.position + Vector3.up * eyeHeight;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(eyePosition, normalDetectionRange);

        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = transform.forward;

        Quaternion leftRayRotation = Quaternion.AngleAxis(-fieldOfViewAngle * 0.5f, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(fieldOfViewAngle * 0.5f, Vector3.up);

        Vector3 leftRayDirection = leftRayRotation * forward.normalized;
        Vector3 rightRayDirection = rightRayRotation * forward.normalized;

        Gizmos.DrawRay(eyePosition, leftRayDirection * normalDetectionRange);
        Gizmos.DrawRay(eyePosition, rightRayDirection * normalDetectionRange);

        if (playerCamo != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, playerCamo.CloseRevealDistance);
        }
    }
}