using UnityEngine;

public unsafe class PlayerController : MonoBehaviour
{
    public PlayerProcessor processor;

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
    }

    void Update()
    {
        fixed (PlayerData* playerData = &processor.playerData)
        {
            UpdatePosition(playerData);
        }
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
        //transform.position = playerData->Position;
        transform.position = Vector3.Lerp(transform.position, playerData->Position, GameManager.InterpolationConstant);
        Debug.Log(transform.position);
        Debug.Log(playerData->Position);
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
        if (grounded)
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

            // Rotate movement vector to face in the camera direction
            movement = camAnchor.rotation * movement;

            // Jump
            if (inputData.Jump)
            {
                Jump(&playerData->Velocity, playerData->JumpForce);
            }
        }

        // Update respective processor's Position variable to equal desired position
        movement = transform.TransformDirection(movement);
        processor.Move(movement);
    }

    void Jump(Vector3* velocity, float jumpForce, bool cancelVerticalVelocity = true)
    {
        grounded = false;

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
        camHandle.localRotation = Quaternion.Euler(lookAngles.y, 0, 0);
    }
}