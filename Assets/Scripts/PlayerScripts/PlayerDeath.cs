using System.Collections;
using UnityEngine;

// PlayerDeathHandler:
// ├── Player-only death script
// ├── Disables movement scripts
// ├── Plays death animation
// ├── Respawns after delay for now
// └── Does NOT destroy the player

[RequireComponent(typeof(Health))]
public class PlayerDeath : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private CharacterController characterController;

    [Header("Scripts To Disable On Death")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    [Header("Respawn Settings")]
    [SerializeField] private bool autoRespawn = true;
    [SerializeField] private float respawnDelay = 2.5f;

    [Header("Animation Parameters")]
    [SerializeField] private string deathTriggerName = "Die";
    [SerializeField] private string respawnTriggerName = "Respawn";

    [Header("Debug")]
    [SerializeField] private bool showDebugs = true;

    private bool isHandlingDeath;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        DebugLog($"Awake complete. AutoRespawn: {autoRespawn}, RespawnDelay: {respawnDelay}");
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

        if (isHandlingDeath)
        {
            DebugLog("Death ignored because death routine is already running.");
            return;
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        isHandlingDeath = true;

        DebugLogError("Player death routine started.");

        CancelMovement();
        SetMovementScripts(false);

        if (animator != null && !string.IsNullOrEmpty(deathTriggerName))
        {
            animator.SetTrigger(deathTriggerName);
            DebugLog($"Death animation triggered: {deathTriggerName}");
        }
        else
        {
            DebugLog("No animator found or death trigger is empty.");
        }

        // Later:
        // fade to black
        // continue button
        // quit button

        if (autoRespawn)
        {
            DebugLog($"Waiting {respawnDelay} seconds before respawn.");
            yield return new WaitForSeconds(respawnDelay);
            RespawnPlayer();
        }
        else
        {
            DebugLog("AutoRespawn is false. Waiting for UI to call RespawnPlayer later.");
        }
    }

    public void RespawnPlayer()
    {
        DebugLogError("RespawnPlayer called.");

        if (characterController != null)
        {
            characterController.enabled = false;
            DebugLog("CharacterController disabled for reposition.");
        }

        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
            DebugLog($"Moved player to respawn point: {respawnPoint.name}");
        }
        else
        {
            DebugLog("No respawn point assigned. Player will respawn at current position.");
        }

        if (characterController != null)
        {
            characterController.enabled = true;
            DebugLog("CharacterController re-enabled.");
        }

        CancelMovement();

        if (health != null)
            health.Respawn();

        SetMovementScripts(true);

        if (animator != null && !string.IsNullOrEmpty(respawnTriggerName))
        {
            animator.SetTrigger(respawnTriggerName);
            DebugLog($"Respawn animation triggered: {respawnTriggerName}");
        }

        isHandlingDeath = false;

        DebugLog("Player death routine finished.");
    }

    private void CancelMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            DebugLog("Rigidbody velocity canceled.");
        }
        else
        {
            DebugLog("No Rigidbody found to cancel.");
        }
    }

    private void SetMovementScripts(bool enabled)
    {
        if (scriptsToDisable == null)
            return;

        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script == null)
                continue;

            if (script == this)
                continue;

            script.enabled = enabled;
            DebugLog($"{(enabled ? "Enabled" : "Disabled")} script: {script.GetType().Name}");
        }
    }

    private void DebugLog(string message)
    {
        if (!showDebugs)
            return;

        Debug.Log($"[PlayerDeathHandler] [{gameObject.name}] {message}", this);
    }

    private void DebugLogError(string message)
    {
        if (!showDebugs)
            return;

        Debug.LogError($"[PlayerDeathHandler] [{gameObject.name}] {message}", this);
    }
}