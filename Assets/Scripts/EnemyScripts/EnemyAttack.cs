using UnityEngine;
using UnityEngine.AI;

// EnemyAttack:
// ├── Checks attack range
// ├── Stops the enemy
// ├── Rotates toward target
// ├── Triggers attack animation
// └── Does NOT directly damage the player
//
// Damage should happen through DamageHitbox animation events.

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.3f;
    [SerializeField] private float attackCooldown = 1.25f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Distance")]
    [SerializeField] private bool useFlatDistance = true;

    [Header("Animation")]
    [SerializeField] private string attackTriggerName = "Attack";

    [Header("Debug")]
    [SerializeField] private bool showDebugs = true;

    private float lastAttackTime = -999f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        DebugLog("Awake complete.");
    }

    public bool IsTargetInAttackRange(Transform target)
    {
        if (target == null)
            return false;

        float distance = GetDistanceToTarget(target);
        bool inRange = distance <= attackRange;

        if (showDebugs)
        {
            Debug.Log($"[EnemyAttack] [{gameObject.name}] Target distance: {distance:F2}. In attack range: {inRange}", this);
        }

        return inRange;
    }

    public void AttackTarget(Transform target)
    {
        if (target == null)
        {
            DebugLog("AttackTarget called but target was null.");
            return;
        }

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        RotateTowardTarget(target);

        if (!IsTargetInAttackRange(target))
        {
            DebugLog("Target is not in range. Attack canceled.");
            return;
        }

        if (Time.time < lastAttackTime + attackCooldown)
        {
            DebugLog("Attack is on cooldown.");
            return;
        }

        TriggerAttackAnimation();
        lastAttackTime = Time.time;
    }

    private void TriggerAttackAnimation()
    {
        DebugLog("Attack animation triggered. Actual damage should come from DamageHitbox.");

        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
        {
            animator.SetTrigger(attackTriggerName);
        }
        else
        {
            DebugLog("No animator found or attack trigger name is empty.");
        }
    }

    private void RotateTowardTarget(Transform target)
    {
        Vector3 lookDirection = target.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection == Vector3.zero)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    private float GetDistanceToTarget(Transform target)
    {
        if (!useFlatDistance)
            return Vector3.Distance(transform.position, target.position);

        Vector3 enemyPosition = transform.position;
        Vector3 targetPosition = target.position;

        enemyPosition.y = 0f;
        targetPosition.y = 0f;

        return Vector3.Distance(enemyPosition, targetPosition);
    }

    private void DebugLog(string message)
    {
        if (!showDebugs)
            return;

        Debug.Log($"[EnemyAttack] [{gameObject.name}] {message}", this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}