using UnityEngine;

/// <summary>
/// Simple ink ability spawner with cooldown. Press Q to emit ink at/near the player.
/// </summary>
public class InkAbility : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private InkCloud inkCloudPrefab;
    [SerializeField, Tooltip("Spawn offset in local space (e.g., slightly behind/under the player)")]
    private Vector3 localSpawnOffset = new Vector3(0f, 0.5f, -0.2f);

    [Header("Timing")]
    [SerializeField] private float cooldown = 6f;

    [Header("Input")]
    [SerializeField] private KeyCode inkKey = KeyCode.Q;

    private float nextReadyTime;

    private void Update()
    {
        if (Input.GetKeyDown(inkKey))
        {
            TryFireInk();
        }
    }

    private void TryFireInk()
    {
        if (inkCloudPrefab == null) { Debug.LogWarning("InkAbility: Missing inkCloudPrefab", this); return; }
        if (Time.time < nextReadyTime) return;

        // Spawn at player + offset, aligned to ground if available
        Vector3 spawnPos = transform.TransformPoint(localSpawnOffset);

        if (Physics.Raycast(spawnPos + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 1.0f, ~0, QueryTriggerInteraction.Ignore))
        {
            spawnPos = hit.point + Vector3.up * 0.05f;
        }

        Instantiate(inkCloudPrefab, spawnPos, Quaternion.identity);

        nextReadyTime = Time.time + cooldown;
    }
}
