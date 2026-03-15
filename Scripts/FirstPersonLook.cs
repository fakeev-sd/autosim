using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonLook : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float sensitivity = 0.12f;
    [SerializeField] private float pitchClamp = 80f;
    [SerializeField] private bool lockCursorOnEnable = true;

    private float _pitch;
    private bool _lookEnabled = true;

    public bool LookEnabled => _lookEnabled;

    private void Reset()
    {
        cameraTransform = Camera.main != null ? Camera.main.transform : null;
    }

    private void OnEnable()
    {
        if (lockCursorOnEnable)
            SetCursorLocked(true);
    }

    public void SetLookEnabled(bool enabled)
    {
        _lookEnabled = enabled;
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    public void AlignToWorldRotation(Quaternion worldRotation)
    {
        Vector3 euler = worldRotation.eulerAngles;

        transform.rotation = Quaternion.Euler(0f, euler.y, 0f);

        _pitch = NormalizePitch(euler.x);
        _pitch = Mathf.Clamp(_pitch, -pitchClamp, pitchClamp);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void Update()
    {
        if (!_lookEnabled || Mouse.current == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue() * sensitivity;

        transform.Rotate(Vector3.up, delta.x, Space.World);

        _pitch -= delta.y;
        _pitch = Mathf.Clamp(_pitch, -pitchClamp, pitchClamp);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private float NormalizePitch(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}