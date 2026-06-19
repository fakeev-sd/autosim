using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ScenarioHudController : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    [Header("Measurement UI / измерения")]
    [SerializeField] private Font measurementFont;

    [Header("Font Sizes / размеры шрифта")]
    [SerializeField] private int previousTaskFontSize = 16;
    [SerializeField] private int currentTaskFontSize = 16;
    [SerializeField] private int nextTaskFontSize = 13;
    [SerializeField] private int statsFontSize = 17;
    [SerializeField] private int heldToolFontSize = 13;
    [SerializeField] private int hintFontSize = 22;
    [SerializeField] private int measurementTitleFontSize = 12;
    [SerializeField] private int measurementValueFontSize = 30;
    [SerializeField] private int wiringDiagramTitleFontSize = 22;
    [SerializeField] private int wiringDiagramNoteFontSize = 14;
    [SerializeField] private int resultTitleFontSize = 19;
    [SerializeField] private int resultTextFontSize = 14;
    [SerializeField] private int resultSaveFontSize = 12;

    private VisualElement root;
    private Label previousStepLabel;
    private Label currentStepLabel;
    private Label nextStepLabel;
    private Label timeLabel;
    private Label errorsLabel;
    private Label heldToolLabel;
    private Image heldToolIconImage;
    private Label hintLabel;
    private VisualElement measurementPanel;
    private Label measurementTitleLabel;
    private Label measurementValueLabel;
    private Coroutine measurementRoutine;
    private VisualElement wiringDiagramOverlay;
    private Label wiringDiagramTitleLabel;
    private Label wiringDiagramNoteLabel;
    private Image wiringDiagramImageElement;
    private Button wiringDiagramCloseButton;
    private Action wiringDiagramCloseCallback;
    private VisualElement fadeOverlay;
    private VisualElement resultOverlay;
    private Label resultTitleLabel;
    private Label resultTimeLabel;
    private Label resultErrorsLabel;
    private Label resultGradeLabel;
    private Label resultSaveLabel;
    private Button returnButton;
    private Action returnCallback;
    private float fadeAlpha;

    private readonly Color panelColor = new Color(0.08f, 0.08f, 0.10f, 0.72f);
    private readonly Color borderColor = new Color(0.22f, 0.22f, 0.26f, 1f);
    private readonly Color whiteColor = new Color(0.92f, 0.92f, 0.96f, 1f);
    private readonly Color grayColor = new Color(0.58f, 0.58f, 0.64f, 1f);
    private readonly Color greenColor = new Color(0.32f, 0.86f, 0.46f, 1f);
    private readonly Color measurementRedColor = new Color(1f, 0.08f, 0.04f, 1f);
    private readonly Color measurementPanelColor = new Color(0.03f, 0.015f, 0.015f, 0.82f);

    private void Awake()
    {
        if (document == null)
            document = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        BuildUi();
    }

    private void OnDisable()
    {
        if (returnButton != null)
            returnButton.clicked -= OnReturnButtonClicked;

        if (wiringDiagramCloseButton != null)
            wiringDiagramCloseButton.clicked -= OnWiringDiagramCloseClicked;
    }

    private void BuildUi()
    {
        if (document == null)
            return;

        root = document.rootVisualElement;
        root.Clear();
        root.style.flexGrow = 1;

        CreateStepsPanel();
        CreateStatsPanel();
        CreateHeldToolPanel();
        CreateMeasurementPanel();
        CreateCrosshairAndHint();
        CreateWiringDiagramPanel();
        CreateResultOverlay();
        CreateFadeOverlay();
    }

    private void CreateStepsPanel()
    {
        VisualElement panel = CreatePanel();
        panel.style.position = Position.Absolute;
        panel.style.left = 20;
        panel.style.top = 20;
        panel.style.width = 430;
        panel.style.paddingTop = 12;
        panel.style.paddingBottom = 12;
        panel.style.paddingLeft = 14;
        panel.style.paddingRight = 14;

        previousStepLabel = CreateStepLabel(previousTaskFontSize, greenColor);
        previousStepLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        currentStepLabel = CreateStepLabel(currentTaskFontSize, whiteColor);
        currentStepLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        nextStepLabel = CreateStepLabel(nextTaskFontSize, grayColor);

        panel.Add(previousStepLabel);
        panel.Add(currentStepLabel);
        panel.Add(nextStepLabel);
        root.Add(panel);
    }

    private void CreateStatsPanel()
    {
        VisualElement panel = CreatePanel();
        panel.style.position = Position.Absolute;
        panel.style.right = 20;
        panel.style.top = 20;
        panel.style.width = 190;
        panel.style.paddingTop = 12;
        panel.style.paddingBottom = 12;
        panel.style.paddingLeft = 14;
        panel.style.paddingRight = 14;

        timeLabel = CreateLabel("Время: 00:00", statsFontSize, whiteColor);
        timeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        errorsLabel = CreateLabel("Ошибки: 0", statsFontSize, whiteColor);
        errorsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        errorsLabel.style.marginTop = 4;

        panel.Add(timeLabel);
        panel.Add(errorsLabel);
        root.Add(panel);
    }

    private void CreateHeldToolPanel()
    {
        VisualElement panel = CreatePanel();
        panel.style.position = Position.Absolute;
        panel.style.left = 20;
        panel.style.bottom = 20;
        panel.style.width = 140;
        panel.style.paddingTop = 12;
        panel.style.paddingBottom = 12;
        panel.style.paddingLeft = 10;
        panel.style.paddingRight = 10;
        panel.style.flexDirection = FlexDirection.Column;
        panel.style.alignItems = Align.Center;
        panel.style.justifyContent = Justify.Center;

        heldToolIconImage = new Image();
        heldToolIconImage.scaleMode = ScaleMode.ScaleToFit;
        heldToolIconImage.style.width = 76;
        heldToolIconImage.style.height = 76;
        heldToolIconImage.style.marginBottom = 6;
        heldToolIconImage.style.display = DisplayStyle.None;
        heldToolIconImage.pickingMode = PickingMode.Ignore;

        heldToolLabel = CreateLabel("Ничего", heldToolFontSize, whiteColor);
        heldToolLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        heldToolLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        heldToolLabel.style.whiteSpace = WhiteSpace.Normal;

        panel.Add(heldToolIconImage);
        panel.Add(heldToolLabel);
        root.Add(panel);
    }

    private void CreateMeasurementPanel()
    {
        measurementPanel = new VisualElement();
        measurementPanel.style.position = Position.Absolute;
        measurementPanel.style.right = 20;
        measurementPanel.style.bottom = 20;
        measurementPanel.style.minWidth = 220;
        measurementPanel.style.paddingTop = 12;
        measurementPanel.style.paddingBottom = 12;
        measurementPanel.style.paddingLeft = 16;
        measurementPanel.style.paddingRight = 16;
        measurementPanel.style.backgroundColor = measurementPanelColor;
        measurementPanel.style.borderTopLeftRadius = 12;
        measurementPanel.style.borderTopRightRadius = 12;
        measurementPanel.style.borderBottomLeftRadius = 12;
        measurementPanel.style.borderBottomRightRadius = 12;
        measurementPanel.style.borderTopWidth = 2;
        measurementPanel.style.borderRightWidth = 2;
        measurementPanel.style.borderBottomWidth = 2;
        measurementPanel.style.borderLeftWidth = 2;
        measurementPanel.style.borderTopColor = measurementRedColor;
        measurementPanel.style.borderRightColor = measurementRedColor;
        measurementPanel.style.borderBottomColor = measurementRedColor;
        measurementPanel.style.borderLeftColor = measurementRedColor;
        measurementPanel.style.display = DisplayStyle.None;
        measurementPanel.pickingMode = PickingMode.Ignore;

        measurementTitleLabel = CreateLabel("", measurementTitleFontSize, measurementRedColor);
        measurementTitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        measurementTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        measurementValueLabel = CreateLabel("", measurementValueFontSize, measurementRedColor);
        measurementValueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        measurementValueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        measurementValueLabel.style.marginTop = 2;

        if (measurementFont != null)
        {
            measurementTitleLabel.style.unityFont = measurementFont;
            measurementValueLabel.style.unityFont = measurementFont;
        }

        measurementPanel.Add(measurementTitleLabel);
        measurementPanel.Add(measurementValueLabel);
        root.Add(measurementPanel);
    }

    private void CreateCrosshairAndHint()
    {
        Label crosshair = CreateLabel("•", 30, whiteColor);
        crosshair.style.position = Position.Absolute;
        crosshair.style.left = Length.Percent(50);
        crosshair.style.top = Length.Percent(50);
        crosshair.style.width = 30;
        crosshair.style.height = 30;
        crosshair.style.marginLeft = -15;
        crosshair.style.marginTop = -22;
        crosshair.style.unityTextAlign = TextAnchor.MiddleCenter;
        root.Add(crosshair);

        hintLabel = CreateLabel("", hintFontSize, whiteColor);
        hintLabel.style.position = Position.Absolute;
        hintLabel.style.left = Length.Percent(50);
        hintLabel.style.bottom = 84;
        hintLabel.style.width = 700;
        hintLabel.style.height = 42;
        hintLabel.style.marginLeft = -350;
        hintLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        hintLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        hintLabel.style.whiteSpace = WhiteSpace.Normal;
        hintLabel.pickingMode = PickingMode.Ignore;
        hintLabel.style.textShadow = new TextShadow
        {
            offset = new Vector2(2f, 2f),
            blurRadius = 2f,
            color = Color.black
        };
        root.Add(hintLabel);
    }


    private void CreateWiringDiagramPanel()
    {
        wiringDiagramOverlay = new VisualElement();
        wiringDiagramOverlay.style.position = Position.Absolute;
        wiringDiagramOverlay.style.left = 0;
        wiringDiagramOverlay.style.right = 0;
        wiringDiagramOverlay.style.top = 0;
        wiringDiagramOverlay.style.bottom = 0;
        wiringDiagramOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
        wiringDiagramOverlay.style.justifyContent = Justify.Center;
        wiringDiagramOverlay.style.alignItems = Align.Center;
        wiringDiagramOverlay.style.display = DisplayStyle.None;

        VisualElement panel = CreatePanel();
        panel.style.width = Length.Percent(84);
        panel.style.height = Length.Percent(88);
        panel.style.paddingTop = 22;
        panel.style.paddingBottom = 20;
        panel.style.paddingLeft = 24;
        panel.style.paddingRight = 24;
        panel.style.backgroundColor = new Color(0.07f, 0.07f, 0.085f, 0.97f);

        wiringDiagramTitleLabel = CreateLabel("Схема цепи стеклоочистителей", wiringDiagramTitleFontSize, whiteColor);
        wiringDiagramTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        wiringDiagramTitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        wiringDiagramTitleLabel.style.marginBottom = 14;

        wiringDiagramImageElement = new Image();
        wiringDiagramImageElement.scaleMode = ScaleMode.ScaleToFit;
        wiringDiagramImageElement.style.flexGrow = 1;
        wiringDiagramImageElement.style.minHeight = 360;
        wiringDiagramImageElement.style.backgroundColor = new Color(0.93f, 0.93f, 0.90f, 1f);
        wiringDiagramImageElement.style.borderTopLeftRadius = 10;
        wiringDiagramImageElement.style.borderTopRightRadius = 10;
        wiringDiagramImageElement.style.borderBottomLeftRadius = 10;
        wiringDiagramImageElement.style.borderBottomRightRadius = 10;
        wiringDiagramImageElement.style.borderTopWidth = 2;
        wiringDiagramImageElement.style.borderRightWidth = 2;
        wiringDiagramImageElement.style.borderBottomWidth = 2;
        wiringDiagramImageElement.style.borderLeftWidth = 2;
        wiringDiagramImageElement.style.borderTopColor = new Color(0.16f, 0.16f, 0.16f, 1f);
        wiringDiagramImageElement.style.borderRightColor = new Color(0.16f, 0.16f, 0.16f, 1f);
        wiringDiagramImageElement.style.borderBottomColor = new Color(0.16f, 0.16f, 0.16f, 1f);
        wiringDiagramImageElement.style.borderLeftColor = new Color(0.16f, 0.16f, 0.16f, 1f);
        wiringDiagramImageElement.pickingMode = PickingMode.Ignore;

        wiringDiagramNoteLabel = CreateLabel("", wiringDiagramNoteFontSize, whiteColor);
        wiringDiagramNoteLabel.style.whiteSpace = WhiteSpace.Normal;
        wiringDiagramNoteLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        wiringDiagramNoteLabel.style.marginTop = 14;
        wiringDiagramNoteLabel.style.marginBottom = 14;

        wiringDiagramCloseButton = new Button();
        wiringDiagramCloseButton.text = "Закрыть схему";
        wiringDiagramCloseButton.style.height = 40;
        wiringDiagramCloseButton.style.width = 220;
        wiringDiagramCloseButton.style.alignSelf = Align.Center;
        wiringDiagramCloseButton.style.borderTopLeftRadius = 10;
        wiringDiagramCloseButton.style.borderTopRightRadius = 10;
        wiringDiagramCloseButton.style.borderBottomLeftRadius = 10;
        wiringDiagramCloseButton.style.borderBottomRightRadius = 10;
        wiringDiagramCloseButton.style.backgroundColor = new Color(0.23f, 0.38f, 1f, 1f);
        wiringDiagramCloseButton.style.color = Color.white;
        wiringDiagramCloseButton.clicked += OnWiringDiagramCloseClicked;

        panel.Add(wiringDiagramTitleLabel);
        panel.Add(wiringDiagramImageElement);
        panel.Add(wiringDiagramNoteLabel);
        panel.Add(wiringDiagramCloseButton);
        wiringDiagramOverlay.Add(panel);
        root.Add(wiringDiagramOverlay);
    }

    private VisualElement CreateCircuitNode(string text)
    {
        Label node = CreateLabel(text, 12, new Color(0.06f, 0.06f, 0.06f, 1f));
        node.style.width = 74;
        node.style.height = 44;
        node.style.unityTextAlign = TextAnchor.MiddleCenter;
        node.style.unityFontStyleAndWeight = FontStyle.Bold;
        node.style.whiteSpace = WhiteSpace.Normal;
        node.style.backgroundColor = new Color(1f, 1f, 1f, 1f);
        node.style.borderTopLeftRadius = 7;
        node.style.borderTopRightRadius = 7;
        node.style.borderBottomLeftRadius = 7;
        node.style.borderBottomRightRadius = 7;
        node.style.borderTopWidth = 2;
        node.style.borderRightWidth = 2;
        node.style.borderBottomWidth = 2;
        node.style.borderLeftWidth = 2;
        node.style.borderTopColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        node.style.borderRightColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        node.style.borderBottomColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        node.style.borderLeftColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        return node;
    }

    private VisualElement CreateCircuitLine(bool broken)
    {
        VisualElement holder = new VisualElement();
        holder.style.width = broken ? 70 : 40;
        holder.style.height = 44;
        holder.style.justifyContent = Justify.Center;
        holder.style.alignItems = Align.Center;

        VisualElement line = new VisualElement();
        line.style.width = broken ? 48 : 36;
        line.style.height = 4;
        line.style.backgroundColor = broken ? new Color(0.85f, 0.06f, 0.04f, 1f) : new Color(0.08f, 0.08f, 0.08f, 1f);
        holder.Add(line);

        if (broken)
        {
            Label mark = CreateLabel("×", 28, new Color(0.85f, 0.06f, 0.04f, 1f));
            mark.style.position = Position.Absolute;
            mark.style.left = 22;
            mark.style.top = -2;
            mark.style.unityFontStyleAndWeight = FontStyle.Bold;
            holder.Add(mark);
        }

        return holder;
    }

    private void CreateResultOverlay()
    {
        resultOverlay = new VisualElement();
        resultOverlay.style.position = Position.Absolute;
        resultOverlay.style.left = 0;
        resultOverlay.style.right = 0;
        resultOverlay.style.top = 0;
        resultOverlay.style.bottom = 0;
        resultOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.66f);
        resultOverlay.style.justifyContent = Justify.Center;
        resultOverlay.style.alignItems = Align.Center;
        resultOverlay.style.display = DisplayStyle.None;

        VisualElement modal = CreatePanel();
        modal.style.width = 430;
        modal.style.paddingTop = 22;
        modal.style.paddingBottom = 22;
        modal.style.paddingLeft = 22;
        modal.style.paddingRight = 22;
        modal.style.backgroundColor = new Color(0.10f, 0.10f, 0.12f, 0.96f);

        resultTitleLabel = CreateLabel("Результат", resultTitleFontSize, whiteColor);
        resultTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        resultTitleLabel.style.marginBottom = 14;

        resultTimeLabel = CreateLabel("Время: 00:00", resultTextFontSize, whiteColor);
        resultErrorsLabel = CreateLabel("Ошибки: 0", resultTextFontSize, whiteColor);
        resultGradeLabel = CreateLabel("Оценка: 5", resultTextFontSize, whiteColor);
        resultGradeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        resultSaveLabel = CreateLabel("Результат сохранён", resultSaveFontSize, grayColor);
        resultSaveLabel.style.marginTop = 12;
        resultSaveLabel.style.whiteSpace = WhiteSpace.Normal;

        returnButton = new Button();
        returnButton.text = "Вернуться в меню";
        returnButton.style.height = 38;
        returnButton.style.marginTop = 18;
        returnButton.style.borderTopLeftRadius = 10;
        returnButton.style.borderTopRightRadius = 10;
        returnButton.style.borderBottomLeftRadius = 10;
        returnButton.style.borderBottomRightRadius = 10;
        returnButton.style.backgroundColor = new Color(0.23f, 0.38f, 1f, 1f);
        returnButton.style.color = Color.white;
        returnButton.clicked += OnReturnButtonClicked;

        modal.Add(resultTitleLabel);
        modal.Add(resultTimeLabel);
        modal.Add(resultErrorsLabel);
        modal.Add(resultGradeLabel);
        modal.Add(resultSaveLabel);
        modal.Add(returnButton);
        resultOverlay.Add(modal);
        root.Add(resultOverlay);
    }

    private void CreateFadeOverlay()
    {
        fadeOverlay = new VisualElement();
        fadeOverlay.style.position = Position.Absolute;
        fadeOverlay.style.left = 0;
        fadeOverlay.style.right = 0;
        fadeOverlay.style.top = 0;
        fadeOverlay.style.bottom = 0;
        fadeOverlay.style.backgroundColor = Color.black;
        fadeOverlay.pickingMode = PickingMode.Ignore;
        SetFade(0f);
        root.Add(fadeOverlay);
    }

    private VisualElement CreatePanel()
    {
        VisualElement panel = new VisualElement();
        panel.style.backgroundColor = panelColor;
        panel.style.borderTopLeftRadius = 14;
        panel.style.borderTopRightRadius = 14;
        panel.style.borderBottomLeftRadius = 14;
        panel.style.borderBottomRightRadius = 14;
        panel.style.borderTopWidth = 1;
        panel.style.borderRightWidth = 1;
        panel.style.borderBottomWidth = 1;
        panel.style.borderLeftWidth = 1;
        panel.style.borderTopColor = borderColor;
        panel.style.borderRightColor = borderColor;
        panel.style.borderBottomColor = borderColor;
        panel.style.borderLeftColor = borderColor;
        return panel;
    }

    private Label CreateLabel(string text, int fontSize, Color color)
    {
        Label label = new Label(text);
        label.style.fontSize = fontSize;
        label.style.color = color;
        return label;
    }

    private Label CreateStepLabel(int fontSize, Color color)
    {
        Label label = CreateLabel("", fontSize, color);
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.marginBottom = 5;
        return label;
    }

    public void SetTasks(List<ScenarioHudTask> hudTasks, List<ScenarioStep> steps, int currentStepIndex)
    {
        if (previousStepLabel == null)
            return;

        if (hudTasks != null && hudTasks.Count > 0)
        {
            SetHudTasksMode(hudTasks, currentStepIndex);
            return;
        }

        // Запасной вариант: если HUD-пункты не заполнены, показываем технические шаги.
        SetTechnicalStepsFallback(steps, currentStepIndex);
    }

    private void SetHudTasksMode(List<ScenarioHudTask> hudTasks, int currentStepIndex)
    {
        List<ScenarioHudTask> orderedTasks = new List<ScenarioHudTask>();

        for (int i = 0; i < hudTasks.Count; i++)
        {
            if (hudTasks[i] != null && !string.IsNullOrWhiteSpace(hudTasks[i].title))
                orderedTasks.Add(hudTasks[i]);
        }

        orderedTasks.Sort((a, b) => a.startStepNumber.CompareTo(b.startStepNumber));

        if (orderedTasks.Count == 0)
        {
            previousStepLabel.text = "";
            currentStepLabel.text = "Нет пунктов HUD";
            nextStepLabel.text = "";
            return;
        }

        int activeTaskIndex = 0;

        for (int i = 0; i < orderedTasks.Count; i++)
        {
            int taskStartStepIndex = Mathf.Max(0, orderedTasks[i].startStepNumber - 1);

            if (currentStepIndex >= taskStartStepIndex)
                activeTaskIndex = i;
            else
                break;
        }

        previousStepLabel.text = activeTaskIndex > 0 ? "✓ " + orderedTasks[activeTaskIndex - 1].title : "";
        currentStepLabel.text = "→ " + orderedTasks[activeTaskIndex].title;
        nextStepLabel.text = activeTaskIndex + 1 < orderedTasks.Count ? orderedTasks[activeTaskIndex + 1].title : "";
    }

    private void SetTechnicalStepsFallback(List<ScenarioStep> steps, int currentIndex)
    {
        if (steps == null || steps.Count == 0)
        {
            previousStepLabel.text = "";
            currentStepLabel.text = "Нет шагов сценария";
            nextStepLabel.text = "";
            return;
        }

        previousStepLabel.text = currentIndex > 0 ? "✓ " + steps[currentIndex - 1].title : "";
        currentStepLabel.text = "→ " + steps[currentIndex].title;
        nextStepLabel.text = currentIndex + 1 < steps.Count ? steps[currentIndex + 1].title : "";
    }

    public void SetStats(int seconds, int errors)
    {
        if (timeLabel != null)
            timeLabel.text = "Время: " + FormatDuration(seconds);

        if (errorsLabel != null)
            errorsLabel.text = "Ошибки: " + errors;
    }

    public void SetHeldTool(string toolName)
    {
        SetHeldTool(toolName, null);
    }

    public void SetHeldTool(string toolName, Texture2D toolIcon)
    {
        if (heldToolLabel == null)
            return;

        bool hasTool = !string.IsNullOrEmpty(toolName);
        heldToolLabel.text = hasTool ? toolName : "Ничего";

        if (heldToolIconImage == null)
            return;

        if (hasTool && toolIcon != null)
        {
            heldToolIconImage.image = toolIcon;
            heldToolIconImage.style.display = DisplayStyle.Flex;
        }
        else
        {
            heldToolIconImage.image = null;
            heldToolIconImage.style.display = DisplayStyle.None;
        }
    }

    public void SetHint(string text)
    {
        if (hintLabel != null)
            hintLabel.text = text;
    }

    public void ShowMeasurement(string title, string value, float seconds)
    {
        if (measurementPanel == null)
            return;

        if (measurementRoutine != null)
            StopCoroutine(measurementRoutine);

        measurementRoutine = StartCoroutine(ShowMeasurementRoutine(title, value, seconds));
    }

    private IEnumerator ShowMeasurementRoutine(string title, string value, float seconds)
    {
        measurementTitleLabel.text = string.IsNullOrWhiteSpace(title) ? "Измерение" : title;
        measurementValueLabel.text = string.IsNullOrWhiteSpace(value) ? "—" : value;
        measurementPanel.style.display = DisplayStyle.Flex;
        measurementPanel.style.opacity = 1f;

        float visibleTime = Mathf.Max(0.1f, seconds);
        yield return new WaitForSeconds(visibleTime);

        measurementPanel.style.display = DisplayStyle.None;
        measurementRoutine = null;
    }

    public void HideMeasurement()
    {
        if (measurementRoutine != null)
        {
            StopCoroutine(measurementRoutine);
            measurementRoutine = null;
        }

        if (measurementPanel != null)
            measurementPanel.style.display = DisplayStyle.None;
    }


    public void ShowWiringDiagram(string title, string note, Texture2D image, Action onClose)
    {
        if (wiringDiagramOverlay == null)
            return;

        wiringDiagramCloseCallback = onClose;
        wiringDiagramTitleLabel.text = string.IsNullOrWhiteSpace(title) ? "Схема цепи стеклоочистителей" : title;
        wiringDiagramNoteLabel.text = string.IsNullOrWhiteSpace(note)
            ? "Изучите схему участка цепи, затем закройте окно и продолжите диагностику мотора стеклоочистителей."
            : note;

        if (wiringDiagramImageElement != null)
        {
            wiringDiagramImageElement.image = image;

            if (image == null)
            {
                wiringDiagramImageElement.style.backgroundColor = new Color(0.16f, 0.16f, 0.18f, 1f);
                wiringDiagramNoteLabel.text = "Картинка схемы не назначена. Перетащите PNG/JPG в поле Wiring Diagram Image у нужного шага ScenarioManager.";
            }
            else
            {
                wiringDiagramImageElement.style.backgroundColor = new Color(0.93f, 0.93f, 0.90f, 1f);
            }
        }

        wiringDiagramOverlay.style.display = DisplayStyle.Flex;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    public void HideWiringDiagram()
    {
        if (wiringDiagramOverlay != null)
            wiringDiagramOverlay.style.display = DisplayStyle.None;

        wiringDiagramCloseCallback = null;
    }

    private void OnWiringDiagramCloseClicked()
    {
        if (wiringDiagramOverlay != null)
            wiringDiagramOverlay.style.display = DisplayStyle.None;

        Action callback = wiringDiagramCloseCallback;
        wiringDiagramCloseCallback = null;

        if (callback != null)
            callback.Invoke();
    }

    public void SetFade(float alpha)
    {
        fadeAlpha = Mathf.Clamp01(alpha);

        if (fadeOverlay != null)
        {
            fadeOverlay.style.opacity = fadeAlpha;
            fadeOverlay.style.display = fadeAlpha <= 0.001f ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    public IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = fadeAlpha;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            SetFade(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetFade(targetAlpha);
    }

    public void ShowResult(int durationSeconds, int errors, int grade, bool saved, string saveMessage, Action onReturn)
    {
        returnCallback = onReturn;

        resultTimeLabel.text = "Время: " + FormatDuration(durationSeconds);
        resultErrorsLabel.text = "Ошибки: " + errors;
        resultGradeLabel.text = "Оценка: " + grade;

        if (saved)
            resultSaveLabel.text = "Результат сохранён.";
        else
            resultSaveLabel.text = "Результат не сохранён. " + saveMessage;

        HideWiringDiagram();
        resultOverlay.style.display = DisplayStyle.Flex;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    public void HideResult()
    {
        if (resultOverlay != null)
            resultOverlay.style.display = DisplayStyle.None;
    }

    private void OnReturnButtonClicked()
    {
        if (returnCallback != null)
            returnCallback.Invoke();
    }

    private string FormatDuration(int seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);

        if (time.TotalHours >= 1)
            return string.Format("{0:00}:{1:00}:{2:00}", (int)time.TotalHours, time.Minutes, time.Seconds);

        return string.Format("{0:00}:{1:00}", time.Minutes, time.Seconds);
    }
}
