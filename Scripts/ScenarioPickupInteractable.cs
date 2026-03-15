using UnityEngine;

public class ScenarioPickupInteractable : ScenarioInteractableBase
{
    [SerializeField] private AKBMassScenarioController.ScenarioPickupType pickupType;
    [SerializeField] private string hoverText = "Взять";

    private Transform _originalParent;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;

    public AKBMassScenarioController.ScenarioPickupType PickupType => pickupType;

    protected override void Awake()
    {
        base.Awake();

        _originalParent = transform.parent;
        _originalPosition = transform.position;
        _originalRotation = transform.rotation;
    }

    public override string GetHoverText()
    {
        if (scenario == null) return string.Empty;
        return scenario.CanPickUp(this) ? hoverText : string.Empty;
    }

    public override void Interact(InteractionController controller)
    {
        scenario?.TryPickUp(this);
    }

    public void TakeIntoHand()
    {
        gameObject.SetActive(false);
    }

    public void ReturnToOrigin()
    {
        transform.SetParent(_originalParent, true);
        transform.SetPositionAndRotation(_originalPosition, _originalRotation);
        gameObject.SetActive(true);
    }

    public void PlaceAt(Transform target)
    {
        if (target == null) return;

        transform.SetParent(target, true);
        transform.SetPositionAndRotation(target.position, target.rotation);
        gameObject.SetActive(true);
    }
}