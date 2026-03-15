using System;
using UnityEngine;

public class ProgressTracker : MonoBehaviour
{
    public static ProgressTracker Instance { get; private set; }

    [SerializeField] private ProgressDefinition definition;

    public event Action Changed;

    public int CurrentIndex { get; private set; } = 0;

    private bool[] _completed;

    public ProgressDefinition Definition => definition;

    private void Awake()
    {
        Instance = this;

        if (definition == null || definition.steps == null)
        {
            _completed = Array.Empty<bool>();
            return;
        }

        _completed = new bool[definition.steps.Count];
    }

    public bool IsCompleted(int index)
    {
        if (_completed == null || index < 0 || index >= _completed.Length) return false;
        return _completed[index];
    }

    public bool TryCompleteStep(string stepId)
    {
        if (definition == null || definition.steps == null) return false;

        if (CurrentIndex < 0 || CurrentIndex >= definition.steps.Count) return false;

        var current = definition.steps[CurrentIndex];
        if (!string.Equals(current.id, stepId, StringComparison.Ordinal))
            return false;

        _completed[CurrentIndex] = true;
        CurrentIndex++;

        Changed?.Invoke();
        return true;
    }

    public int GetWindowStart()
    {
        return (CurrentIndex / 3) * 3;
    }
}
