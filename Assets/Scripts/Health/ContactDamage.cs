using UnityEngine;

// ContactDamage:
// ├── For spikes, slime, fire, hazards, simple touch enemies
// └── Not ideal for animated bite attacks

public class ContactDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageCooldown = 1f;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private string requiredTag = "Player";

    [Header("Debug")]
    [SerializeField] private bool showDebugs = true;

    private float lastDamageTime;

    private void OnCollisionStay(Collision collision)
    {
        TryDamage(collision.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamage(other.gameObject);
    }

    private void TryDamage(GameObject targetObject)
    {
        if (Time.time < lastDamageTime + damageCooldown)
            return;

        if (!IsCorrectLayer(targetObject))
            return;

        if (!string.IsNullOrEmpty(requiredTag) && !targetObject.CompareTag(requiredTag))
            return;

        Health health = targetObject.GetComponentInParent<Health>();

        if (health == null)
            return;

        health.TakeDamage(damage);
        lastDamageTime = Time.time;

        if (showDebugs)
            Debug.Log($"[ContactDamage] Damaged {health.gameObject.name} for {damage}.", this);
    }

    private bool IsCorrectLayer(GameObject targetObject)
    {
        if (damageableLayers.value == 0)
            return true;

        return (damageableLayers.value & (1 << targetObject.layer)) != 0;
    }
}