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

    private WorkshopPanel _workshopPanel;
    private ItemData _selected;

    private void Awake() =>
        _workshopPanel = GetComponentInParent<WorkshopPanel>();

    private void Start() => UpdateUI();

    private void UpdateUI()
    {
        foreach (Transform old in itemGrid) Pooling.Destroy(old.gameObject);

        foreach (var data in _workshopPanel.GetItemDatas)
        {
            WorkshopComponent obj = Pooling.Instantiate(itemPrefab, itemGrid)
                                           .GetComponent<WorkshopComponent>();
            obj.Init(
                icon: data.icon,
                title: data.name,
                clickAction: () => OnItemClick(data),
                itemData: data
            );
        }
    }

    private void OnItemClick(ItemData data)
    {
        _selected = data;

        foreach (Transform old in recipeGrid) Pooling.Destroy(old.gameObject);

        titleText.text = data.name;
        iconImage.sprite = data.icon;

        foreach (var ingredient in data.recipe)
        {
            var compData = _workshopPanel.GetComponentData(ingredient.component);
            if (compData == null)
            {
                Debug.LogError($"ComponentData not found: {ingredient.component}", this);
                continue;
            }

            WorkshopComponent slot = Pooling.Instantiate(recipePrefab, recipeGrid)
                                            .GetComponent<WorkshopComponent>();
            slot.Init(icon: compData.icon, amount: ingredient.amount);
        }

        RefreshCraftButton();
    }

    private void RefreshCraftButton()
    {
        if (_selected == null)
        {
            craftButton.interactable = false;
            return;
        }

        bool canCraft = CanCraft(_selected);
        craftButton.interactable = canCraft;
        craftButtonText.text = canCraft ? "Скрафтить" : "Не хватает ресурсов";

        craftButton.onClick.RemoveAllListeners();
        craftButton.onClick.AddListener(OnCraftClick);
    }

    private void OnCraftClick()
    {
        if (_selected == null || !CanCraft(_selected)) return;

        foreach (var ingredient in _selected.recipe)
            InventoryManager.Instance.TrySpendComponent(ingredient.component, ingredient.amount);

        InventoryManager.Instance.AddItem(_selected.type, 1);

        UpdateUI();
        RefreshCraftButton();
    }

    private bool CanCraft(ItemData data)
    {
        foreach (var ingredient in data.recipe)
            if (!InventoryManager.Instance.CanSpendComponent(ingredient.component, ingredient.amount))
                return false;
        return true;
    }
}