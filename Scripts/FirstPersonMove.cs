using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonMove : MonoBehaviour
{
    [SerializeField] private float speed = 4.5f;
    [SerializeField] private float gravity = -20f;

    private CharacterController _cc;
    private float _yVel;
    private bool _movementEnabled = true;

    public bool MovementEnabled => _movementEnabled;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    public void SetMovementEnabled(bool enabled)
    {
        _movementEnabled = enabled;

        if (!enabled)
            _yVel = 0f;
    }

    private void Update()
    {
        if (!_movementEnabled)
            return;

        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        }

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 move = (transform.forward * input.y + transform.right * input.x) * speed;

        if (_cc.isGrounded && _yVel < 0f)
            _yVel = -2f;

        _yVel += gravity * Time.deltaTime;
        move.y = _yVel;

        _cc.Move(move * Time.deltaTime);
    }
}