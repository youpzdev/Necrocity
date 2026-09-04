using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LabTab : BaseComponentTab
{
    [Header("Craft UI")]
    [SerializeField] private Button craftButton;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text craftButtonText;
    [SerializeField] private GameObject gridLockOverlay;

    [Header("Craft Labels")]
    [SerializeField] private string craftingLabel = "Крафтится...";
    [SerializeField] private string redeemLabel = "Забрать";
    [SerializeField] private string craftLabel = "Крафт";

    private ComponentData _selected;
    private bool _subscribed;

    protected override void Start()
    {
        base.Start();

        Subscribe();

        if (CraftingQueue.Instance != null &&
            (CraftingQueue.Instance.IsActive || CraftingQueue.Instance.IsReadyToCollect))
            RestoreActiveState();

        RefreshCraftUI();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Subscribe();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (_subscribed || CraftingQueue.Instance == null) return;

        CraftingQueue.Instance.OnCraftStarted += OnCraftStarted;
        CraftingQueue.Instance.OnCraftCompleted += OnCraftCompleted;
        CraftingQueue.Instance.OnCraftCancelled += RefreshCraftUI;
        CraftingQueue.Instance.OnCraftRedeemed += OnCraftRedeemed;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        _subscribed = false;

        if (CraftingQueue.Instance == null) return;
        CraftingQueue.Instance.OnCraftStarted -= OnCraftStarted;
        CraftingQueue.Instance.OnCraftCompleted -= OnCraftCompleted;
        CraftingQueue.Instance.OnCraftCancelled -= RefreshCraftUI;
        CraftingQueue.Instance.OnCraftRedeemed -= OnCraftRedeemed;
    }

    private void Update()
    {
        if (CraftingQueue.Instance == null || !CraftingQueue.Instance.IsActive) return;
        var q = CraftingQueue.Instance;
        progressText.text = $"{FormatTime(q.Elapsed)} / {FormatTime(q.Duration)}";
    }

    protected override ComponentData[] GetComponentDatas() =>
        workshopPanel.GetAllComponentDatas;

    protected override int GetAmount(ComponentData data) =>
        InventoryManager.Instance.GetComponent(data.type);

    protected override void OnComponentClick(ComponentData data)
    {
        if (CraftingQueue.Instance.IsActive) return;

        base.OnComponentClick(data);
        _selected = data;
        RefreshCraftUI();
    }

    private void OnCraftStarted() => RefreshCraftUI();

    private void OnCraftCompleted(ComponentType _)
    {
        UpdateUI();
        RefreshCraftUI();
    }

    private void OnCraftRedeemed(ComponentType _)
    {
        UpdateUI();
        RefreshCraftUI();
    }

    private void RestoreActiveState()
    {
        var data = workshopPanel.GetComponentData(CraftingQueue.Instance.CurrentType);
        if (data == null) return;
        _selected = data;
        base.OnComponentClick(data);
    }

    private void RefreshCraftUI()
    {
        var q = CraftingQueue.Instance;
        if (q == null) return;

        bool active = q.IsActive;
        bool ready = q.IsReadyToCollect;

        progressText.gameObject.SetActive(active);
        if (gridLockOverlay != null) gridLockOverlay.SetActive(active || ready);

        craftButton.onClick.RemoveAllListeners();

        if (active)
        {
            progressText.text = $"{FormatTime(q.Elapsed)} / {FormatTime(q.Duration)}";
            craftButton.interactable = false;
            craftButtonText.text = craftingLabel;
        }
        else if (ready)
        {
            craftButton.interactable = true;
            craftButtonText.text = redeemLabel;
            craftButton.onClick.AddListener(OnRedeemClick);
        }
        else
        {
            craftButton.interactable = _selected != null;
            craftButtonText.text = craftLabel;
            craftButton.onClick.AddListener(OnCraftClick);
        }
    }

    private void OnRedeemClick()
    {
        CraftingQueue.Instance.Redeem();
    }

    private void OnCraftClick()
    {
        if (_selected == null) return;

        var result = CraftingQueue.Instance.TryStartCraft(_selected);
        if (result != CraftStartResult.Started) RefreshCraftUI();
    }

    private static string FormatTime(float seconds)
    {
        var ts = System.TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}
