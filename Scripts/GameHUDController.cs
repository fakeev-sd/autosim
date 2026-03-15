using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class GameHUDController : MonoBehaviour
{
    [Serializable]
    public class ToolIconBinding
    {
        public AKBMassScenarioController.ScenarioPickupType toolType;
        public string displayName;
        public Texture2D icon;
    }

    public static GameHUDController Instance { get; private set; }

    [SerializeField] private AKBMassScenarioController scenario;
    [SerializeField] private InteractionController interaction;

    [Header("Tool UI")]
    [SerializeField] private ToolIconBinding[] toolIcons;

    [Header("Readout Font")]
    [SerializeField] private Font readoutDigitalFont;

    private VisualElement _root;

    private VisualElement _taskPrevRow;
    private VisualElement _taskCurrentRow;
    private VisualElement _taskNextRow;

    private Label _taskPrevText;
    private Label _taskCurrentText;
    private Label _taskNextText;

    private Label _timerValue;
    private Label _errorsValue;

    private VisualElement _heldItemPanel;
    private Image _heldItemImage;
    private Label _heldItemName;

    private VisualElement _hintPanel;
    private Label _hintLabel;

    private VisualElement _crosshair;

    private VisualElement _fadeOverlay;

    private VisualElement _readoutPanel;
    private Label _readoutTitle;
    private Label _readoutValue;

    private VisualElement _resultOverlay;
    private Label _resultTime;
    private Label _resultErrors;
    private Label _resultGrade;
    private Button _resultBackButton;

    private Action _resultBackAction;
    private Coroutine _readoutRoutine;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        if (scenario == null) scenario = FindAnyObjectByType<AKBMassScenarioController>();
        if (interaction == null) interaction = FindAnyObjectByType<InteractionController>();

        _root = GetComponent<UIDocument>().rootVisualElement;

        CacheUI();
        ApplyReadoutFont();

        if (scenario != null)
            scenario.Changed += RedrawAll;

        if (interaction != null)
        {
            interaction.HoverChanged += OnHoverChanged;
            interaction.HoverTextChanged += OnHoverTextChanged;
        }

        if (_resultBackButton != null)
            _resultBackButton.clicked += OnResultBackClicked;

        HideReadoutImmediate();
        HideResultImmediate();
        SetFadeImmediate(0f);
        RedrawAll();
    }

    private void OnDisable()
    {
        if (scenario != null)
            scenario.Changed -= RedrawAll;

        if (interaction != null)
        {
            interaction.HoverChanged -= OnHoverChanged;
            interaction.HoverTextChanged -= OnHoverTextChanged;
        }

        if (_resultBackButton != null)
            _resultBackButton.clicked -= OnResultBackClicked;
    }

    private void Update()
    {
        UpdateTimerAndErrors();
    }

    private void CacheUI()
    {
        _taskPrevRow = _root.Q<VisualElement>("task-prev-row");
        _taskCurrentRow = _root.Q<VisualElement>("task-current-row");
        _taskNextRow = _root.Q<VisualElement>("task-next-row");

        _taskPrevText = _root.Q<Label>("task-prev-text");
        _taskCurrentText = _root.Q<Label>("task-current-text");
        _taskNextText = _root.Q<Label>("task-next-text");

        _timerValue = _root.Q<Label>("timer-value");
        _errorsValue = _root.Q<Label>("errors-value");

        _heldItemPanel = _root.Q<VisualElement>("held-item-panel");
        _heldItemImage = _root.Q<Image>("held-item-image");
        _heldItemName = _root.Q<Label>("held-item-name");

        _hintPanel = _root.Q<VisualElement>("hint-panel");
        _hintLabel = _root.Q<Label>("hint-label");

        _crosshair = _root.Q<VisualElement>("crosshair");

        _fadeOverlay = _root.Q<VisualElement>("fade-overlay");

        _readoutPanel = _root.Q<VisualElement>("readout-panel");
        _readoutTitle = _root.Q<Label>("readout-title");
        _readoutValue = _root.Q<Label>("readout-value");

        _resultOverlay = _root.Q<VisualElement>("result-overlay");
        _resultTime = _root.Q<Label>("result-time");
        _resultErrors = _root.Q<Label>("result-errors");
        _resultGrade = _root.Q<Label>("result-grade");
        _resultBackButton = _root.Q<Button>("result-back-btn");
    }

    private void ApplyReadoutFont()
    {
        if (readoutDigitalFont == null)
            return;

        if (_readoutTitle != null)
            _readoutTitle.style.unityFontDefinition = FontDefinition.FromFont(readoutDigitalFont);

        if (_readoutValue != null)
            _readoutValue.style.unityFontDefinition = FontDefinition.FromFont(readoutDigitalFont);
    }

    private void RedrawAll()
    {
        RedrawTasks();
        UpdateTimerAndErrors();
        RedrawHeldItem();
    }

    private void RedrawTasks()
    {
        if (scenario == null)
            return;

        int current = scenario.CurrentStepIndex;
        var steps = scenario.StepTexts;

        SetTaskRow(_taskPrevRow, _taskPrevText, current > 0 ? steps[current - 1] : string.Empty, "completed", current > 0);
        SetTaskRow(_taskCurrentRow, _taskCurrentText, current < steps.Count ? steps[current] : string.Empty, "current", current < steps.Count);
        SetTaskRow(_taskNextRow, _taskNextText, current + 1 < steps.Count ? steps[current + 1] : string.Empty, "next", current + 1 < steps.Count);
    }

    private void SetTaskRow(VisualElement row, Label label, string text, string stateClass, bool visible)
    {
        if (row == null || label == null)
            return;

        row.RemoveFromClassList("completed");
        row.RemoveFromClassList("current");
        row.RemoveFromClassList("next");

        if (!visible)
        {
            row.AddToClassList("hidden");
            label.text = string.Empty;
            return;
        }

        row.RemoveFromClassList("hidden");
        row.AddToClassList(stateClass);
        label.text = text;
    }

    private void UpdateTimerAndErrors()
    {
        if (scenario == null)
            return;

        if (_timerValue != null)
            _timerValue.text = FormatTime(scenario.ElapsedTime);

        if (_errorsValue != null)
            _errorsValue.text = scenario.ErrorCount.ToString();
    }

    private void RedrawHeldItem()
    {
        if (_heldItemPanel == null || _heldItemImage == null || _heldItemName == null || scenario == null)
            return;

        ToolIconBinding binding = FindToolBinding(scenario.HeldItemType);

        if (binding == null || binding.icon == null)
        {
            _heldItemPanel.AddToClassList("empty");
            _heldItemImage.image = null;
        }
        else
        {
            _heldItemPanel.RemoveFromClassList("empty");
            _heldItemImage.image = binding.icon;
            _heldItemImage.scaleMode = ScaleMode.ScaleToFit;
        }

        if (binding == null || string.IsNullOrWhiteSpace(binding.displayName))
            _heldItemName.text = "—";
        else
            _heldItemName.text = binding.displayName;
    }

    private ToolIconBinding FindToolBinding(AKBMassScenarioController.ScenarioPickupType toolType)
    {
        if (toolIcons == null)
            return null;

        foreach (ToolIconBinding binding in toolIcons)
        {
            if (binding != null && binding.toolType == toolType)
                return binding;
        }

        return null;
    }

    private void OnHoverChanged(bool isHovering)
    {
        if (_crosshair == null)
            return;

        if (isHovering) _crosshair.AddToClassList("active");
        else _crosshair.RemoveFromClassList("active");
    }

    private void OnHoverTextChanged(string hoverText)
    {
        if (_hintPanel == null || _hintLabel == null)
            return;

        if (string.IsNullOrWhiteSpace(hoverText))
        {
            _hintPanel.AddToClassList("hidden");
            _hintLabel.text = string.Empty;
        }
        else
        {
            _hintPanel.RemoveFromClassList("hidden");
            _hintLabel.text = hoverText;
        }
    }

    public IEnumerator FadeRoutine(float from, float to, float duration)
    {
        if (_fadeOverlay == null)
            yield break;

        _fadeOverlay.style.display = DisplayStyle.Flex;
        _fadeOverlay.style.opacity = from;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
            _fadeOverlay.style.opacity = Mathf.Lerp(from, to, lerp);
            yield return null;
        }

        _fadeOverlay.style.opacity = to;

        if (to <= 0.001f)
            _fadeOverlay.style.display = DisplayStyle.None;
    }

    public void SetFadeImmediate(float alpha)
    {
        if (_fadeOverlay == null)
            return;

        if (alpha <= 0.001f)
        {
            _fadeOverlay.style.opacity = 0f;
            _fadeOverlay.style.display = DisplayStyle.None;
        }
        else
        {
            _fadeOverlay.style.display = DisplayStyle.Flex;
            _fadeOverlay.style.opacity = alpha;
        }
    }

    public IEnumerator ShowInstrumentReadout(string title, string value, float duration)
    {
        if (_readoutRoutine != null)
            StopCoroutine(_readoutRoutine);

        if (_readoutPanel == null || _readoutTitle == null || _readoutValue == null)
            yield break;

        _readoutTitle.text = title.ToUpperInvariant();
        _readoutValue.text = value;
        _readoutPanel.RemoveFromClassList("hidden");

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        _readoutPanel.AddToClassList("hidden");
        _readoutRoutine = null;
    }

    public void HideReadoutImmediate()
    {
        if (_readoutPanel != null)
            _readoutPanel.AddToClassList("hidden");
    }

    public void ShowResult(float elapsedTime, int errors, int grade, Action onBack)
    {
        _resultBackAction = onBack;

        if (_resultTime != null)
            _resultTime.text = FormatTime(elapsedTime);

        if (_resultErrors != null)
            _resultErrors.text = errors.ToString();

        if (_resultGrade != null)
            _resultGrade.text = grade.ToString();

        if (_resultOverlay != null)
            _resultOverlay.RemoveFromClassList("hidden");
    }

    public void HideResultImmediate()
    {
        _resultBackAction = null;

        if (_resultOverlay != null)
            _resultOverlay.AddToClassList("hidden");
    }

    private void OnResultBackClicked()
    {
        _resultBackAction?.Invoke();
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return $"{minutes:00}:{secs:00}";
    }
}