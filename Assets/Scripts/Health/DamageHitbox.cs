using System.Collections.Generic;
using UnityEngine;

// DamageHitbox:
// ├── For animation-based attacks
// ├── Activate with animation event
// ├── Deactivate with animation event
// ├── Damages Health components
// └── Debugs exactly what it hits

public class DamageHitbox : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private string requiredTag = "Player";

    [Header("Hitbox Settings")]
    [SerializeField] private Collider hitboxCollider;
    [SerializeField] private bool disableOnStart = true;
    [SerializeField] private bool canHitSameTargetOncePerActivation = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugs = true;

    private readonly HashSet<Health> alreadyHitTargets = new HashSet<Health>();

    private void Awake()
    {
        if (hitboxCollider == null)
            hitboxCollider = GetComponent<Collider>();

        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
            DebugLog($"Found collider: {hitboxCollider.name}. IsTrigger forced to true.");
        }
        else
        {
            DebugLogError("No collider found. Add a BoxCollider/SphereCollider to this hitbox.");
        }
    }

    private void Start()
    {
        if (disableOnStart)
            DeactivateHitbox();
    }

    public void ActivateHitbox()
    {
        alreadyHitTargets.Clear();

        if (hitboxCollider != null)
            hitboxCollider.enabled = true;

        DebugLogError("Hitbox ACTIVATED.");
    }

    public void DeactivateHitbox()
    {
        if (hitboxCollider != null)
            hitboxCollider.enabled = false;

        DebugLog("Hitbox deactivated.");
    }

    private void OnTriggerEnter(Collider other)
    {
        DebugLog($"OnTriggerEnter with: {other.gameObject.name}");
        TryDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider other)
    {
        if (!IsCorrectLayer(other.gameObject))
        {
            DebugLog($"Ignored {other.gameObject.name}. Wrong layer: {LayerMask.LayerToName(other.gameObject.layer)}");
            return;
        }

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
        {
            DebugLog($"Ignored {other.gameObject.name}. Required tag: {requiredTag}, Actual tag: {other.tag}");
            return;
        }

        Health health = other.GetComponentInParent<Health>();

        if (health == null)
        {
            DebugLog($"Ignored {other.gameObject.name}. No Health found in parent.");
            return;
        }

        if (canHitSameTargetOncePerActivation && alreadyHitTargets.Contains(health))
        {
            DebugLog($"Ignored {health.gameObject.name}. Already hit this activation.");
            return;
        }

        alreadyHitTargets.Add(health);

        DebugLogError($"Damaging {health.gameObject.name} for {damage}.");

        health.TakeDamage(damage);
    }

    private bool IsCorrectLayer(GameObject targetObject)
    {
        if (damageableLayers.value == 0)
            return true;

        return (damageableLayers.value & (1 << targetObject.layer)) != 0;
    }

    private void DebugLog(string message)
    {
        if (!showDebugs)
            return;

        Debug.Log($"[DamageHitbox] [{gameObject.name}] {message}", this);
    }

    private void DebugLogError(string message)
    {
        if (!showDebugs)
            return;

        Debug.LogError($"[DamageHitbox] [{gameObject.name}] {message}", this);
    }
}