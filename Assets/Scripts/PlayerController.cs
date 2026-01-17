using UnityEngine;

public unsafe class PlayerController : MonoBehaviour
{
    public PlayerProcessor processor;

    [Header("Animations")]
    public Animator animator;
    public Transform model;

    // Animator parameters
    public string animid_speed = "Speed";
    public string animid_velocityX = "VelocityX";
    public string animid_velocityY = "VelocityY";
    public string animid_jump = "Jump";
    public string animid_grounded = "Grounded";
    public string animid_freeFall = "FreeFall";
    public string animid_motionSpeed = "MotionSpeed";
    public string animid_attack = "Attack";

    public float jumpIntervalTime = 0.5f;
    bool canJump;
    float groundedTimer;        // Updated in fixed update
    public float attackIntervalTime = 1.5f;
    bool canAttack;
    float attackIntervalTimer;  // Updated in dynamic update

    float velocityX;
    float velocityY;

    [Header("Jumping")]
    public LayerMask groundLayer;
    public LayerMask playerLayer;
    [Tooltip("The amount of time after leaving a ledge for which the player can still jump")]
    public float coyoteTime = 0.1f;
    float coyoteTimer = 0;
    [Tooltip("The distance from the ground at which the player will be considered grounded")]
    public float groundedDistance = 0.01f;
    bool grounded = true;

    Vector3 movement = new();

    [Header("Camera")]
    [Tooltip("The transform around which the camera handle will rotate")]
    public Transform camAnchor;
    [Tooltip("The object which the camera will copy the transform of")]
    public Transform camHandle;
    public float maxYLookAngle = 89f;
    public float minYLookAngle = -60f;
    public float maxCamDistance = 6.5f;
    public float camClipDistance = 0.5f;
    Vector2 lookAngles;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        groundedTimer = jumpIntervalTime;
        attackIntervalTimer = attackIntervalTime;
    }

    void Update()
    {
        attackIntervalTimer += Time.deltaTime;
        DynamicUpdateInput(processor.inputData);

        fixed (PlayerData* playerData = &processor.playerData)
        {
            UpdatePosition(playerData);
            UpdateAnimator(processor.inputData, playerData);
        }
    }

    void DynamicUpdateInput(InputData inputData)
    {
        canAttack = attackIntervalTimer > attackIntervalTime;

        if (inputData.Attack && canAttack)
        {
            OnAttack();
        }
    }

    void UpdateAnimator(InputData inputData, PlayerData* playerData)
    {
        animator.SetFloat(animid_speed, movement.magnitude);
        animator.SetFloat(animid_velocityX, velocityX = Mathf.Lerp(velocityX, movement.x / playerData->Speed, 0.01f));
        animator.SetFloat(animid_velocityY, velocityY = Mathf.Lerp(velocityY, movement.z / playerData->Speed, 0.01f));
        animator.SetBool(animid_jump, inputData.Jump && canJump);
        animator.SetBool(animid_grounded, grounded);
        animator.SetBool(animid_freeFall, !grounded && !(inputData.Jump && canJump));
        animator.SetFloat(animid_motionSpeed, inputData.MoveDelta.magnitude);
        animator.SetBool(animid_attack, inputData.Attack && canAttack);
    }

    void UpdatePosition(PlayerData* playerData)
    {
        Vector3 moveDelta = playerData->Position - transform.position;
        float pad = GameManager.RaycastPad;
        // Correct the desired position if it clips through an object
        if (Physics.Raycast(
            transform.position,
            moveDelta + -moveDelta.normalized * pad,
            out RaycastHit hit,
            Vector3.Distance(transform.position, playerData->Position) + pad,
            ~playerLayer
        ))
        {
            playerData->Position = hit.point;
        }

        // Interpolate between current position and desired position
        transform.position = Vector3.Lerp(transform.position, playerData->Position, GameManager.InterpolationConstant);
    }

    // Called 50 times per second
    public void FixedUpdate()
    {
        fixed (PlayerData* playerData = &processor.playerData)
        {
            // If any of this gets moved out of FixedUpdate(), remember to change Time.fixedDeltaTime
            UpdateGroundState(playerData, Vector3.up);
            Move(processor.inputData, playerData);
            processor.SimulatePhysics(grounded);
        }
    }

    public void LateUpdate()
    {
        fixed (PlayerData* playerData = &processor.playerData)
        {
            // If any of this gets moved out of LateUpdate(), remember to change Time.deltaTime
            Look(processor.inputData, playerData);
        }
    }

    void UpdateGroundState(PlayerData* playerData, Vector3 up)
    {
        // Pad the raycast so the ground collider is detected when ground.y = player.y
        float pad = GameManager.RaycastPad;
        if (Physics.Raycast(playerData->Position + up * pad, -up, groundedDistance + pad, groundLayer))
        {
            grounded = true;
            coyoteTimer = 0;
        }
        else
        {
            coyoteTimer += Time.fixedDeltaTime;
            if (coyoteTimer > coyoteTime)
            {
                grounded = false;
            }
        }
    }

    public void Move(InputData inputData, PlayerData* playerData)
    {
        canJump = groundedTimer > jumpIntervalTime && grounded;

        if (grounded)
        {
            // Update canJump
            groundedTimer += Time.fixedDeltaTime;
        }
        
        if (grounded && attackIntervalTimer > attackIntervalTime)
        {
            // Parse input
            Vector2 moveDelta = inputData.MoveDelta;
            movement = new(moveDelta.x, 0, moveDelta.y);

            // Set movement vector if on ground
            movement *= playerData->Speed;

            // Sprint
            if (inputData.Sprint)
            {
                movement *= playerData->SprintSpeedMultiplier;
            }

            // Jump
            if (inputData.Jump && canJump)
            {
                Jump(&playerData->Velocity, playerData->JumpForce);
            }
        }

        if (attackIntervalTimer < attackIntervalTime)
        {
            movement = Vector3.zero;
        }

        // Update respective processor's Position variable to equal desired position
        // Transform the direction based on the camera anchor so that forward = viewport forward
        processor.Move(camAnchor.TransformDirection(movement));
    }

    void Jump(Vector3* velocity, float jumpForce, bool cancelVerticalVelocity = true)
    {
        grounded = false;
        groundedTimer = 0;

        if (cancelVerticalVelocity)
        {
            *velocity = new(
                (*velocity).x,
                jumpForce,
                (*velocity).z
            );
        }
        else
        {
            *velocity = *velocity + Vector3.up * jumpForce;
        }
    }

    void Look(InputData inputData, PlayerData* playerData)
    {
        // Apply inputData.LookDelta considering sensitivity and delta time
        // Use Time.deltaTime instead of Time.fixedDeltaTime here because Look() is called in LateUpdate()
        Vector2 adjustedLookDelta = new(inputData.LookDelta.x, -inputData.LookDelta.y);
        lookAngles += playerData->LookSensitivity * Time.deltaTime * adjustedLookDelta;
        lookAngles.y = Mathf.Clamp(lookAngles.y, minYLookAngle, maxYLookAngle);

        float yDist = Mathf.Sin(Mathf.Deg2Rad * lookAngles.y);
        float zDist = Mathf.Cos(Mathf.Deg2Rad * lookAngles.y);

        Vector3 cameraPosition = new(
            0,
            yDist,
            -zDist
        );
        // If the vector between the anchor and handle is interrupted
        if (Physics.Raycast(camAnchor.position, -camHandle.forward, out RaycastHit hit, maxCamDistance + camClipDistance))
            cameraPosition *= hit.distance - camClipDistance;
        else
            cameraPosition *= maxCamDistance;

        camHandle.localPosition = cameraPosition;

        camAnchor.rotation = Quaternion.Euler(0, lookAngles.x, 0);
        model.rotation = camAnchor.rotation;
        camHandle.localRotation = Quaternion.Euler(lookAngles.y, 0, 0);
    }

    void OnAttack()
    {
        attackIntervalTimer = 0;
    }
}