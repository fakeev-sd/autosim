using UnityEngine;

public class GrabbableTool : MonoBehaviour
{
    public string displayName = "Инструмент";

    [Header("HUD / иконка в руках")]
    public Texture2D icon;

    private Transform startParent;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool cached;

    private void Awake()
    {
        CacheStartPose();
    }

    public void CacheStartPose()
    {
        if (cached)
            return;

        startParent = transform.parent;
        startPosition = transform.position;
        startRotation = transform.rotation;
        cached = true;
    }

    public void TakeToHands()
    {
        CacheStartPose();
        gameObject.SetActive(false);
    }

    public void ReturnToWorld()
    {
        CacheStartPose();
        transform.SetParent(startParent);
        transform.position = startPosition;
        transform.rotation = startRotation;
        gameObject.SetActive(true);
    }
}
