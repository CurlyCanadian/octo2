using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Expanding / lingering / dispersing ink cloud with a visibility multiplier field.
/// Other systems (cameras, enemies) can call InkCloud.GetVisibilityMultiplier(worldPos)
/// to reduce detection inside/near the cloud.
/// </summary>
[DisallowMultipleComponent]
public class InkCloud : MonoBehaviour
{
    [Header("Lifecycle")]
    [SerializeField, Tooltip("How fast the cloud expands to MaxRadius (seconds)")]
    private float expandTime = 0.6f;
    [SerializeField, Tooltip("Time at max size before dispersing (seconds)")]
    private float lingerTime = 3.0f;
    [SerializeField, Tooltip("How long it takes to fully disperse (seconds)")]
    private float fadeTime = 1.2f;

    [Header("Size / Field")]
    [SerializeField, Tooltip("Final radius of the obscuring field (meters)")]
    private float maxRadius = 4.0f;
    [SerializeField, Tooltip("Minimum visibility multiplier at the cloud center")]
    private float minVisibilityMultiplier = 0.25f; // 0.25 = 75% reduction at the core
    [SerializeField, Tooltip("Edge softness for falloff (0 = hard edge, 1 = soft)")]
    private float edgeFeather = 0.35f;

    [Header("FX (optional)")]
    [SerializeField] private ParticleSystem particles; // assign on prefab
    [SerializeField] private AudioSource sfxSpawn;     // optional

    // Runtime state
    private float t;
    private float phaseEnd_expand;
    private float phaseEnd_linger;
    private float phaseEnd_fade;

    private float currentRadius;   // for Gizmos & static field
    private float currentDensity;  // 0..1 (used in static field)

    // Static registry: lets sensors query any active cloud
    private static readonly List<InkCloud> s_active = new List<InkCloud>();

    // --------- Public API ---------

    /// <summary>Returns a visibility multiplier (0..1) for the given world position,
    /// considering all active ink clouds. 1 = no reduction. Lower = more hidden.</summary>
    public static float GetVisibilityMultiplier(Vector3 worldPos)
    {
        float result = 1f;
        for (int i = 0; i < s_active.Count; i++)
        {
            var c = s_active[i];
            // Skip if cloud is effectively gone
            if (c.currentDensity <= 0.001f || c.currentRadius <= 0.001f) continue;

            // Distance-based falloff with edge feather
            float d = Vector3.Distance(worldPos, c.transform.position);
            if (d > c.currentRadius) continue;

            float x = Mathf.Clamp01(1f - (d / Mathf.Max(0.001f, c.currentRadius))); // 1 at center → 0 at edge
            // Feathered edge (ease curve)
            float falloff = Mathf.SmoothStep(0f, 1f, Mathf.Pow(x, Mathf.Lerp(1f, 3f, c.edgeFeather)));

            // Blend to the cloud's current strength (density fades in/out)
            float cloudStrength = Mathf.Lerp(1f, c.minVisibilityMultiplier, falloff * c.currentDensity);

            // Stack clouds by taking the minimum (most obscured wins)
            result = Mathf.Min(result, cloudStrength);
        }
        return result;
    }

    // --------- MonoBehaviour ---------

    private void OnEnable()
    {
        if (!s_active.Contains(this)) s_active.Add(this);

        // Phase timings
        t = 0f;
        phaseEnd_expand = expandTime;
        phaseEnd_linger = phaseEnd_expand + lingerTime;
        phaseEnd_fade   = phaseEnd_linger + fadeTime;

        if (!particles) particles = GetComponentInChildren<ParticleSystem>();
        if (sfxSpawn) sfxSpawn.Play();

        // Start FX roughly at radius 0
        ApplyFxSize(0f);
    }

    private void OnDisable()
    {
        s_active.Remove(this);
    }

    private void Update()
    {
        t += Time.deltaTime;

        // Compute normalized phase (0..1 per segment)
        float radius01;
        float density01;

        if (t <= phaseEnd_expand)
        {
            // Expand up
            float a = Mathf.Clamp01(t / Mathf.Max(0.0001f, expandTime));
            radius01  = a;
            density01 = a; // fade in while expanding
        }
        else if (t <= phaseEnd_linger)
        {
            // Hold
            radius01  = 1f;
            density01 = 1f;
        }
        else if (t <= phaseEnd_fade)
        {
            // Disperse (fade out)
            float f = Mathf.Clamp01((t - phaseEnd_linger) / Mathf.Max(0.0001f, fadeTime));
            radius01  = 1f;           // keep size, just reduce density
            density01 = 1f - f;
        }
        else
        {
            // Done
            currentRadius  = 0f;
            currentDensity = 0f;
            Destroy(gameObject);
            return;
        }

        currentRadius  = radius01 * maxRadius;
        currentDensity = density01;

        ApplyFxSize(currentRadius);
        ApplyFxOpacity(currentDensity);
    }

    // --------- FX helpers ---------

    private void ApplyFxSize(float radius)
    {
        if (!particles) return;

        var shape = particles.shape; // struct proxy
        // If using a Sphere/Donut shape, radius applies visually
        if (shape.enabled)
        {
            shape.radius = Mathf.Max(0f, radius);
        }
        // Optionally scale transform as well if your particle uses size-over-lifetime
        // transform.localScale = Vector3.one * Mathf.Max(0.001f, radius * 0.5f);
    }

    private void ApplyFxOpacity(float density01)
    {
        if (!particles) return;
        var main = particles.main;
        var c = main.startColor;
        // Multiply alpha by density (supports MinMaxGradient with single color)
        Color col = c.color;
        col.a = Mathf.Clamp01(density01) * 0.9f; // cap to avoid full opaque blobs
        main.startColor = col;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize current field
        Gizmos.color = new Color(0f, 0f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, currentRadius));
        Gizmos.color = new Color(0f, 0f, 0f, 0.05f + 0.2f * currentDensity);
        Gizmos.DrawSphere(transform.position, Mathf.Max(0.01f, currentRadius * 0.98f));
    }
#endif
}
