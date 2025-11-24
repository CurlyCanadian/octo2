using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

//
// AFK.cs
// "player is AFK -> camera swings to the octo's face"
//
// does NOT affect normal camera play
// ONLY takes over after idleTimeToTrigger seconds of no input
//
// Put this on the same GameObject as your Cinemachine FreeLook camera.
// (the one with CinemachineCamera + CinemachineOrbitalFollow)
//

public class AFK : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;          // octo root (or playerObj)
    [SerializeField] private CinemachineOrbitalFollow orbital; 
    // auto-grabbed if null

    [Header("AFK Timing")]
    [SerializeField] private float idleTimeToTrigger = 6f;  // seconds before AFK kicks in
    [SerializeField] private float recenterDuration = 2f;  // how long swing takes

    [Header("Face View Settings")]
    [Tooltip("180 puts camera in front of where player is facing.")]
    [SerializeField] private float faceYawOffset = 180f;
    [Tooltip("Your favorite vertical axis value for the AFK pose.")]
    [SerializeField] private float faceVerticalValue = 17.5f;

    [Header("Input Sensitivity")]
    [SerializeField] private float moveDeadzone = 0.01f;
    [SerializeField] private float lookDeadzone = 0.01f;

    private float idleTimer;
    private Coroutine recenterRoutine;
    private bool isRecentering;

    private void Awake()
    {
        if (orbital == null)
            orbital = GetComponent<CinemachineOrbitalFollow>();

        if (player == null)
            Debug.LogError("AFK: player reference is missing!");
    }

    private void Update()
    {
        if (orbital == null || player == null) return;

        bool playerMoving =
            Mathf.Abs(Input.GetAxisRaw("Horizontal")) > moveDeadzone ||
            Mathf.Abs(Input.GetAxisRaw("Vertical")) > moveDeadzone;

        bool cameraMoving =
            Mathf.Abs(Input.GetAxisRaw("Mouse X")) > lookDeadzone ||
            Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > lookDeadzone;

        bool anyButtons =
            Input.GetMouseButton(0) || Input.GetMouseButton(1) ||
            Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.Q);

        // If ANY activity, cancel AFK + reset timer
        if (playerMoving || cameraMoving || anyButtons)
        {
            idleTimer = 0f;

            if (isRecentering && recenterRoutine != null)
                StopCoroutine(recenterRoutine);

            isRecentering = false;
            return;
        }

        // otherwise we’re idle
        idleTimer += Time.deltaTime;

        if (!isRecentering && idleTimer >= idleTimeToTrigger)
        {
            recenterRoutine = StartCoroutine(RecenterToFace());
        }
    }

    private IEnumerator RecenterToFace()
    {
        isRecentering = true;

        float startYaw = orbital.HorizontalAxis.Value;
        float startV   = orbital.VerticalAxis.Value;

        // Put camera in front of player:
        // player yaw + 180 degrees
        float playerYaw = player.eulerAngles.y;
        float targetYaw = Mathf.Repeat(playerYaw + faceYawOffset, 360f);

        float t = 0f;
        while (t < recenterDuration)
        {
            // If input wakes up mid-swing, bail
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > moveDeadzone ||
                Mathf.Abs(Input.GetAxisRaw("Vertical")) > moveDeadzone ||
                Mathf.Abs(Input.GetAxisRaw("Mouse X")) > lookDeadzone ||
                Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > lookDeadzone)
            {
                isRecentering = false;
                yield break;
            }

            t += Time.deltaTime;
            float a = t / recenterDuration;

            orbital.HorizontalAxis.Value = Mathf.LerpAngle(startYaw, targetYaw, a);
            orbital.VerticalAxis.Value   = Mathf.Lerp(startV, faceVerticalValue, a);

            yield return null;
        }

        orbital.HorizontalAxis.Value = targetYaw;
        orbital.VerticalAxis.Value   = faceVerticalValue;

        isRecentering = false;
    }
}
