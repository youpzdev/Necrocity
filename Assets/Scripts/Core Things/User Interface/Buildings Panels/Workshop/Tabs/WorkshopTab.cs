using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorkshopTab : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Transform itemGrid;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform recipeGrid;
    [SerializeField] private GameObject recipePrefab;
    [SerializeField] private Button craftButton;
    [SerializeField] private TMP_Text craftButtonText;
    [SerializeField] private ModelPreview modelPreview;

    [Header("Labels")]
    [SerializeField] private string craftLabel = "Скрафтить";
    [SerializeField] private string notEnoughLabel = "Не хватает ресурсов";

    [Header("Empty State")]
    [SerializeField] private string emptyTitle = "Ничего не выбрано";
    [SerializeField] private string emptyCraftLabel = "Выберите предмет";

    private WorkshopPanel _workshopPanel;
    private ItemData _selected;

    private void Awake()
    {
        _workshopPanel = GetComponentInParent<WorkshopPanel>(true);
        craftButton.onClick.AddListener(OnCraftClick);
        ClearSelection();
    }

    private void UpdateUI()
    {
        for (int i = itemGrid.childCount - 1; i >= 0; i--)
            Pooling.Destroy(itemGrid.GetChild(i).gameObject);

        foreach (var data in _workshopPanel.GetItemDatas)
        {
            WorkshopComponent obj = Pooling.Instantiate(itemPrefab, itemGrid)
                                           .GetComponent<WorkshopComponent>();
            obj.Init(
                icon: data.icon,
                title: data.name,
                clickAction: () => OnItemClick(data),
                itemData: data,
                available: CanCraft(data)
            );
        }
    }

    private void OnItemClick(ItemData data)
    {
        _selected = data;

        titleText.text = data.name;
        ApplyIcon(data.icon);
        if (modelPreview != null) modelPreview.Show(data.prefab3D);

        BuildRecipe();
        RefreshCraftButton();
    }

    private void ClearSelection()
    {
        _selected = null;

        if (titleText != null) titleText.text = emptyTitle;
        ApplyIcon(null);
        if (modelPreview != null) modelPreview.Clear();

        BuildRecipe();
        RefreshCraftButton();
    }

    private void ApplyIcon(Sprite icon)
    {
        if (iconImage == null) return;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }

    private void BuildRecipe()
    {
        for (int i = recipeGrid.childCount - 1; i >= 0; i--)
            Pooling.Destroy(recipeGrid.GetChild(i).gameObject);

        if (_selected == null) return;

        foreach (var ingredient in _selected.recipe)
        {
            var compData = _workshopPanel.GetComponentData(ingredient.component);
            if (compData == null)
            {
                Debug.LogError($"ComponentData not found: {ingredient.component}", this);
                continue;
            }

            int owned = InventoryManager.Instance.GetComponent(ingredient.component);

            WorkshopComponent slot = Pooling.Instantiate(recipePrefab, recipeGrid)
                                            .GetComponent<WorkshopComponent>();
            slot.Init(
                icon: compData.icon,
                amount: owned,
                available: owned >= ingredient.amount,
                requiredAmount: ingredient.amount
            );
        }
    }

    private void RefreshCraftButton()
    {
        if (_selected == null)
        {
            craftButton.interactable = false;
            craftButtonText.text = emptyCraftLabel;
            return;
        }

        bool canCraft = CanCraft(_selected);
        craftButton.interactable = canCraft;
        craftButtonText.text = canCraft ? craftLabel : notEnoughLabel;
    }

    private void OnCraftClick()
    {
        if (_selected == null || !CanCraft(_selected)) return;

        foreach (var ingredient in _selected.recipe)
            InventoryManager.Instance.TrySpendComponent(ingredient.component, ingredient.amount);

        InventoryManager.Instance.AddItem(_selected.type, 1);

        EventBus<InventoryChangedEvent>.Raise(new InventoryChangedEvent());
    }

    private bool CanCraft(ItemData data)
    {
        foreach (var ingredient in data.recipe)
            if (!InventoryManager.Instance.CanSpendComponent(ingredient.component, ingredient.amount))
                return false;
        return true;
    }

    private void OnEnable()
    {
        EventBus<InventoryChangedEvent>.Subscribe(OnInventoryChanged, this);
        ClearSelection();
        UpdateUI();
    }

    private void OnDisable()
    {
        EventBus<InventoryChangedEvent>.Unsubscribe(OnInventoryChanged);
    }

    private void OnInventoryChanged(InventoryChangedEvent _)
    {
        UpdateUI();
        BuildRecipe();
        RefreshCraftButton();
    }
}
