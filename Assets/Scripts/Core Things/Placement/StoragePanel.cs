using TMPro;
using UnityEngine;

public class StoragePanel : MonoBehaviour
{
    [SerializeField] private UIPanel panel;
    [SerializeField] private CraftingConfig craftingConfig;
    [SerializeField] private Transform itemGrid;
    [SerializeField] private GameObject itemPrefab;

    [Header("Empty State")]
    [SerializeField] private GameObject emptyState;
    [SerializeField] private TMP_Text emptyText;
    [SerializeField] private string emptyLabel = "На складе пусто";

    private void Awake()
    {
        if (panel == null) panel = GetComponent<UIPanel>();
        if (emptyText != null) emptyText.text = emptyLabel;
    }

    private void OnEnable()
    {
        EventBus<InventoryChangedEvent>.Subscribe(OnInventoryChanged, this);
        Rebuild();
    }

    private void OnDisable()
    {
        EventBus<InventoryChangedEvent>.Unsubscribe(OnInventoryChanged);
    }

    public void Open()
    {
        if (UIManager.Instance != null && UIManager.Instance.AreModalWindowOpened()) return;
        if (PlacementManager.Instance != null && PlacementManager.Instance.IsPlacing) return;

        panel.Show();
        Rebuild();
    }

    public void Close() => panel.Hide();

    private void Rebuild()
    {
        if (itemGrid == null || InventoryManager.Instance == null) return;

        for (int i = itemGrid.childCount - 1; i >= 0; i--)
            Pooling.Destroy(itemGrid.GetChild(i).gameObject);

        int shown = 0;

        foreach (var slot in InventoryManager.Instance.GetAllItems())
        {
            if (slot.Amount <= 0) continue;

            ItemData data = craftingConfig.GetItemData(slot.ItemType);
            if (data == null) continue;

            ItemType type = slot.ItemType;
            WorkshopComponent cell = Pooling.Instantiate(itemPrefab, itemGrid)
                                           .GetComponent<WorkshopComponent>();
            cell.Init(
                icon: data.icon,
                title: data.name,
                amount: slot.Amount,
                clickAction: () => OnItemClick(type)
            );

            shown++;
        }

        if (emptyState != null) emptyState.SetActive(shown == 0);
    }

    private void OnItemClick(ItemType type)
    {
        if (PlacementManager.Instance == null) return;
        if (!PlacementManager.Instance.BeginPlacement(type)) return;

        Close();
    }

    private void OnInventoryChanged(InventoryChangedEvent _) => Rebuild();
}
