using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AutoSim/Task Catalog", fileName = "TaskCatalog")]
public class TaskCatalog : ScriptableObject
{
    public List<TaskInfo> tasks = new();
}

[Serializable]
public class TaskInfo
{
    public string title;
    [TextArea] public string description;
    public string sceneName;
}
