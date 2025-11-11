using UnityEngine;

/// <summary>
/// Controller for player movement and mouse look
/// </summary>
public class PlayerMovementController : MonoBehaviour
{
    private const float MOVEMENT_DEADZONE = 0.0001f;
    private const float GROUNDED_VELOCITY_Y = -2f;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Mouse Look")]
    [Tooltip("Head pivot: rotates on X axis (pitch)")]
    [SerializeField] private Transform headPivot;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxPitch = 75f;
    [SerializeField] private float minPitch = -75f;

    private CharacterController characterController;
    private float pitch = 0f;
    private Vector3 velocity;

    private void Start()
    {
        InitializeCharacterController();
        InitializeCursor();
        ValidateConfiguration();
    }

    private void InitializeCharacterController()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("PlayerMovementController: CharacterController component is required!");
        }
    }

    private void InitializeCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ValidateConfiguration()
    {
        if (headPivot == null)
        {
            Debug.LogError("PlayerMovementController: headPivot is not assigned!");
        }
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        if (headPivot == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Yaw: rotate player horizontally
        transform.Rotate(Vector3.up * mouseX);

        // Pitch: rotate head pivot vertically and clamp
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        headPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        if (characterController == null) return;

        Vector3 movement = CalculateMovement();
        ApplyGravity();
        ApplyMovement(movement);
    }

    private Vector3 CalculateMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 desiredDirection = forward * vertical + right * horizontal;
        desiredDirection.y = 0f;

        if (desiredDirection.sqrMagnitude < MOVEMENT_DEADZONE)
        {
            return Vector3.zero;
        }

        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        return desiredDirection.normalized * speed;
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && velocity.y < 0f)
        {
            velocity.y = GROUNDED_VELOCITY_Y;
        }
        velocity.y += gravity * Time.deltaTime;
    }

    private void ApplyMovement(Vector3 movement)
    {
        if (characterController == null) return;

        characterController.Move(movement * Time.deltaTime);
        characterController.Move(new Vector3(0f, velocity.y, 0f) * Time.deltaTime);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
