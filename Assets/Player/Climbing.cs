using UnityEngine;

public class Climbing : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Rigidbody rb;
    public PlayerMovement pm;
    public LayerMask whatIsWall;

    [Header("Climbing")]
    public float climbSpeed;
    public float maxClimbTime;
    private float climbTimer;
    private bool climbing;

    [Header("Climb Jumping")]
    public float climbJumpUpForce;
    public float climbJumpBackForce;
    public KeyCode jumpKey = KeyCode.Space;
    public int climbJumps;
    private int climbJumpsLeft;

    [Header("Sticking")]
    public float maxStickTime;
    private bool sticking;
    private float stickTimer;


    [Header("Detection")]
    public float detectionLength;
    public float sphereCastRadius;
    public float maxWallLookAngle;
    private float wallLookAngle;

    private RaycastHit frontWallHit;
    private bool wallFront;
    private Transform lastWall;
    private Vector3 lastWallNormal;
    public float minWallNormalAngleChange;

    private void Update()
    {
        WallCheck();
        StateMachine();

        if (climbing) ClimbingMovement();
        if (sticking) StickBehaviour();

    }

    private void StateMachine()
    {
        // State 1: climbing
        if (wallFront && Input.GetKey(KeyCode.W) && wallLookAngle < maxWallLookAngle)
        {
            if (!climbing && climbTimer > 0) StartClimbing();

            if (climbTimer > 0) climbTimer -= Time.deltaTime;
            if (climbTimer <= 0) StopClimbing();

        }
        // State 2: sticking
        else
        {
            if (climbing)
            {
                StopClimbing();
                StartStick();

            }
        }

        // Climb jump
        if (wallFront && Input.GetKeyDown(jumpKey) && climbJumpsLeft > 0)
            ClimbJump();

    }

    private void WallCheck()
    {
        wallFront = Physics.SphereCast(
            transform.position,
            sphereCastRadius,
            orientation.forward,
            out frontWallHit,
            detectionLength,
            whatIsWall

        );

        if (wallFront)
            wallLookAngle = Vector3.Angle(orientation.forward, -frontWallHit.normal);

        bool newWall = frontWallHit.transform != lastWall ||
                       Mathf.Abs(Vector3.Angle(lastWallNormal, frontWallHit.normal)) > minWallNormalAngleChange;

        //if ((wallFront && newWall) || pm.grounded)
        //{
            //climbTimer = maxClimbTime;
            //limbJumpsLeft = climbJumps;

        //}
    }

    private void StartClimbing()
    {
        climbing = true;
        lastWall = frontWallHit.transform;
        lastWallNormal = frontWallHit.normal;
        rb.useGravity = false;

    }

    private void ClimbingMovement()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, climbSpeed, rb.linearVelocity.z);

    }

    private void StopClimbing()
    {
        climbing = false;

    }

    private void ClimbJump()
    {
        Vector3 forceToApply = transform.up * climbJumpUpForce + frontWallHit.normal * climbJumpBackForce;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);

        climbJumpsLeft--;
        ReleaseStick();

    }

    private void StartStick()
    {
        sticking = true;
        stickTimer = maxStickTime;

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

    }

    private void StickBehaviour()
    {
        stickTimer -= Time.deltaTime;

        // Break stick if timer runs out OR player presses input
        if (stickTimer <= 0f || Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0 || Input.GetKeyDown(jumpKey))
        {
            ReleaseStick();

        }
    }

    private void ReleaseStick()
    {
        sticking = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

    }
}
