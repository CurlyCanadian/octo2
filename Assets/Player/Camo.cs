using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CamouflageController : MonoBehaviour
{
    [Header("Renderer using the camo material (Shader Graph)")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Transition")]
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private AnimationCurve curve = null; // if null → EaseInOut will be created
    [SerializeField] private float pulseSpeed = 2.0f;     // how fast the bloom ring expands

    [Header("Debug Hotkeys")]
    [SerializeField] private bool debugHotkeys = true;
    [SerializeField] private KeyCode cloakKey   = KeyCode.C;
    [SerializeField] private KeyCode uncloakKey = KeyCode.V;
    [Tooltip("If true, the pulse starts at the point you're looking at (camera ray). If false, it starts at the player position.")]
    [SerializeField] private bool useAimPulse = true;
    [SerializeField] private LayerMask aimRayMask = ~0;   // everything by default
    [SerializeField] private float aimRayMaxDistance = 100f;

    // Shader property IDs (must match your Shader Graph property names)
    private static readonly int BlendID       = Shader.PropertyToID("_Blend");
    private static readonly int PulseCenterID = Shader.PropertyToID("_PulseCenter");
    private static readonly int PulseRadiusID = Shader.PropertyToID("_PulseRadius");

    private MaterialPropertyBlock mpb;
    private Coroutine camoRoutine;
    private float currentBlend = 0f; // track locally; MPB doesn't expose getters in all Unity versions

    private void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>();
        if (!targetRenderer)
        {
            Debug.LogError("CamouflageController: No Renderer found/assigned. Please assign Target Renderer.", this);
            enabled = false;
            return;
        }

        mpb = new MaterialPropertyBlock();
        if (curve == null) curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // initialize properties
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(BlendID, 0f);
        mpb.SetFloat(PulseRadiusID, 0f);
        mpb.SetVector(PulseCenterID, transform.position);
        targetRenderer.SetPropertyBlock(mpb);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!debugHotkeys) return;

        if (Input.GetKeyDown(cloakKey))
        {
            SetPulseCenter(GetAimPoint());
            ToggleCamo(true);
        }
        if (Input.GetKeyDown(uncloakKey))
        {
            SetPulseCenter(GetAimPoint());
            ToggleCamo(false);
        }
#endif
    }

    /// <summary>Sets where the bloom ring starts (world space).</summary>
    public void SetPulseCenter(Vector3 worldPos)
    {
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetVector(PulseCenterID, worldPos);
        mpb.SetFloat(PulseRadiusID, 0f);
        targetRenderer.SetPropertyBlock(mpb);
    }

    /// <summary>Starts or ends the camouflage blend animation.</summary>
    public void ToggleCamo(bool turnOn)
    {
        if (camoRoutine != null) StopCoroutine(camoRoutine);
        camoRoutine = StartCoroutine(CamoRoutine(turnOn));
    }

    private IEnumerator CamoRoutine(bool turnOn)
    {
        float startBlend = currentBlend;
        float endBlend   = turnOn ? 1f : 0f;

        float t = 0f;
        float pulse = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = curve.Evaluate(Mathf.Clamp01(t / fadeDuration));
            currentBlend = Mathf.Lerp(startBlend, endBlend, a);

            pulse += Time.deltaTime * pulseSpeed;

            targetRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat(BlendID, currentBlend);
            mpb.SetFloat(PulseRadiusID, pulse);
            targetRenderer.SetPropertyBlock(mpb);

            yield return null;
        }

        // ensure final values
        currentBlend = endBlend;
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(BlendID, currentBlend);
        targetRenderer.SetPropertyBlock(mpb);
        camoRoutine = null;
    }

    /// <summary>
    /// Returns a world-space point from where the camera is looking (mouse/crosshair).
    /// Falls back to player position if no hit or no camera.
    /// </summary>
    private Vector3 GetAimPoint()
    {
        if (!useAimPulse) return transform.position;

        Camera cam = Camera.main;
        if (!cam) return transform.position;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, aimRayMaxDistance, aimRayMask, QueryTriggerInteraction.Ignore))
            return hit.point;

        return transform.position;
    }
}
