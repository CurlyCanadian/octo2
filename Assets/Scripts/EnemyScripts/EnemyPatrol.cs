using UnityEngine;
using UnityEngine.AI;

// EnemyPatrol:
// ├── Uses a PatrolRoute
// ├── Moves from point to point
// ├── Waits at each point
// └── Does not care about the player

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private PatrolRoute patrolRoute;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTimeAtPoint = 1f;
    [SerializeField] private float pointReachedDistance = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool showDebugs = false;

    private int currentPointIndex;
    private float waitTimer;
    private bool isWaiting;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public bool HasPatrolRoute()
    {
        return patrolRoute != null && patrolRoute.PointCount > 0;
    }

    public void Patrol()
    {
        if (!HasPatrolRoute())
            return;

        Transform currentPoint = patrolRoute.GetPoint(currentPointIndex);

        if (currentPoint == null)
            return;

        agent.speed = patrolSpeed;
        agent.stoppingDistance = pointReachedDistance;

        if (isWaiting)
        {
            WaitAtPoint();
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(currentPoint.position);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + pointReachedDistance)
        {
            StartWaiting();
        }
    }

    private void StartWaiting()
    {
        isWaiting = true;
        waitTimer = 0f;
        agent.isStopped = true;

        if (showDebugs)
            Debug.Log("[EnemyPatrol] Waiting at patrol point.", this);
    }

    private void WaitAtPoint()
    {
        waitTimer += Time.deltaTime;

        if (waitTimer >= waitTimeAtPoint)
        {
            GoToNextPoint();
        }
    }

    private void GoToNextPoint()
    {
        isWaiting = false;
        waitTimer = 0f;

        currentPointIndex = patrolRoute.GetNextIndex(currentPointIndex);

        if (showDebugs)
            Debug.Log($"[EnemyPatrol] Moving to patrol point {currentPointIndex}.", this);
    }
}