using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// EnemyDeathHandler:
// ├── Enemy-only death script
// ├── Plays death animation
// ├── Disables AI scripts
// ├── Drops item
// ├── Fades out
// ├── Destroys enemy
// └── Has safety check so it does NOT delete the player

[RequireComponent(typeof(Health))]
public class EnemyDeath : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;

    [Header("Scripts To Disable On Death")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    [Header("Colliders")]
    [SerializeField] private Collider[] collidersToDisable;

    [Header("Drops")]
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private Transform dropPoint;

    [Header("Death Settings")]
    [SerializeField] private float destroyDelay = 4f;
    [SerializeField] private bool fadeBeforeDestroy = true;
    [SerializeField] private float fadeDuration = 1.25f;

    [Header("Animation Parameters")]
    [SerializeField] private string deathTriggerName = "Die";

    [Header("Safety")]
    [SerializeField] private bool preventDestroyIfTaggedPlayer = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugs = true;

    private bool isDead;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        DebugLog($"Awake complete. Tag: {gameObject.tag}. Destroy delay: {destroyDelay}");
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDied.AddListener(HandleDeath);
            DebugLog("Subscribed to Health.OnDied.");
        }
        else
        {
            DebugLogError("No Health component found.");
        }
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDied.RemoveListener(HandleDeath);
    }

    private void HandleDeath()
    {
        DebugLogError("HandleDeath was called.");

        if (preventDestroyIfTaggedPlayer && CompareTag("Player"))
        {
            DebugLogError("THIS SCRIPT IS ON THE PLAYER. REMOVE EnemyDeathHandler FROM THE PLAYER. Destroy canceled.");
            return;
        }

        if (isDead)
        {
            DebugLog("HandleDeath ignored because enemy is already dead.");
            return;
        }

        isDead = true;

        StopEnemyMovement();
        DisableScripts();
        DisableColliders();
        DropItem();

        if (animator != null && !string.IsNullOrEmpty(deathTriggerName))
        {
            animator.SetTrigger(deathTriggerName);
            DebugLog($"Death animation triggered: {deathTriggerName}");
        }
        else
        {
            DebugLog("No animator found or death trigger name is empty.");
        }

        StartCoroutine(DeathCleanupRoutine());
    }

    private void StopEnemyMovement()
    {
        if (agent == null)
        {
            DebugLog("No NavMeshAgent to stop.");
            return;
        }

        if (agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;

            DebugLog("NavMeshAgent stopped and disabled.");
        }
    }

    private void DisableScripts()
    {
        if (scriptsToDisable == null)
            return;

        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script == null)
                continue;

            if (script == this)
                continue;

            script.enabled = false;
            DebugLog($"Disabled script: {script.GetType().Name}");
        }
    }

    private void DisableColliders()
    {
        if (collidersToDisable == null)
            return;

        foreach (Collider col in collidersToDisable)
        {
            if (col == null)
                continue;

            col.enabled = false;
            DebugLog($"Disabled collider: {col.name}");
        }
    }

    private void DropItem()
    {
        if (dropPrefab == null)
        {
            DebugLog("No drop prefab assigned.");
            return;
        }

        Vector3 spawnPosition = dropPoint != null ? dropPoint.position : transform.position;
        Quaternion spawnRotation = dropPoint != null ? dropPoint.rotation : Quaternion.identity;

        Instantiate(dropPrefab, spawnPosition, spawnRotation);

        DebugLog($"Dropped item: {dropPrefab.name}");
    }

    private IEnumerator DeathCleanupRoutine()
    {
        DebugLog($"Death cleanup started. Destroy delay: {destroyDelay}, Fade: {fadeBeforeDestroy}");

        float waitBeforeFade = fadeBeforeDestroy ? Mathf.Max(0f, destroyDelay - fadeDuration) : destroyDelay;

        yield return new WaitForSeconds(waitBeforeFade);

        if (fadeBeforeDestroy)
            yield return StartCoroutine(FadeOutRoutine());

        DebugLogError("Destroying enemy GameObject now.");
        Destroy(gameObject);
    }

    private IEnumerator FadeOutRoutine()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            DebugLog("No renderers found for fade.");
            yield break;
        }

        DebugLog("Fade out started.");

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);

            foreach (Renderer rend in renderers)
            {
                foreach (Material mat in rend.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color color = mat.color;
                        color.a = alpha;
                        mat.color = color;
                    }
                }
            }

            yield return null;
        }

        DebugLog("Fade out finished.");
    }

    private void DebugLog(string message)
    {
        if (!showDebugs)
            return;

        Debug.Log($"[EnemyDeathHandler] [{gameObject.name}] {message}", this);
    }

    private void DebugLogError(string message)
    {
        if (!showDebugs)
            return;

        Debug.LogError($"[EnemyDeathHandler] [{gameObject.name}] {message}", this);
    }
}