using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class SimpleFpsPlayer : MonoBehaviour
{
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private float moveSpeed = 3.0f;
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -75f;
    [SerializeField] private float maxPitch = 75f;
    [SerializeField] private float gravity = -18f;

    private CharacterController controller;
    private float yaw;
    private float pitch;
    private float verticalVelocity;
    private bool inputEnabled = true;
    private bool movementEnabled = true;

    public Vector3 PlayerPosition { get { return transform.position; } }
    public Quaternion PlayerRotation { get { return transform.rotation; } }
    public Quaternion CameraLocalRotation { get { return cameraRoot.localRotation; } }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraRoot == null && Camera.main != null)
            cameraRoot = Camera.main.transform;

        yaw = transform.eulerAngles.y;
        pitch = NormalizeAngle(cameraRoot.localEulerAngles.x);
    }


    private void Start()
    {
        SetInputEnabled(true);
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (!inputEnabled)
            return;

        HandleLook();

        if (movementEnabled)
            HandleMovement();
#else
        Debug.LogError("В проекте не включён New Input System. Проверьте Project Settings > Player > Active Input Handling.");
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void HandleLook()
    {
        if (Mouse.current == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        yaw += delta.x * mouseSensitivity;
        pitch -= delta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        if (Keyboard.current == null)
            return;

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector3 move = transform.forward * input.y + transform.right * input.x;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * moveSpeed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }
#endif

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (enabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        verticalVelocity = 0f;
    }

    public void TeleportTo(Transform pose)
    {
        if (pose == null)
            return;

        TeleportToPose(pose.position, Quaternion.Euler(0f, pose.eulerAngles.y, 0f), Quaternion.Euler(NormalizeAngle(pose.eulerAngles.x), 0f, 0f));
    }

    public void TeleportToPose(Vector3 position, Quaternion bodyRotation, Quaternion cameraRotation)
    {
        bool wasEnabled = controller.enabled;
        controller.enabled = false;

        transform.position = position;
        transform.rotation = bodyRotation;
        cameraRoot.localRotation = cameraRotation;

        yaw = transform.eulerAngles.y;
        pitch = NormalizeAngle(cameraRoot.localEulerAngles.x);

        controller.enabled = wasEnabled;
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}
