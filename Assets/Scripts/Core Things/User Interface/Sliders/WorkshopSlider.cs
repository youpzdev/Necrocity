using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WorkshopSlider : MonoBehaviour
{
    [SerializeField] private Image slider;
    [SerializeField] private float tweenDuration = 0.5f;
    [Space(5)]
    [SerializeField] private Image componentIcon;
    [SerializeField] private GameObject readyIcon;
    [SerializeField] private TMP_Text readyText;
    [SerializeField] private string readyMessage = "Компонент готов";
    [Tooltip("Если пусто, ищется в сцене при старте (префаб не в иерархии панели).")]
    [SerializeField] private WorkshopPanel workshopPanel;
    [SerializeField] private Button openWorkshopButton;

    [Header("Screen Size")]
    [SerializeField] private bool keepScreenSize = true;
    [SerializeField] private bool calibrateReferenceOnStart = true;
    [SerializeField] private float referenceDistance = 35f;
    [SerializeField] private float referenceFieldOfView = 65f;
    [SerializeField] private float referenceOrthographicSize = 5f;
    [SerializeField] private float badgeScale = 2.5f;
    [SerializeField] private float minScreenScale = 0.35f;
    [SerializeField] private float maxScreenScale = 1.5f;

    private float currentFill;
    private Vector3 _baseScale;
    private Camera _camera;
    private float _referenceViewHeight;
    private bool _referenceCalibrated;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _camera = Camera.main;

        if (workshopPanel == null) workshopPanel = FindFirstObjectByType<WorkshopPanel>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        if (workshopPanel == null) workshopPanel = FindFirstObjectByType<WorkshopPanel>(FindObjectsInactive.Include);

        CalibrateReference();

        CraftingQueue.Instance.RestoreCurrentData();

        CraftingQueue.Instance.OnCraftStarted += OnCraftStarted;
        CraftingQueue.Instance.OnCraftCompleted += OnCraftCompleted;
        CraftingQueue.Instance.OnCraftCancelled += OnCraftCancelled;
        CraftingQueue.Instance.OnCraftRedeemed += OnCraftRedeemed;
        if (openWorkshopButton) openWorkshopButton.onClick.AddListener(OnOpenWorkshopClicked);

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (openWorkshopButton) openWorkshopButton.onClick.RemoveListener(OnOpenWorkshopClicked);

        if (CraftingQueue.Instance == null) return;
        CraftingQueue.Instance.OnCraftStarted -= OnCraftStarted;
        CraftingQueue.Instance.OnCraftCompleted -= OnCraftCompleted;
        CraftingQueue.Instance.OnCraftCancelled -= OnCraftCancelled;
        CraftingQueue.Instance.OnCraftRedeemed -= OnCraftRedeemed;
    }

    private void OnOpenWorkshopClicked()
    {
        if (UIManager.Instance == null) return;

        var q = CraftingQueue.Instance;
        if (q != null && q.IsReadyToCollect) UIManager.Instance.ShowWorkshopLaboratory();
        else UIManager.Instance.ShowWorkshopPanel();
    }

    private void Update()
    {
        if (!CraftingQueue.Instance.IsActive) return;
        UpdateFill(CraftingQueue.Instance.Progress);
    }

    private void LateUpdate()
    {
        if (!keepScreenSize) return;

        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;

        transform.localScale = _baseScale * (ScreenSizeCompensation() * Mathf.Max(0.01f, badgeScale));
    }

    private void CalibrateReference()
    {
        if (!calibrateReferenceOnStart) return;

        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;

        float height = CurrentViewHeight();
        if (height < 0.01f) return;

        _referenceViewHeight = height;
        _referenceCalibrated = true;
    }

    private float ScreenSizeCompensation()
    {
        float reference = ReferenceViewHeight();
        float current = CurrentViewHeight();
        if (reference < 0.01f || current < 0.01f) return 1f;

        return Mathf.Clamp(current / reference, minScreenScale, maxScreenScale);
    }

    private float CurrentViewHeight()
    {
        if (_camera.orthographic) return _camera.orthographicSize;

        float distance = Vector3.Distance(_camera.transform.position, transform.position);
        return distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    private float ReferenceViewHeight()
    {
        if (_referenceCalibrated) return _referenceViewHeight;

        return _camera.orthographic
            ? referenceOrthographicSize
            : referenceDistance * Mathf.Tan(referenceFieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    private void OnCraftStarted() => UpdateUI();

    private void OnCraftCompleted(ComponentType _) => UpdateUI();

    private void OnCraftCancelled() => UpdateUI();

    private void OnCraftRedeemed(ComponentType _) => UpdateUI();

    private void UpdateUI()
    {
        var q = CraftingQueue.Instance;
        bool show = q.IsActive || q.IsReadyToCollect;
        if (!show)
        {
            if (slider) DOTween.Kill(slider);
            currentFill = 0f;
            gameObject.SetActive(false);
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        bool ready = q.IsReadyToCollect;
        if (readyIcon) readyIcon.SetActive(ready);
        SetReadyText(ready);

        Sprite icon = q.CurrentData?.icon ?? GetIcon(q.CurrentType);
        SetIcon(icon);

        float fillTarget = ready ? 1f : q.Progress;
        UpdateFill(fillTarget);
    }

    private void SetReadyText(bool ready)
    {
        if (!readyText) return;
        readyText.text = readyMessage;
        readyText.gameObject.SetActive(ready);
    }

    private void SetIcon(Sprite sprite)
    {
        if (!componentIcon) return;
        componentIcon.sprite = sprite;
        componentIcon.enabled = sprite != null;
    }

    private Sprite GetIcon(ComponentType type)
    {
        if (workshopPanel == null) return null;
        return workshopPanel.GetComponentData(type)?.icon;
    }

    private void UpdateFill(float target)
    {
        if (!slider || Mathf.Approximately(currentFill, target)) return;

        DOTween.Kill(slider);
        slider.DOFillAmount(target, tweenDuration).SetEase(Ease.OutCubic);
        currentFill = target;
    }
}
