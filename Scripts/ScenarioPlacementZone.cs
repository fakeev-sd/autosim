using UnityEngine;

public class ScenarioPlacementZone : ScenarioInteractableBase
{
    [SerializeField] private Transform snapPoint;

    private bool _occupied;
    private ScenarioPickupInteractable _placedPickup;

    public bool IsOccupied => _occupied;
    public Transform SnapPoint => snapPoint != null ? snapPoint : transform;

    public override string GetHoverText()
    {
        if (scenario == null) return string.Empty;
        return scenario.CanPlaceCurrentChockHere(this) ? "Установить" : string.Empty;
    }

    public override void Interact(InteractionController controller)
    {
        scenario?.TryPlaceCurrentChock(this);
    }

    public void Occupy(ScenarioPickupInteractable pickup)
    {
        _occupied = true;
        _placedPickup = pickup;
    }

    public void ResetState()
    {
        _occupied = false;
        _placedPickup = null;
    }
}