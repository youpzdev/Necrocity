using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComponentsTab : BaseComponentTab
{
    [SerializeField] private Button sellButton;
    [SerializeField] private TMP_Text sellButtonText;
    [SerializeField] private GameObject amountPanel;
    [SerializeField] private TMP_Text amountText;

    private ComponentData selectedComponent;

    protected override void Awake()
    {
        base.Awake();
        sellButton.onClick.AddListener(SellComponent);
    }

    private void SellComponent()
    {
        if (selectedComponent == null) return;

        bool sold = InventoryManager.Instance.TrySpendComponent(selectedComponent.type, 1);
        if (sold)
        {
            Debug.Log($"Sold: {selectedComponent.name}");
            ResourceManager.Instance.AddResource(selectedComponent.SellPrice.ResourceType, selectedComponent.SellPrice.Amount);
            UpdateUI();

            int remaining = InventoryManager.Instance.GetComponent(selectedComponent.type);
            amountPanel.SetActive(remaining > 1);
            amountText.text = remaining.ToString();
        }
    }

    protected override void OnComponentClick(ComponentData data)
    {
        base.OnComponentClick(data);
        selectedComponent = data;

        int amount = InventoryManager.Instance.GetComponent(data.type);
        amountPanel.SetActive(amount > 1);
        amountText.text = amount.ToString();
        string sellReward = data.SellPrice.ResourceType == ResourceType.Dublons ? "Дублонов" : "Любви";
        sellButtonText.text = $"Продать за {data.SellPrice.Amount} {sellReward}";
    }

    protected override ComponentData[] GetComponentDatas()
    {
        var all = workshopPanel.GetAllComponentDatas;
        var result = new System.Collections.Generic.List<ComponentData>();

        foreach (var data in all)
        {
            if (InventoryManager.Instance.GetComponent(data.type) > 0)
                result.Add(data);
        }

        return result.ToArray();
    }

    protected override int GetAmount(ComponentData data) => InventoryManager.Instance.GetComponent(data.type);

}