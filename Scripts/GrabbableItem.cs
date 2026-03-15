using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GrabbableItem : MonoBehaviour
{
    public string itemId = "item";

    [HideInInspector] public Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
}
