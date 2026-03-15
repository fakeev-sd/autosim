using UnityEngine;

public class ToggleInteractable : MonoBehaviour
{
    [Header("Progress")]
    [SerializeField] private string stepIdOnToggleOn;

    [Header("Visual")]
    [SerializeField] private Transform movingPart;
    [SerializeField] private Vector3 onLocalEuler;
    [SerializeField] private Vector3 offLocalEuler;
    [SerializeField] private bool startOn;

    private bool _isOn;

    private void Awake()
    {
        _isOn = startOn;
        ApplyVisual();
    }

    public void Toggle()
    {
        _isOn = !_isOn;
        ApplyVisual();

        if (_isOn && !string.IsNullOrWhiteSpace(stepIdOnToggleOn))
            ProgressTracker.Instance?.TryCompleteStep(stepIdOnToggleOn);
    }

    private void ApplyVisual()
    {
        if (movingPart == null) return;
        movingPart.localEulerAngles = _isOn ? onLocalEuler : offLocalEuler;
    }
}
