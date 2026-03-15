using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class AKBMassScenarioController : MonoBehaviour
{
    public enum ScenarioPickupType
    {
        None,
        WheelChockA,
        WheelChockB,
        LoadTester,
        Multimeter
    }

    private enum ErrorType
    {
        LoadTesterWrongPlus,
        LoadTesterTooEarly,
        MultimeterWrongPlus
    }

    [Header("Core References")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private FirstPersonMove playerMove;
    [SerializeField] private FirstPersonLook playerLook;
    [SerializeField] private InteractionController interaction;
    [SerializeField] private GameHUDController hud;

    [Header("Scene Roots")]
    [SerializeField] private GameObject carExteriorRoot;
    [SerializeField] private GameObject povUnderHoodRoot;

    [Header("POV")]
    [SerializeField] private Transform povUnderHoodAnchor;
    [SerializeField] private Transform exteriorReturnPoint;
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("POV Collision Control")]
    [Tooltip("Коллайдеры под этими root-объектами будут отключаться при входе в POV и включаться при выходе.")]
    [SerializeField] private GameObject[] disableColliderRootsInPOV;

    [Header("Pickups")]
    [SerializeField] private ScenarioPickupInteractable wheelChockA;
    [SerializeField] private ScenarioPickupInteractable wheelChockB;
    [SerializeField] private ScenarioPickupInteractable loadTester;
    [SerializeField] private ScenarioPickupInteractable multimeter;

    [Header("Placement Zones")]
    [SerializeField] private ScenarioPlacementZone[] chockZones;

    [Header("Visual State")]
    [SerializeField] private GameObject batteryMinusTerminalObject;
    [SerializeField] private GameObject brokenGroundWireObject;
    [SerializeField] private GameObject repairedGroundWireObject;

    [Header("Multimeter Probe Visuals")]
    [SerializeField] private GameObject multimeterMinusProbeObject;
    [SerializeField] private GameObject multimeterGroundProbeObject;

    [Header("Readouts")]
    [SerializeField] private string loadTesterReadoutTitle = "Нагрузочная вилка";
    [SerializeField] private string loadTesterReadoutValue = "10.1 В";
    [SerializeField] private string multimeterReadoutTitle = "Мультиметр";
    [SerializeField] private string multimeterReadoutValue = "1.8 Ом";
    [SerializeField] private float readoutDuration = 1.6f;

    [Header("Result")]
    [SerializeField] private float resultDelaySeconds = 1.2f;

    [Header("Grading")]
    [SerializeField] private float grade5MaxSeconds = 90f;
    [SerializeField] private int grade5MaxErrors = 0;

    [SerializeField] private float grade4MaxSeconds = 120f;
    [SerializeField] private int grade4MaxErrors = 1;

    [SerializeField] private float grade3MaxSeconds = 240f;
    [SerializeField] private int grade3MaxErrors = 2;

    [Header("Errors")]
    [SerializeField] private float errorCooldownSeconds = 3f;

    [Header("Scene Names")]
    [SerializeField] private string hubSceneName = "Hub";

    public event Action Changed;

    private readonly string[] _stepTexts =
    {
        "Установить противооткатные упоры",
        "Проверить АКБ нагрузочной вилкой",
        "Проверить массу кузова мультиметром",
        "Заменить неисправный провод массы"
    };

    private readonly Dictionary<ErrorType, float> _lastErrorTimes = new();
    private readonly List<Collider> _povToggleColliders = new();

    private ScenarioPickupInteractable _heldPickup;

    private bool _inPOV;
    private bool _transitionBusy;
    private bool _completed;
    private bool _timerRunning;

    private int _currentStepIndex;
    private int _errorCount;
    private float _elapsedTime;

    private bool _minusTerminalDisconnected;
    private bool _batteryChecked;

    private bool _multimeterConnectedToMinus;
    private bool _groundChecked;

    private bool _wireReplaced;

    private int _placedChockCount;

    public IReadOnlyList<string> StepTexts => _stepTexts;
    public int CurrentStepIndex => _currentStepIndex;
    public int ErrorCount => _errorCount;
    public float ElapsedTime => _elapsedTime;
    public bool InPOV => _inPOV;
    public bool Completed => _completed;
    public ScenarioPickupType HeldItemType => _heldPickup != null ? _heldPickup.PickupType : ScenarioPickupType.None;

    private void Reset()
    {
        playerRoot = GameObject.Find("Player") != null ? GameObject.Find("Player").transform : null;
        playerMove = FindAnyObjectByType<FirstPersonMove>();
        playerLook = FindAnyObjectByType<FirstPersonLook>();
        interaction = FindAnyObjectByType<InteractionController>();
        hud = FindAnyObjectByType<GameHUDController>();

        if (playerRoot != null && characterController == null)
            characterController = playerRoot.GetComponent<CharacterController>();
    }

    private void Awake()
    {
        if (playerMove == null) playerMove = FindAnyObjectByType<FirstPersonMove>();
        if (playerLook == null) playerLook = FindAnyObjectByType<FirstPersonLook>();
        if (interaction == null) interaction = FindAnyObjectByType<InteractionController>();
        if (hud == null) hud = FindAnyObjectByType<GameHUDController>();

        if (playerRoot == null && playerMove != null)
            playerRoot = playerMove.transform;

        if (characterController == null && playerRoot != null)
            characterController = playerRoot.GetComponent<CharacterController>();

        RebuildPOVColliderCache();
    }

    private void Start()
    {
        ResetScenario();
    }

    private void Update()
    {
        if (_timerRunning && !_completed)
            _elapsedTime += Time.deltaTime;

        if (_inPOV && !_transitionBusy && !_completed && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            StartCoroutine(ExitUnderHoodRoutine());
    }

    private void RebuildPOVColliderCache()
    {
        _povToggleColliders.Clear();

        if (disableColliderRootsInPOV == null)
            return;

        foreach (GameObject root in disableColliderRootsInPOV)
        {
            if (root == null)
                continue;

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
            {
                if (col != null && !_povToggleColliders.Contains(col))
                    _povToggleColliders.Add(col);
            }
        }
    }

    private void SetPOVCollisionRootsEnabled(bool enabled)
    {
        for (int i = 0; i < _povToggleColliders.Count; i++)
        {
            if (_povToggleColliders[i] != null)
                _povToggleColliders[i].enabled = enabled;
        }
    }

    public void ResetScenario()
    {
        _heldPickup = null;

        _inPOV = false;
        _transitionBusy = false;
        _completed = false;
        _timerRunning = true;

        _currentStepIndex = 0;
        _errorCount = 0;
        _elapsedTime = 0f;

        _minusTerminalDisconnected = false;
        _batteryChecked = false;

        _multimeterConnectedToMinus = false;
        _groundChecked = false;

        _wireReplaced = false;
        _placedChockCount = 0;

        _lastErrorTimes.Clear();

        if (carExteriorRoot != null) carExteriorRoot.SetActive(true);
        if (povUnderHoodRoot != null) povUnderHoodRoot.SetActive(false);

        SetPOVCollisionRootsEnabled(true);

        if (batteryMinusTerminalObject != null) batteryMinusTerminalObject.SetActive(true);

        if (brokenGroundWireObject != null) brokenGroundWireObject.SetActive(true);
        if (repairedGroundWireObject != null) repairedGroundWireObject.SetActive(false);

        if (multimeterMinusProbeObject != null) multimeterMinusProbeObject.SetActive(false);
        if (multimeterGroundProbeObject != null) multimeterGroundProbeObject.SetActive(false);

        if (wheelChockA != null) wheelChockA.ReturnToOrigin();
        if (wheelChockB != null) wheelChockB.ReturnToOrigin();
        if (loadTester != null) loadTester.ReturnToOrigin();
        if (multimeter != null) multimeter.ReturnToOrigin();

        if (chockZones != null)
        {
            foreach (ScenarioPlacementZone zone in chockZones)
            {
                if (zone != null)
                    zone.ResetState();
            }
        }

        if (playerMove != null) playerMove.SetMovementEnabled(true);

        if (playerLook != null)
        {
            playerLook.SetLookEnabled(true);
            playerLook.SetCursorLocked(true);
        }

        if (interaction != null) interaction.SetInteractionEnabled(true);

        if (hud != null)
        {
            hud.HideReadoutImmediate();
            hud.HideResultImmediate();
            hud.SetFadeImmediate(0f);
        }

        NotifyChanged();
    }

    public bool CanPickUp(ScenarioPickupInteractable pickup)
    {
        if (pickup == null || _transitionBusy || _completed)
            return false;

        if (_heldPickup != null)
            return false;

        return pickup.PickupType switch
        {
            ScenarioPickupType.WheelChockA => _currentStepIndex == 0 && _placedChockCount < 2,
            ScenarioPickupType.WheelChockB => _currentStepIndex == 0 && _placedChockCount < 2,
            ScenarioPickupType.LoadTester => _currentStepIndex == 1 && !_batteryChecked,
            ScenarioPickupType.Multimeter => _currentStepIndex == 2 && !_groundChecked,
            _ => false
        };
    }

    public void TryPickUp(ScenarioPickupInteractable pickup)
    {
        if (!CanPickUp(pickup))
            return;

        _heldPickup = pickup;
        pickup.TakeIntoHand();

        NotifyChanged();
    }

    public bool CanPlaceCurrentChockHere(ScenarioPlacementZone zone)
    {
        if (zone == null || zone.IsOccupied || _transitionBusy || _completed)
            return false;

        if (_currentStepIndex != 0)
            return false;

        return HeldItemType == ScenarioPickupType.WheelChockA || HeldItemType == ScenarioPickupType.WheelChockB;
    }

    public void TryPlaceCurrentChock(ScenarioPlacementZone zone)
    {
        if (!CanPlaceCurrentChockHere(zone))
            return;

        _heldPickup.PlaceAt(zone.SnapPoint);
        zone.Occupy(_heldPickup);

        _heldPickup = null;
        _placedChockCount++;

        if (_placedChockCount >= 2)
            CompleteCurrentStep();

        NotifyChanged();
    }

    public string GetHoverTextForAction(ScenarioActionInteractable.ActionType actionType)
    {
        if (_transitionBusy || _completed)
            return string.Empty;

        switch (actionType)
        {
            case ScenarioActionInteractable.ActionType.EnterUnderHood:
                return !_inPOV ? "Осмотреть под капотом" : string.Empty;

            case ScenarioActionInteractable.ActionType.BatteryMinusTerminal:
                if (_inPOV && _currentStepIndex == 1 && HeldItemType == ScenarioPickupType.LoadTester && !_minusTerminalDisconnected)
                    return "Отключить";
                return string.Empty;

            case ScenarioActionInteractable.ActionType.BatteryPlusTerminal:
                if (_inPOV && _currentStepIndex == 1 && HeldItemType == ScenarioPickupType.LoadTester && !_minusTerminalDisconnected)
                    return "Отключить";
                return string.Empty;

            case ScenarioActionInteractable.ActionType.BatteryPostsHotspot:
                if (_inPOV && _currentStepIndex == 1 && HeldItemType == ScenarioPickupType.LoadTester)
                    return "Подключить вилку";
                return string.Empty;

            case ScenarioActionInteractable.ActionType.BatteryMinusPostHotspot:
                if (_inPOV && _currentStepIndex == 2 && HeldItemType == ScenarioPickupType.Multimeter && !_multimeterConnectedToMinus)
                    return "Подключить щуп";
                return string.Empty;

            case ScenarioActionInteractable.ActionType.BatteryPlusPostHotspot:
                if (_inPOV && _currentStepIndex == 2 && HeldItemType == ScenarioPickupType.Multimeter && !_multimeterConnectedToMinus)
                    return "Подключить щуп";
                return string.Empty;

            case ScenarioActionInteractable.ActionType.BodyGroundPoint:
                if (_inPOV && _currentStepIndex == 2 && HeldItemType == ScenarioPickupType.Multimeter && _multimeterConnectedToMinus && !_groundChecked)
                    return "Подключить щуп";
                return string.Empty;

            case ScenarioActionInteractable.ActionType.BrokenGroundWire:
                if (_inPOV && _currentStepIndex == 3)
                    return "Заменить";
                return string.Empty;
        }

        return string.Empty;
    }

    public void HandleAction(ScenarioActionInteractable.ActionType actionType)
    {
        if (_transitionBusy || _completed)
            return;

        switch (actionType)
        {
            case ScenarioActionInteractable.ActionType.EnterUnderHood:
                if (!_inPOV)
                    StartCoroutine(EnterUnderHoodRoutine());
                break;

            case ScenarioActionInteractable.ActionType.BatteryMinusTerminal:
                HandleBatteryMinusTerminal();
                break;

            case ScenarioActionInteractable.ActionType.BatteryPlusTerminal:
                HandleBatteryPlusTerminal();
                break;

            case ScenarioActionInteractable.ActionType.BatteryPostsHotspot:
                HandleBatteryPosts();
                break;

            case ScenarioActionInteractable.ActionType.BatteryMinusPostHotspot:
                HandleBatteryMinusPost();
                break;

            case ScenarioActionInteractable.ActionType.BatteryPlusPostHotspot:
                HandleBatteryPlusPost();
                break;

            case ScenarioActionInteractable.ActionType.BodyGroundPoint:
                HandleBodyGroundPoint();
                break;

            case ScenarioActionInteractable.ActionType.BrokenGroundWire:
                HandleBrokenGroundWire();
                break;
        }
    }

    private void HandleBatteryMinusTerminal()
    {
        if (!_inPOV || _currentStepIndex != 1 || HeldItemType != ScenarioPickupType.LoadTester || _minusTerminalDisconnected)
            return;

        _minusTerminalDisconnected = true;

        if (batteryMinusTerminalObject != null)
            batteryMinusTerminalObject.SetActive(false);

        NotifyChanged();
    }

    private void HandleBatteryPlusTerminal()
    {
        if (!_inPOV || _currentStepIndex != 1 || HeldItemType != ScenarioPickupType.LoadTester || _minusTerminalDisconnected)
            return;

        AddError(ErrorType.LoadTesterWrongPlus);
    }

    private void HandleBatteryPosts()
    {
        if (!_inPOV || _currentStepIndex != 1 || HeldItemType != ScenarioPickupType.LoadTester || _batteryChecked)
            return;

        if (!_minusTerminalDisconnected)
        {
            AddError(ErrorType.LoadTesterTooEarly);
            return;
        }

        _batteryChecked = true;
        CompleteCurrentStep();
        StartCoroutine(BatteryReadoutRoutine());
    }

    private void HandleBatteryMinusPost()
    {
        if (!_inPOV || _currentStepIndex != 2 || HeldItemType != ScenarioPickupType.Multimeter || _multimeterConnectedToMinus)
            return;

        _multimeterConnectedToMinus = true;

        if (multimeterMinusProbeObject != null)
            multimeterMinusProbeObject.SetActive(true);

        NotifyChanged();
    }

    private void HandleBatteryPlusPost()
    {
        if (!_inPOV || _currentStepIndex != 2 || HeldItemType != ScenarioPickupType.Multimeter || _multimeterConnectedToMinus)
            return;

        AddError(ErrorType.MultimeterWrongPlus);
    }

    private void HandleBodyGroundPoint()
    {
        if (!_inPOV || _currentStepIndex != 2 || HeldItemType != ScenarioPickupType.Multimeter || !_multimeterConnectedToMinus || _groundChecked)
            return;

        _groundChecked = true;

        if (multimeterGroundProbeObject != null)
            multimeterGroundProbeObject.SetActive(true);

        CompleteCurrentStep();
        StartCoroutine(MultimeterReadoutRoutine());
    }

    private void HandleBrokenGroundWire()
    {
        if (!_inPOV || _currentStepIndex != 3 || _wireReplaced)
            return;

        _wireReplaced = true;

        if (brokenGroundWireObject != null) brokenGroundWireObject.SetActive(false);
        if (repairedGroundWireObject != null) repairedGroundWireObject.SetActive(true);

        CompleteCurrentStep();
        StartCoroutine(FinishScenarioRoutine());
    }

    private IEnumerator BatteryReadoutRoutine()
    {
        if (_heldPickup == loadTester)
            _heldPickup = null;

        if (loadTester != null)
            loadTester.ReturnToOrigin();

        NotifyChanged();

        if (hud != null)
            yield return StartCoroutine(
                hud.ShowInstrumentReadout(loadTesterReadoutTitle, loadTesterReadoutValue, readoutDuration)
            );
    }

    private IEnumerator MultimeterReadoutRoutine()
    {
        if (_heldPickup == multimeter)
            _heldPickup = null;

        if (multimeter != null)
            multimeter.ReturnToOrigin();

        NotifyChanged();

        if (hud != null)
            yield return StartCoroutine(
                hud.ShowInstrumentReadout(multimeterReadoutTitle, multimeterReadoutValue, readoutDuration)
            );
    }

    private IEnumerator FinishScenarioRoutine()
    {
        _timerRunning = false;
        _completed = true;

        int grade = CalculateGrade();

        if (playerMove != null) playerMove.SetMovementEnabled(false);
        if (interaction != null) interaction.SetInteractionEnabled(false);

        if (playerLook != null)
        {
            playerLook.SetLookEnabled(false);
            playerLook.SetCursorLocked(false);
        }

        NotifyChanged();

        if (resultDelaySeconds > 0f)
            yield return new WaitForSeconds(resultDelaySeconds);

        if (hud != null)
            hud.ShowResult(_elapsedTime, _errorCount, grade, ReturnToHub);
    }

    private IEnumerator EnterUnderHoodRoutine()
    {
        if (_transitionBusy)
            yield break;

        _transitionBusy = true;

        if (interaction != null)
            interaction.SetInteractionEnabled(false);

        if (hud != null)
            yield return StartCoroutine(hud.FadeRoutine(0f, 1f, fadeDuration));

        SetPOVCollisionRootsEnabled(false);

        if (carExteriorRoot != null) carExteriorRoot.SetActive(false);
        if (povUnderHoodRoot != null) povUnderHoodRoot.SetActive(true);

        TeleportPlayerTo(povUnderHoodAnchor);

        if (playerMove != null)
            playerMove.SetMovementEnabled(false);

        _inPOV = true;

        if (interaction != null)
            interaction.SetInteractionEnabled(true);

        if (hud != null)
            yield return StartCoroutine(hud.FadeRoutine(1f, 0f, fadeDuration));

        _transitionBusy = false;
        NotifyChanged();
    }

    private IEnumerator ExitUnderHoodRoutine()
    {
        if (_transitionBusy)
            yield break;

        _transitionBusy = true;

        if (interaction != null)
            interaction.SetInteractionEnabled(false);

        if (hud != null)
            yield return StartCoroutine(hud.FadeRoutine(0f, 1f, fadeDuration));

        if (povUnderHoodRoot != null) povUnderHoodRoot.SetActive(false);
        if (carExteriorRoot != null) carExteriorRoot.SetActive(true);

        SetPOVCollisionRootsEnabled(true);

        TeleportPlayerTo(exteriorReturnPoint);

        _inPOV = false;

        if (playerMove != null)
            playerMove.SetMovementEnabled(true);

        if (interaction != null)
            interaction.SetInteractionEnabled(true);

        if (hud != null)
            yield return StartCoroutine(hud.FadeRoutine(1f, 0f, fadeDuration));

        _transitionBusy = false;
        NotifyChanged();
    }

    private void TeleportPlayerTo(Transform target)
    {
        if (target == null || playerRoot == null)
            return;

        if (characterController != null)
            characterController.enabled = false;

        playerRoot.SetPositionAndRotation(target.position, Quaternion.Euler(0f, target.eulerAngles.y, 0f));

        if (playerLook != null)
            playerLook.AlignToWorldRotation(target.rotation);

        if (characterController != null)
            characterController.enabled = true;
    }

    private void CompleteCurrentStep()
    {
        if (_currentStepIndex < _stepTexts.Length)
            _currentStepIndex++;

        NotifyChanged();
    }

    private void AddError(ErrorType errorType)
    {
        float now = Time.time;

        if (_lastErrorTimes.TryGetValue(errorType, out float lastTime))
        {
            if (now - lastTime < errorCooldownSeconds)
                return;
        }

        _lastErrorTimes[errorType] = now;
        _errorCount++;

        NotifyChanged();
    }

    private int CalculateGrade()
    {
        if (_errorCount <= grade5MaxErrors && _elapsedTime <= grade5MaxSeconds)
            return 5;

        if (_errorCount <= grade4MaxErrors && _elapsedTime <= grade4MaxSeconds)
            return 4;

        if (_errorCount <= grade3MaxErrors && _elapsedTime <= grade3MaxSeconds)
            return 3;

        return 2;
    }

    private void ReturnToHub()
    {
        SceneManager.LoadScene(hubSceneName);
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }
}