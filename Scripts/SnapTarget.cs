using UnityEngine;

public class SnapTarget : MonoBehaviour
{
    [Header("Matching")]
    public string acceptsItemId = "item";

    [Header("Snap")]
    public Transform snapPoint;

    [Header("Progress")]
    public string stepIdOnSnap;

    public bool CanAccept(GrabbableItem item)
    {
        return item != null && item.itemId == acceptsItemId;
    }

    public void Snap(GrabbableItem item)
    {
        if (item == null || snapPoint == null) return;

        item.transform.SetPositionAndRotation(snapPoint.position, snapPoint.rotation);
        item.transform.SetParent(snapPoint, true);

        if (item.rb != null)
        {
            item.rb.linearVelocity = Vector3.zero;
            item.rb.angularVelocity = Vector3.zero;
            item.rb.isKinematic = true;
        }

        if (!string.IsNullOrWhiteSpace(stepIdOnSnap))
            ProgressTracker.Instance?.TryCompleteStep(stepIdOnSnap);
    }
}
