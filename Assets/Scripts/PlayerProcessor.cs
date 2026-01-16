using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerProcessor : MonoBehaviour
{
    InputActions input;

    InputAction a_move;
    InputAction a_look;
    InputAction a_jump;
    InputAction a_attack;
    InputAction a_interact;
    InputAction a_sprint;

    public InputData inputData;

    public PlayerData playerData;
    public float speed = 5;
    public float jumpForce = 5;
    public float sprintSpeedMultiplier = 2;
    public float lookSensitivity = 25;

    void Awake()
    {
        input = new();
    }

    void OnEnable()
    {
        input.Enable();
    }

    void OnDisable()
    {
        input.Disable();
    }

    void Start()
    {
        // Resolve the references to various InputActions
        a_move = input.Player.Move;
        a_look = input.Player.Look;
        a_jump = input.Player.Jump;
        a_attack = input.Player.Attack;
        a_interact = input.Player.Interact;
        a_sprint = input.Player.Sprint;

        inputData = new();

        // Must be updated if another inspector field is added
        playerData = new()
        {
            Speed = speed,
            JumpForce = jumpForce,
            SprintSpeedMultiplier = sprintSpeedMultiplier,
            LookSensitivity = lookSensitivity
        };
    }

    void Update()
    {
        GatherInput();
    }

    void GatherInput()
    {
        inputData.MoveDelta = a_move.ReadValue<Vector2>();
        inputData.LookDelta = a_look.ReadValue<Vector2>();
        inputData.Jump = a_jump.IsPressed();
        inputData.Attack = a_attack.IsPressed();
        inputData.Interact = a_interact.IsPressed();
        inputData.Sprint = a_sprint.IsPressed();
    }

    // Modifies Position by movementVector, taking into account the time since last update
    public void Move(Vector3 movementVector, bool fixedTime = true)
    {
        if (fixedTime)
        {
            playerData.Position += movementVector * Time.fixedDeltaTime;
        }
        else
        {
            playerData.Position += movementVector * Time.deltaTime;
        }
    }

    // Modifies Velocity by velocityVector, taking into account the time since last update
    public void Accelerate(Vector3 velocityVector, bool fixedTime = true)
    {
        if (fixedTime)
        {
            playerData.Velocity += velocityVector * Time.fixedDeltaTime;
        }
        else
        {
            playerData.Velocity += velocityVector * Time.deltaTime;
        }
    }

    public void SimulatePhysics(bool grounded)
    {
        Accelerate(playerData.Acceleration);
        SimulateGravity(grounded);
        Move(playerData.Velocity);
    }

    void SimulateGravity(bool grounded)
    {
        Accelerate(GameManager.Gravity);
        if (grounded)
        {
            playerData.Velocity = new(
                playerData.Velocity.x,
                0,
                playerData.Velocity.z
            );
        }
    }
}

public struct InputData
{
    public Vector2 MoveDelta;
    public Vector2 LookDelta;
    public bool Jump;
    public bool Attack;
    public bool Interact;
    public bool Sprint;
}

public struct PlayerData
{
    public Vector3 Position;
    public Vector3 Velocity;
    public Vector3 Acceleration;
    public Quaternion Rotation;
    public float Speed;
    public float JumpForce;
    public float SprintSpeedMultiplier;
    public float LookSensitivity;
}