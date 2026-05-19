using UnityEngine;
using UnityEngine.AI;

// RatBrain:
// ├── The rat's tiny evil manager
// ├── Decides whether to patrol, chase, attack, or idle
// ├── Detection does NOT control movement directly
// ├── Chase/Attack/Patrol are separate behaviors
// └── Animation bools are commented out for now to avoid Animator warning spam

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyDetection))]
[RequireComponent(typeof(EnemyChase))]
[RequireComponent(typeof(EnemyAttack))]
[RequireComponent(typeof(EnemyPatrol))]
public class RatBrain : MonoBehaviour
{
    private enum RatState
    {
        Idle,
        Patrol,
        Chase,
        Attack
    }

    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    private EnemyDetection detection;
    private EnemyChase chase;
    private EnemyAttack attack;
    private EnemyPatrol patrol;

    [Header("Debug")]
    [SerializeField] private bool showDebugs = true;

    private RatState currentState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        detection = GetComponent<EnemyDetection>();
        chase = GetComponent<EnemyChase>();
        attack = GetComponent<EnemyAttack>();
        patrol = GetComponent<EnemyPatrol>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        DebugLog("Awake complete.");
    }

    private void Start()
    {
        if (patrol.HasPatrolRoute())
            SetState(RatState.Patrol);
        else
            SetState(RatState.Idle);
    }

    private void Update()
    {
        bool canSeeTarget = detection.CanSeeTarget();
        Transform target = detection.CurrentTarget;

        if (target != null && canSeeTarget && attack.IsTargetInAttackRange(target))
        {
            SetState(RatState.Attack);
        }
        else if (target != null && canSeeTarget)
        {
            SetState(RatState.Chase);
        }
        else if (patrol.HasPatrolRoute())
        {
            SetState(RatState.Patrol);
        }
        else
        {
            SetState(RatState.Idle);
        }

        RunCurrentState(target);

        // Animation updates are disabled for now.
        // UpdateAnimations();
    }

    private void RunCurrentState(Transform target)
    {
        switch (currentState)
        {
            case RatState.Idle:
                agent.isStopped = true;
                break;

            case RatState.Patrol:
                patrol.Patrol();
                break;

            case RatState.Chase:
                chase.ChaseTarget(target);
                break;

            case RatState.Attack:
                attack.AttackTarget(target);
                break;
        }
    }

    private void SetState(RatState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        DebugLog($"State changed to: {currentState}");
    }

    private void UpdateAnimations()
    {
        if (animator == null)
            return;

        bool isMoving = agent.velocity.magnitude > 0.1f && !agent.isStopped;

        // Commented out for now because the Animator does not have these parameters yet.
        // Uncomment these later after adding matching bool parameters to the Animator.
        //
        // animator.SetBool("IsMoving", isMoving);
        // animator.SetBool("IsChasing", currentState == RatState.Chase);
    }

    private void DebugLog(string message)
    {
        if (!showDebugs)
            return;

        Debug.Log($"[RatBrain] [{gameObject.name}] {message}", this);
    }
}