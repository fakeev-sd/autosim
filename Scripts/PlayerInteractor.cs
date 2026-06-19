using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private ScenarioManager scenarioManager;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionMask = ~0;

    private HighlightObject currentHighlight;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (scenarioManager == null)
            scenarioManager = FindFirstObjectByType<ScenarioManager>();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (scenarioManager == null || !scenarioManager.CanInteract)
        {
            SetHover(null);
            return;
        }

        if (Mouse.current == null || Keyboard.current == null)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
            scenarioManager.DropHeldTool();

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            scenarioManager.HandleEsc();

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactionMask, QueryTriggerInteraction.Collide))
        {
            Collider col = hit.collider;
            SetHover(col.GetComponentInParent<HighlightObject>());
            scenarioManager.SetHint(GetHint(col));

            if (Mouse.current.leftButton.wasPressedThisFrame)
                HandleLeftClick(col);
        }
        else
        {
            SetHover(null);
            scenarioManager.SetHint("");
            // Клик в пустоту не считается ошибкой.
        }
#else
        Debug.LogError("В проекте не включён New Input System. Проверьте Project Settings > Player > Active Input Handling.");
#endif
    }

    private string GetHint(Collider col)
    {
        GrabbableTool tool = col.GetComponentInParent<GrabbableTool>();
        if (tool != null)
        {
            if (scenarioManager.HasHeldTool)
                return "Сначала уберите предмет из рук: ПКМ";

            return "ЛКМ — взять: " + tool.displayName;
        }

        ScenarioTarget target = col.GetComponentInParent<ScenarioTarget>();
        if (target != null)
            return "ЛКМ — взаимодействовать: " + target.displayName;

        PovPoint pov = col.GetComponentInParent<PovPoint>();
        if (pov != null)
            return "ЛКМ — перейти: " + pov.displayName;

        return "";
    }

    private void HandleLeftClick(Collider col)
    {
        GrabbableTool tool = col.GetComponentInParent<GrabbableTool>();
        if (tool != null)
        {
            scenarioManager.TryTakeTool(tool);
            return;
        }

        ScenarioTarget target = col.GetComponentInParent<ScenarioTarget>();
        if (target != null)
        {
            scenarioManager.TryUseTarget(target);
            return;
        }

        PovPoint pov = col.GetComponentInParent<PovPoint>();
        if (pov != null)
        {
            scenarioManager.EnterPov(pov);
            return;
        }
    }

    private void SetHover(HighlightObject highlight)
    {
        if (currentHighlight == highlight)
            return;

        if (currentHighlight != null)
            currentHighlight.SetHover(false);

        currentHighlight = highlight;

        if (currentHighlight != null)
            currentHighlight.SetHover(true);
    }
}
