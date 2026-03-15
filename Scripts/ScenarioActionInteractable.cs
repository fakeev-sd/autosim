using UnityEngine;

public class ScenarioActionInteractable : ScenarioInteractableBase
{
    public enum ActionType
    {
        EnterUnderHood,
        BatteryMinusTerminal,
        BatteryPlusTerminal,
        BatteryPostsHotspot,
        BatteryMinusPostHotspot,
        BatteryPlusPostHotspot,
        BodyGroundPoint,
        BrokenGroundWire
    }

    [SerializeField] private ActionType actionType;

    public ActionType Type => actionType;

    public override string GetHoverText()
    {
        if (scenario == null) return string.Empty;
        return scenario.GetHoverTextForAction(actionType);
    }

    public override void Interact(InteractionController controller)
    {
        scenario?.HandleAction(actionType);
    }
}