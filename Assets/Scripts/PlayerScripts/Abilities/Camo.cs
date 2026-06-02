using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CamouflageController : MonoBehaviour
{
    [Header("Renderer using the camo material (Shader Graph)")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Crouch + Camo Binding")]
    [Tooltip("Camo only activates while this crouch key is held.")]
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Tooltip("If true, camouflage can only activate while crouch is held.")]
    [SerializeField] private bool requireCrouchForCamo = true;

    [Tooltip("Optional. If assigned, camo can require the player to be grounded.")]
    [SerializeField] private PlayerMovement playerMovement;

    [Tooltip("If true, camo only works while grounded. Turn off if you want midair crouch/camo later.")]
    [SerializeField] private bool requireGrounded = false;

    [Header("Enemy Detection Values For Later")]
    [Tooltip("Enemies can still detect the player inside this range, even while camouflaged.")]
    [SerializeField] private float closeRevealDistance = 1.25f;

    [Tooltip("Enemy detection range multiplier while camouflaged. 0 = almost invisible except close reveal range.")]
    [SerializeField] [Range(0f, 1f)] private float camoDetectionMultiplier = 0.15f;

    [Header("Transition")]
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private AnimationCurve curve = null; // if null → EaseInOut will be created
    [SerializeField] private float pulseSpeed = 2.0f;     // how fast the bloom ring expands

    [Header("Pulse")]
    [Tooltip("If true, the pulse starts at the point you're looking at. If false, it starts at the player position.")]
    [SerializeField] private bool useAimPulse = false;

    [SerializeField] private LayerMask aimRayMask = ~0;
    [SerializeField] private float aimRayMaxDistance = 100f;

    [Header("Debug Hotkeys")]
    [Tooltip("Editor-only testing keys. Keep this off for normal gameplay crouch-camo.")]
    [SerializeField] private bool debugHotkeys = false;

    [SerializeField] private KeyCode cloakKey = KeyCode.C;
    [SerializeField] private KeyCode uncloakKey = KeyCode.V;

    [Header("Debug Logs")]
    [SerializeField] private bool debugCamoState = true;

    // Shader property IDs must match your Shader Graph property names
    private static readonly int BlendID = Shader.PropertyToID("_Blend");
    private static readonly int PulseCenterID = Shader.PropertyToID("_PulseCenter");
    private static readonly int PulseRadiusID = Shader.PropertyToID("_PulseRadius");

    public bool IsCamouflaged { get; private set; }
    public bool IsCrouchHeld { get; private set; }
    public float CloseRevealDistance => closeRevealDistance;
    public float CamoDetectionMultiplier => camoDetectionMultiplier;

    private MaterialPropertyBlock mpb;
    private Coroutine camoRoutine;

    private float currentBlend = 0f;
    private bool desiredCamoState = false;
    private bool wasCamouflagedLastFrame = false;

    private void Awake()
    {
        if (!targetRenderer)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (!targetRenderer)
        {
            Debug.LogError("CamouflageController: No Renderer found/assigned. Please assign Target Renderer.", this);
            enabled = false;
            return;
        }

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        mpb = new MaterialPropertyBlock();

        if (curve == null)
            curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // Initialize shader properties
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(BlendID, 0f);
        mpb.SetFloat(PulseRadiusID, 0f);
        mpb.SetVector(PulseCenterID, transform.position);
        targetRenderer.SetPropertyBlock(mpb);

        IsCamouflaged = false;
        desiredCamoState = false;
        currentBlend = 0f;
    }

    private void Update()
    {
        UpdateCrouchCamouflage();
        HandleDebugHotkeys();
        DebugCamouflageState();
    }

    private void UpdateCrouchCamouflage()
    {
        IsCrouchHeld = Input.GetKey(crouchKey);

        bool canCamo = true;

        if (requireCrouchForCamo)
            canCamo = canCamo && IsCrouchHeld;

        if (requireGrounded && playerMovement != null)
            canCamo = canCamo && playerMovement.Grounded;

        SetCamoFromGameplay(canCamo);
    }

    private void SetCamoFromGameplay(bool turnOn)
    {
        if (desiredCamoState == turnOn)
            return;

        desiredCamoState = turnOn;

        SetPulseCenter(GetPulseStartPoint());
        ToggleCamo(turnOn);
    }

    private void HandleDebugHotkeys()
    {
#if UNITY_EDITOR
        if (!debugHotkeys)
            return;

        if (Input.GetKeyDown(cloakKey))
        {
            SetPulseCenter(GetPulseStartPoint());
            ToggleCamo(true);
            desiredCamoState = true;
        }

        if (Input.GetKeyDown(uncloakKey))
        {
            SetPulseCenter(GetPulseStartPoint());
            ToggleCamo(false);
            desiredCamoState = false;
        }
#endif
    }

    /// <summary>
    /// Sets where the bloom ring starts in world space.
    /// </summary>
    public void SetPulseCenter(Vector3 worldPos)
    {
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetVector(PulseCenterID, worldPos);
        mpb.SetFloat(PulseRadiusID, 0f);
        targetRenderer.SetPropertyBlock(mpb);
    }

    /// <summary>
    /// Starts or ends the camouflage blend animation.
    /// </summary>
    public void ToggleCamo(bool turnOn)
    {
        if (camoRoutine != null)
            StopCoroutine(camoRoutine);

        camoRoutine = StartCoroutine(CamoRoutine(turnOn));
    }

    private IEnumerator CamoRoutine(bool turnOn)
    {
        float startBlend = currentBlend;
        float endBlend = turnOn ? 1f : 0f;

        float t = 0f;
        float pulse = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(t / fadeDuration);
            float a = curve.Evaluate(normalizedTime);

            currentBlend = Mathf.Lerp(startBlend, endBlend, a);
            pulse += Time.deltaTime * pulseSpeed;

            targetRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat(BlendID, currentBlend);
            mpb.SetFloat(PulseRadiusID, pulse);
            targetRenderer.SetPropertyBlock(mpb);

            IsCamouflaged = currentBlend > 0.5f;

            yield return null;
        }

        currentBlend = endBlend;
        IsCamouflaged = turnOn;

        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(BlendID, currentBlend);
        targetRenderer.SetPropertyBlock(mpb);

        camoRoutine = null;
    }

    /// <summary>
    /// External scripts can call this later if crouch is handled somewhere else.
    /// Example: PlayerController can call SetCamoInput(isCrouching).
    /// </summary>
    public void SetCamoInput(bool isCrouching)
    {
        if (!requireCrouchForCamo)
            return;

        SetCamoFromGameplay(isCrouching);
    }

    /// <summary>
    /// Later, enemies can call this to check whether close-range detection should ignore camo.
    /// </summary>
    public bool CanEnemyDetectAtCloseRange(Transform enemy)
    {
        if (enemy == null)
            return false;

        float distance = Vector3.Distance(enemy.position, transform.position);
        return distance <= closeRevealDistance;
    }

    /// <summary>
    /// Later, enemies can call this to shrink their detection range while player is camouflaged.
    /// </summary>
    public float GetDetectionMultiplierForEnemy(Transform enemy)
    {
        if (!IsCamouflaged)
            return 1f;

        if (CanEnemyDetectAtCloseRange(enemy))
            return 1f;

        return camoDetectionMultiplier;
    }

    private void DebugCamouflageState()
    {
        if (!debugCamoState)
            return;

        if (IsCamouflaged == wasCamouflagedLastFrame)
            return;

        if (IsCamouflaged)
        {
            Debug.Log(
                "CamouflageController: CAMO ACTIVE. Player is crouched. " +
                $"Enemy detection multiplier: {camoDetectionMultiplier}, Close reveal distance: {closeRevealDistance}"
            );
        }
        else
        {
            Debug.Log("CamouflageController: CAMO OFF. Player is not crouched or camo condition failed.");
        }

        wasCamouflagedLastFrame = IsCamouflaged;
    }

    private Vector3 GetPulseStartPoint()
    {
        if (!useAimPulse)
            return transform.position;

        Camera cam = Camera.main;

        if (!cam)
            return transform.position;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, aimRayMaxDistance, aimRayMask, QueryTriggerInteraction.Ignore))
            return hit.point;

        return transform.position;
    }
}