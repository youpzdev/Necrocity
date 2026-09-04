using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComponentsTab : BaseComponentTab
{
    [SerializeField] private Button sellButton;
    [SerializeField] private TMP_Text sellButtonText;
    [SerializeField] private GameObject amountPanel;
    [SerializeField] private TMP_Text amountText;

    [Header("Labels")]
    [SerializeField] private string dublonsLabel = "Дублонов";
    [SerializeField] private string loveLabel = "Любви";
    [SerializeField] private string sellFormat = "Продать за {0} {1}";
    [SerializeField] private string emptySellLabel = "Продать";
    [SerializeField] private string emptyTitle = "";
    [SerializeField] private string emptyDescription = "";

    private ComponentData selectedComponent;

    protected override void Awake()
    {
        base.Awake();
        sellButton.onClick.AddListener(SellComponent);
        ClearSelection();
    }

    protected override void UpdateUI()
    {
        base.UpdateUI();
        RefreshSelection();
    }

    private void SellComponent()
    {
        if (selectedComponent == null) return;

        bool sold = InventoryManager.Instance.TrySpendComponent(selectedComponent.type, 1);
        if (!sold) return;

        ResourceManager.Instance.AddResource(selectedComponent.SellPrice.ResourceType, selectedComponent.SellPrice.Amount);

        EventBus<InventoryChangedEvent>.Raise(new InventoryChangedEvent());
    }

    protected override void OnComponentClick(ComponentData data)
    {
        base.OnComponentClick(data);
        selectedComponent = data;
        if (iconImage != null) iconImage.enabled = true;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        if (selectedComponent == null)
        {
            ClearSelection();
            return;
        }

        int amount = InventoryManager.Instance.GetComponent(selectedComponent.type);
        if (amount <= 0)
        {
            ClearSelection();
            return;
        }

        amountPanel.SetActive(true);
        amountText.text = amount.ToString();

        string sellReward = selectedComponent.SellPrice.ResourceType == ResourceType.Dublons ? dublonsLabel : loveLabel;
        sellButtonText.text = string.Format(sellFormat, selectedComponent.SellPrice.Amount, sellReward);
        sellButton.interactable = true;
    }

    private void ClearSelection()
    {
        selectedComponent = null;
        isChoosen = false;

        if (titleText != null) titleText.text = emptyTitle;
        if (descText != null) descText.text = emptyDescription;
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        amountPanel.SetActive(false);
        amountText.text = string.Empty;
        sellButtonText.text = emptySellLabel;
        sellButton.interactable = false;
    }

    protected override ComponentData[] GetComponentDatas()
    {
        var all = workshopPanel.GetAllComponentDatas;
        var result = new List<ComponentData>();

        foreach (ComponentRarity rarity in System.Enum.GetValues(typeof(ComponentRarity)))
        {
            foreach (var data in all)
            {
                if (data.rarity != rarity) continue;
                if (InventoryManager.Instance.GetComponent(data.type) > 0)
                    result.Add(data);
            }
        }

        return result.ToArray();
    }

    protected override int GetAmount(ComponentData data) => InventoryManager.Instance.GetComponent(data.type);

}
