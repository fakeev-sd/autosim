using UnityEngine;

public class PovPoint : MonoBehaviour
{
    public string displayName = "POV-точка";
    public Transform cameraPose;

    [Header("Objects while this POV is active")]
    [Tooltip("Эти объекты включатся при входе в POV и выключатся при выходе из POV.")]
    public GameObject[] showObjectsOnEnter;

    [Tooltip("Эти объекты выключатся при входе в POV и включатся при выходе из POV.")]
    public GameObject[] hideObjectsOnEnter;
}
