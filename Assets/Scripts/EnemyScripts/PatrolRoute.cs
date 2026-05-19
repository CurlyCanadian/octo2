using UnityEngine;

// PatrolRoute:
// ├── Holds patrol points
// ├── Lets multiple enemies use the same route
// └── Keeps the rat inspector cleaner

public class PatrolRoute : MonoBehaviour
{
    [Header("Patrol Points")]
    [SerializeField] private Transform[] patrolPoints;

    [Header("Route Settings")]
    [SerializeField] private bool loopRoute = true;

    [Header("Gizmos")]
    [SerializeField] private bool showGizmos = true;

    public int PointCount => patrolPoints == null ? 0 : patrolPoints.Length;

    public Transform GetPoint(int index)
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return null;

        index = Mathf.Clamp(index, 0, patrolPoints.Length - 1);

        return patrolPoints[index];
    }

    public int GetNextIndex(int currentIndex)
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return 0;

        int nextIndex = currentIndex + 1;

        if (nextIndex >= patrolPoints.Length)
        {
            if (loopRoute)
                nextIndex = 0;
            else
                nextIndex = patrolPoints.Length - 1;
        }

        return nextIndex;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || patrolPoints == null)
            return;

        Gizmos.color = Color.blue;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null)
                continue;

            Gizmos.DrawSphere(patrolPoints[i].position, 0.2f);

            int nextIndex = i + 1;

            if (nextIndex >= patrolPoints.Length)
            {
                if (!loopRoute)
                    continue;

                nextIndex = 0;
            }

            if (patrolPoints[nextIndex] != null)
            {
                Gizmos.DrawLine(
                    patrolPoints[i].position,
                    patrolPoints[nextIndex].position
                );
            }
        }
    }
}