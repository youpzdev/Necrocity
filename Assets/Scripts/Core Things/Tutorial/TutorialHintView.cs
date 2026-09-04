using TMPro;
using UnityEngine;

public class TutorialHintView : MonoBehaviour
{
    [SerializeField] private GameObject body;
    [SerializeField] private TMP_Text messageText;

    [Header("Screen Size")]
    [SerializeField] private bool keepScreenSize;
    [SerializeField] private float referenceDistance = 35f;
    [SerializeField] private float referenceFieldOfView = 65f;
    [SerializeField] private float referenceOrthographicSize = 5f;
    [SerializeField] private float minScreenScale = 0.35f;
    [SerializeField] private float maxScreenScale = 1.5f;

    private Vector3 baseScale = Vector3.one;
    private Camera worldCamera;
    private bool initialized;

    private void Awake()
    {
        Init();
        Hide();
    }

    public void Show(string message)
    {
        Init();

        if (messageText != null)
        {
            messageText.text = message ?? string.Empty;
            messageText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
        }

        if (body != null) body.SetActive(true);
    }

    public void Hide()
    {
        if (body != null) body.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!keepScreenSize) return;
        if (body == null || !body.activeSelf) return;

        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null) return;

        transform.localScale = baseScale * ScreenCompensation();
    }

    private void Init()
    {
        if (initialized) return;
        initialized = true;

        baseScale = transform.localScale;
        worldCamera = Camera.main;
    }

    private float ScreenCompensation()
    {
        float reference = worldCamera.orthographic
            ? referenceOrthographicSize
            : referenceDistance * Mathf.Tan(referenceFieldOfView * 0.5f * Mathf.Deg2Rad);

        float current = CurrentViewHeight();
        if (reference < 0.01f || current < 0.01f) return 1f;

        return Mathf.Clamp(current / reference, minScreenScale, maxScreenScale);
    }

    private float CurrentViewHeight()
    {
        if (worldCamera.orthographic) return worldCamera.orthographicSize;

        float distance = Vector3.Distance(worldCamera.transform.position, transform.position);
        return distance * Mathf.Tan(worldCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }
}
