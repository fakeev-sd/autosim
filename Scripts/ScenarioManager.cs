using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenarioManager : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private string scenarioCode = "C1";
    [SerializeField] private string hubSceneName = "Hub";
    [SerializeField] private List<ScenarioStep> steps = new List<ScenarioStep>();

    [Header("HUD tasks / учебные пункты")]
    [SerializeField] private List<ScenarioHudTask> hudTasks = new List<ScenarioHudTask>();

    [Header("Grade: time + errors")]
    [SerializeField] private int grade5MaxSeconds = 180;
    [SerializeField] private int grade5MaxErrors = 1;
    [SerializeField] private int grade4MaxSeconds = 240;
    [SerializeField] private int grade4MaxErrors = 3;
    [SerializeField] private int grade3MaxSeconds = 360;
    [SerializeField] private int grade3MaxErrors = 5;

    [Header("References")]
    [SerializeField] private SimpleFpsPlayer player;
    [SerializeField] private ScenarioHudController hud;
    [SerializeField] private DatabaseManager database;

    private int currentStepIndex;
    private int errors;
    private float elapsedSeconds;
    private bool finished;
    private bool inPov;
    private bool switchingPov;
    private bool uiBlocked;
    private GrabbableTool heldTool;
    private PovPoint activePov;

    private Vector3 savedPlayerPosition;
    private Quaternion savedPlayerRotation;
    private Quaternion savedCameraRotation;

    public bool CanInteract
    {
        get { return !finished && !switchingPov && !uiBlocked; }
    }

    public bool HasHeldTool
    {
        get { return heldTool != null; }
    }

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<SimpleFpsPlayer>();

        if (hud == null)
            hud = FindFirstObjectByType<ScenarioHudController>();

        if (database == null)
            database = DatabaseManager.Instance;
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(AppState.CurrentScenarioCode))
            scenarioCode = AppState.CurrentScenarioCode;

        ResetRuntimeState();
    }

    private void Update()
    {
        if (finished)
            return;

        elapsedSeconds += Time.deltaTime;

        if (hud != null)
            hud.SetStats(Mathf.FloorToInt(elapsedSeconds), errors);
    }

    private void ResetRuntimeState()
    {
        currentStepIndex = 0;
        errors = 0;
        elapsedSeconds = 0f;
        finished = false;
        inPov = false;
        switchingPov = false;
        uiBlocked = false;
        heldTool = null;
        activePov = null;

        if (player != null)
        {
            player.SetMovementEnabled(true);
            player.SetInputEnabled(true);
        }

        if (hud != null)
        {
            hud.SetHeldTool(null, null);
            hud.SetStats(0, 0);
            hud.SetTasks(hudTasks, steps, currentStepIndex);
            hud.SetHint("");
            hud.SetFade(0f);
            hud.HideMeasurement();
            hud.HideWiringDiagram();
            hud.HideResult();
        }
    }

    public void TryTakeTool(GrabbableTool tool)
    {
        if (tool == null || heldTool != null)
            return;

        heldTool = tool;
        heldTool.TakeToHands();

        if (hud != null)
            hud.SetHeldTool(heldTool.displayName, heldTool.icon);
    }

    public void DropHeldTool()
    {
        if (heldTool == null)
            return;

        heldTool.ReturnToWorld();
        heldTool = null;

        if (hud != null)
            hud.SetHeldTool(null, null);
    }

    public void TryUseTarget(ScenarioTarget clickedTarget)
    {
        if (finished || steps.Count == 0 || currentStepIndex >= steps.Count)
            return;

        ScenarioStep step = steps[currentStepIndex];

        if (clickedTarget != step.target)
        {
            AddError();
            return;
        }

        if (step.requiredTool != null && heldTool != step.requiredTool)
        {
            AddError();
            return;
        }

        CompleteStep(step);
    }

    private void CompleteStep(ScenarioStep step)
    {
        SetObjectsActive(step.hideObjects, false);
        SetObjectsActive(step.showObjects, true);

        if (step.showMeasurement && hud != null)
            hud.ShowMeasurement(step.measurementTitle, step.measurementValue, step.measurementSeconds);

        if (step.showWiringDiagram && hud != null)
            OpenWiringDiagram(step);

        currentStepIndex++;

        if (currentStepIndex >= steps.Count)
        {
            FinishScenario();
            return;
        }

        if (hud != null)
            hud.SetTasks(hudTasks, steps, currentStepIndex);
    }


    private void OpenWiringDiagram(ScenarioStep step)
    {
        uiBlocked = true;

        if (player != null)
            player.SetInputEnabled(false);

        if (hud != null)
        {
            hud.SetHint("");
            hud.ShowWiringDiagram(step.wiringDiagramTitle, step.wiringDiagramNote, step.wiringDiagramImage, OnWiringDiagramClosed);
        }
    }

    private void OnWiringDiagramClosed()
    {
        uiBlocked = false;

        if (player != null && !finished)
            player.SetInputEnabled(true);
    }

    private void AddError()
    {
        errors++;

        if (hud != null)
            hud.SetStats(Mathf.FloorToInt(elapsedSeconds), errors);
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }

    private void FinishScenario()
    {
        finished = true;

        if (player != null)
            player.SetInputEnabled(false);

        int duration = Mathf.CeilToInt(elapsedSeconds);
        int grade = CalculateGrade(duration, errors);

        bool saved = false;
        string saveMessage = "";

        if (database != null && AppState.IsLoggedIn)
            saved = database.SaveAttempt(AppState.UserId, scenarioCode, duration, grade, out saveMessage);
        else
            saveMessage = "Нет активного пользователя или DatabaseManager.";

        if (hud != null)
            hud.ShowResult(duration, errors, grade, saved, saveMessage, ReturnToHub);
    }

    private int CalculateGrade(int duration, int errorsCount)
    {
        if (duration <= grade5MaxSeconds && errorsCount <= grade5MaxErrors)
            return 5;

        if (duration <= grade4MaxSeconds && errorsCount <= grade4MaxErrors)
            return 4;

        if (duration <= grade3MaxSeconds && errorsCount <= grade3MaxErrors)
            return 3;

        return 2;
    }

    public void EnterPov(PovPoint pov)
    {
        if (pov == null || pov.cameraPose == null || inPov || switchingPov)
            return;

        StartCoroutine(EnterPovRoutine(pov));
    }

    private IEnumerator EnterPovRoutine(PovPoint pov)
    {
        switchingPov = true;

        savedPlayerPosition = player.PlayerPosition;
        savedPlayerRotation = player.PlayerRotation;
        savedCameraRotation = player.CameraLocalRotation;

        if (hud != null)
            yield return hud.FadeTo(1f, 0.22f);

        ApplyPovObjects(pov, true);

        player.TeleportTo(pov.cameraPose);
        player.SetMovementEnabled(false);
        activePov = pov;
        inPov = true;

        if (hud != null)
            yield return hud.FadeTo(0f, 0.22f);

        switchingPov = false;
    }

    public void HandleEsc()
    {
        if (inPov && !switchingPov)
            StartCoroutine(ExitPovRoutine());
    }

    private IEnumerator ExitPovRoutine()
    {
        switchingPov = true;

        if (hud != null)
            yield return hud.FadeTo(1f, 0.22f);

        if (activePov != null)
            ApplyPovObjects(activePov, false);

        player.TeleportToPose(savedPlayerPosition, savedPlayerRotation, savedCameraRotation);
        player.SetMovementEnabled(true);
        activePov = null;
        inPov = false;

        if (hud != null)
            yield return hud.FadeTo(0f, 0.22f);

        switchingPov = false;
    }

    private void ApplyPovObjects(PovPoint pov, bool entering)
    {
        if (pov == null)
            return;

        // При входе: show = true, hide = false.
        // При выходе: show = false, hide = true.
        SetObjectsActive(pov.showObjectsOnEnter, entering);
        SetObjectsActive(pov.hideObjectsOnEnter, !entering);
    }

    public void SetHint(string text)
    {
        if (hud != null)
            hud.SetHint(text);
    }

    private void ReturnToHub()
    {
        SceneManager.LoadScene(hubSceneName);
    }
}

[Serializable]
public class ScenarioStep
{
    [Tooltip("Техническое название шага. В HUD не выводится, нужно только для удобства в инспекторе.")]
    public string title;

    public ScenarioTarget target;
    public GrabbableTool requiredTool;
    public GameObject[] showObjects;
    public GameObject[] hideObjects;

    [Header("Measurement UI / показ измерения")]
    [Tooltip("Если включено, после выполнения этого шага справа снизу появится блок измерения.")]
    public bool showMeasurement;

    [Tooltip("Текст сверху. Например: Напряжение АКБ")]
    public string measurementTitle;

    [Tooltip("Цифры/значение снизу. Например: 12.4 V")]
    public string measurementValue;

    [Min(0.1f)]
    [Tooltip("Сколько секунд показывать измерение.")]
    public float measurementSeconds = 7f;

    [Header("Wiring Diagram UI / схема электроцепи")]
    [Tooltip("Если включено, после выполнения этого шага откроется информационная схема цепи стеклоочистителей.")]
    public bool showWiringDiagram;

    [Tooltip("Заголовок окна схемы.")]
    public string wiringDiagramTitle = "Схема цепи стеклоочистителей";

    [TextArea(2, 4)]
    [Tooltip("Короткое пояснение под схемой.")]
    public string wiringDiagramNote = "По схеме видно, что питание проходит через предохранитель и переключатель к фишке мотора стеклоочистителей. Если на фишке нет нормального напряжения, проверьте участок проводки перед мотором.";

    [Tooltip("Картинка схемы. Перетащите сюда PNG/JPG с электрической схемой. Если не назначить, будет показана заглушка.")]
    public Texture2D wiringDiagramImage;
}

[Serializable]
public class ScenarioHudTask
{
    [Tooltip("Учебная формулировка, которая будет видна в HUD.")]
    public string title;

    [Min(1)]
    [Tooltip("С какого технического шага начинается этот HUD-пункт. Нумерация человеческая: 1, 2, 3, а не 0, 1, 2.")]
    public int startStepNumber = 1;
}
