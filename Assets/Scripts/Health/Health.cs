using UnityEngine;
using UnityEngine.Events;

// Health:
// ├── Universal health for player + enemies
// ├── Player can have 8 HP
// ├── Rats/enemies can have 3 HP
// ├── Sends death events
// └── Debugs basically everything important

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 8;
    [SerializeField] private bool startAtFullHealth = true;

    [Header("Damage Settings")]
    [SerializeField] private bool canTakeDamage = true;
    [SerializeField] private float invulnerabilityTime = 0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugs = true;
    [SerializeField] private string debugNameOverride = "";

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool CanTakeDamage => canTakeDamage && !IsDead;

    [Header("Events")]
    public UnityEvent<int, int> OnHealthChanged; 
    public UnityEvent<int> OnDamaged;
    public UnityEvent<int> OnHealed;
    public UnityEvent OnDied;
    public UnityEvent OnRespawned;

    private float lastDamageTime = -999f;

    private string DebugName
    {
        get
        {
            if (!string.IsNullOrEmpty(debugNameOverride))
                return debugNameOverride;

            return gameObject.name;
        }
    }

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);

        if (startAtFullHealth)
            CurrentHealth = maxHealth;
        else
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        DebugLog($"Awake complete. HP: {CurrentHealth}/{maxHealth}. Tag: {gameObject.tag}. Layer: {LayerMask.LayerToName(gameObject.layer)}");

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void TakeDamage(int damageAmount)
    {
        DebugLog($"TakeDamage called with amount: {damageAmount}");

        if (damageAmount <= 0)
        {
            DebugLog("Damage ignored because amount was 0 or less.");
            return;
        }

        if (IsDead)
        {
            DebugLog("Damage ignored because object is already dead.");
            return;
        }

        if (!canTakeDamage)
        {
            DebugLog("Damage ignored because canTakeDamage is false.");
            return;
        }

        if (Time.time < lastDamageTime + invulnerabilityTime)
        {
            DebugLog("Damage ignored because object is invulnerable right now.");
            return;
        }

        lastDamageTime = Time.time;

        CurrentHealth -= damageAmount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        DebugLog($"Took {damageAmount} damage. HP: {CurrentHealth}/{maxHealth}");

        OnDamaged?.Invoke(damageAmount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        if (healAmount <= 0)
        {
            DebugLog("Heal ignored because amount was 0 or less.");
            return;
        }

        if (IsDead)
        {
            DebugLog("Heal ignored because object is dead.");
            return;
        }

        CurrentHealth += healAmount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        DebugLog($"Healed {healAmount}. HP: {CurrentHealth}/{maxHealth}");

        OnHealed?.Invoke(healAmount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void RestoreToFull()
    {
        IsDead = false;
        canTakeDamage = true;
        CurrentHealth = maxHealth;

        DebugLog($"Restored to full health. HP: {CurrentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void SetMaxHealth(int newMaxHealth, bool refillHealth)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);

        if (refillHealth)
            CurrentHealth = maxHealth;
        else
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        DebugLog($"Max health changed. HP: {CurrentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void SetCanTakeDamage(bool value)
    {
        canTakeDamage = value;
        DebugLog($"CanTakeDamage set to: {canTakeDamage}");
    }

    public void Kill()
    {
        DebugLog("Kill called.");

        if (IsDead)
            return;

        CurrentHealth = 0;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        Die();
    }

    private void Die()
    {
        if (IsDead)
        {
            DebugLog("Die ignored because already dead.");
            return;
        }

        IsDead = true;
        canTakeDamage = false;

        DebugLogError($"DIED. If this is the player unexpectedly, check what called TakeDamage/Kill.");

        OnDied?.Invoke();
    }

    public void Respawn()
    {
        IsDead = false;
        canTakeDamage = true;
        CurrentHealth = maxHealth;
        lastDamageTime = -999f;

        DebugLog($"Respawned. HP: {CurrentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnRespawned?.Invoke();
    }

    private void DebugLog(string message)
    {
        if (!showDebugs)
            return;

        Debug.Log($"[Health] [{DebugName}] {message}", this);
    }

    private void DebugLogError(string message)
    {
        if (!showDebugs)
            return;

        Debug.LogError($"[Health] [{DebugName}] {message}", this);
    }
}