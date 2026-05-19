using UnityEngine;
using UnityEngine.AI;

// EnemyChase:
// ├── Takes a target
// ├── Tells the NavMeshAgent to chase it
// └── Does not decide when chasing should happen

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float stoppingDistance = 1.3f;

    [Header("Debug")]
    [SerializeField] private bool showDebugs = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void ChaseTarget(Transform target)
    {
        if (target == null)
            return;

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = stoppingDistance;

        agent.SetDestination(target.position);

        if (showDebugs)
            Debug.Log("[EnemyChase] Chasing target.", this);
    }
}