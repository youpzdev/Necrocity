using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseComponentTab : MonoBehaviour
{
    [SerializeField] protected TMP_Text titleText;
    [SerializeField] protected TMP_Text descText;
    [SerializeField] protected Image iconImage;
    [SerializeField] protected Transform componentGrid;
    [SerializeField] protected GameObject componentPrefab;

    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = new Color(0.36f, 0.68f, 1f, 1f);
    [SerializeField] private Color rareColor = new Color(0.65f, 0.4f, 1f, 1f);
    [SerializeField] private Color epicColor = new Color(1f, 0.82f, 0.25f, 1f);

    [Header("Empty State")]
    [SerializeField] protected string emptyTitle = "Ничего не выбрано";
    [SerializeField] protected string emptyDescription = "Выберите компонент в списке слева";

    protected WorkshopPanel workshopPanel;
    [SerializeField] protected bool isChoosen = false;

    protected virtual void Awake()
    {
        workshopPanel = GetComponentInParent<WorkshopPanel>(true);
        ShowEmptyInfo();
    }

    protected virtual void Start() => UpdateUI();

    protected virtual void UpdateUI()
    {
        for (int i = componentGrid.childCount - 1; i >= 0; i--)
            Pooling.Destroy(componentGrid.GetChild(i).gameObject);

        foreach (var compData in GetComponentDatas())
        {
            int amount = GetAmount(compData);
            WorkshopComponent obj = Pooling.Instantiate(componentPrefab, componentGrid)
                                           .GetComponent<WorkshopComponent>();
            obj.Init(
                icon: compData.icon,
                title: compData.name,
                amount: amount,
                clickAction: () => OnComponentClick(compData),
                rarityColor: GetRarityColor(compData.rarity)
            );
        }
    }

    protected Color GetRarityColor(ComponentRarity rarity)
    {
        switch (rarity)
        {
            case ComponentRarity.Rare: return rareColor;
            case ComponentRarity.Epic: return epicColor;
            default: return commonColor;
        }
    }

    protected abstract ComponentData[] GetComponentDatas();
    protected abstract int GetAmount(ComponentData data);

    protected void ApplyIcon(Sprite icon)
    {
        if (iconImage == null) return;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }

    protected void ShowEmptyInfo()
    {
        if (titleText != null) titleText.text = emptyTitle;
        if (descText != null) descText.text = emptyDescription;
        ApplyIcon(null);
        isChoosen = false;
    }

    protected virtual void OnComponentClick(ComponentData data)
    {
        titleText.text = data.name;
        if (descText != null) descText.text = data.description;
        ApplyIcon(data.icon);
        isChoosen = true;
    }

    protected virtual void OnEnable()
    {
        EventBus<InventoryChangedEvent>.Subscribe(OnInventoryChanged, this);
        UpdateUI();
    }

    protected virtual void OnDisable()
    {
        EventBus<InventoryChangedEvent>.Unsubscribe(OnInventoryChanged);
    }

    private void OnInventoryChanged(InventoryChangedEvent _)
    {
        UpdateUI();
    }
}
