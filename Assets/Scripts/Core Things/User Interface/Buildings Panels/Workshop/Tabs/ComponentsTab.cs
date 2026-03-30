
using UnityEngine;
public class ComponentsTab : BaseComponentTab
{
    protected override ComponentData[] GetComponentDatas()
    {
        var all = workshopPanel.GetAllComponentDatas;
        var result = new System.Collections.Generic.List<ComponentData>();

        foreach (var data in all)
        {
            int amount = InventoryManager.Instance.GetComponent(data.type);
            if (amount > 0)
                result.Add(data);
        }

        return result.ToArray();
    }

    protected override int GetAmount(ComponentData data) =>
        InventoryManager.Instance.GetComponent(data.type);
}
