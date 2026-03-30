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

    private ComponentData _selected;

    protected override void Start()
    {
        base.Start();

        CraftingQueue.Instance.OnCraftStarted += OnCraftStarted;
        CraftingQueue.Instance.OnCraftCompleted += OnCraftCompleted;
        CraftingQueue.Instance.OnCraftCancelled += RefreshCraftUI;

        if (CraftingQueue.Instance.IsActive)
            RestoreActiveState();

        RefreshCraftUI();
    }

    private void OnDestroy()
    {
        if (CraftingQueue.Instance == null) return;
        CraftingQueue.Instance.OnCraftStarted -= OnCraftStarted;
        CraftingQueue.Instance.OnCraftCompleted -= OnCraftCompleted;
        CraftingQueue.Instance.OnCraftCancelled -= RefreshCraftUI;
    }

    private void Update()
    {
        if (!CraftingQueue.Instance.IsActive) return;
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

    private void OnCraftStarted()
    {
        RefreshCraftUI();
    }

    private void OnCraftCompleted(ComponentType _)
    {
        UpdateUI();
        RefreshCraftUI();
    }

    // ─── State ─────────────────────────────────────────────────────

    private void RestoreActiveState()
    {
        var data = workshopPanel.GetComponentData(CraftingQueue.Instance.CurrentType);
        if (data == null) return;

        _selected = data;

        base.OnComponentClick(data);
    }

    // ─── Craft UI ──────────────────────────────────────────────────

    private void RefreshCraftUI()
    {
        var q = CraftingQueue.Instance;
        bool active = q.IsActive;

        progressText.gameObject.SetActive(active);


        if (gridLockOverlay != null)
            gridLockOverlay.SetActive(active);

        if (active)
        {
            progressText.text = $"{FormatTime(q.Elapsed)} / {FormatTime(q.Duration)}";
            craftButton.interactable = false;
            craftButtonText.text = "Крафтится...";
        }
        else
        {
            craftButton.interactable = _selected != null;
            craftButtonText.text = "Крафт";
        }

        craftButton.onClick.RemoveAllListeners();
        craftButton.onClick.AddListener(OnCraftClick);
    }

    private void OnCraftClick()
    {
        if (_selected == null) return;
        CraftingQueue.Instance.TryStartCraft(_selected);
    }

    private static string FormatTime(float seconds)
    {
        var ts = System.TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}