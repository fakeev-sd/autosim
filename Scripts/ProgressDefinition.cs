using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AutoSim/Progress Definition", fileName = "ProgressDefinition")]
public class ProgressDefinition : ScriptableObject
{
    public List<ProgressStep> steps = new();
}

[Serializable]
public class ProgressStep
{
    public string id;
    [TextArea] public string text;
}
