using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionController : MonoBehaviour
{
    public event Action<bool> HoverChanged;
    public event Action<string> HoverTextChanged;

    public bool IsHoveringInteractable { get; private set; }
    public string CurrentHoverText { get; private set; } = string.Empty;

    [Header("Raycast")]
    [SerializeField] private Camera cam;
    [SerializeField] private float interactDistance = 2.5f;
    [SerializeField] private LayerMask interactMask = ~0;

    private ScenarioInteractableBase _currentInteractable;
    private bool _interactionEnabled = true;

    private void Reset()
    {
        cam = Camera.main;
    }

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;
    }

    public void SetInteractionEnabled(bool enabled)
    {
        _interactionEnabled = enabled;

        if (!enabled)
            ClearHover();
    }

    private void Update()
    {
        UpdateHover();

        if (!_interactionEnabled || Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame && _currentInteractable != null)
        {
            _currentInteractable.Interact(this);
        }
    }

    private void UpdateHover()
    {
        if (!_interactionEnabled || cam == null)
        {
            ClearHover();
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            ScenarioInteractableBase interactable = hit.collider.GetComponentInParent<ScenarioInteractableBase>();

            if (interactable != null)
            {
                string hoverText = interactable.GetHoverText();

                if (!string.IsNullOrWhiteSpace(hoverText))
                {
                    _currentInteractable = interactable;
                    SetHoverState(true, hoverText);
                    return;
                }
            }
        }

        ClearHover();
    }

    private void ClearHover()
    {
        _currentInteractable = null;
        SetHoverState(false, string.Empty);
    }

    private void SetHoverState(bool hovering, string hoverText)
    {
        if (IsHoveringInteractable != hovering)
        {
            IsHoveringInteractable = hovering;
            HoverChanged?.Invoke(hovering);
        }

        if (CurrentHoverText != hoverText)
        {
            CurrentHoverText = hoverText;
            HoverTextChanged?.Invoke(CurrentHoverText);
        }
    }
}