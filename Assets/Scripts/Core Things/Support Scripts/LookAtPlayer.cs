using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    private const float ViewportMargin = 0.1f;

    private Camera targetCamera;

    void Awake()
    {
        targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (!targetCamera) return;
        if (!IsOnScreen()) return;

        transform.rotation = Quaternion.LookRotation(transform.position - targetCamera.transform.position);
    }

    private bool IsOnScreen()
    {
        Vector3 viewportPoint = targetCamera.WorldToViewportPoint(transform.position);
        if (viewportPoint.z <= 0f) return false;

        return viewportPoint.x >= -ViewportMargin && viewportPoint.x <= 1f + ViewportMargin
            && viewportPoint.y >= -ViewportMargin && viewportPoint.y <= 1f + ViewportMargin;
    }
}
