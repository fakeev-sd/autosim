using UnityEngine;

public abstract class ScenarioInteractableBase : MonoBehaviour
{
    [SerializeField] protected AKBMassScenarioController scenario;

    protected virtual void Awake()
    {
        if (scenario == null)
            scenario = FindAnyObjectByType<AKBMassScenarioController>();
    }

    public abstract string GetHoverText();
    public abstract void Interact(InteractionController controller);
}