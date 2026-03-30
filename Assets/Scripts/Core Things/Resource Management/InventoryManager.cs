using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private List<ComponentSlot> componentSlots = new();
    [SerializeField] private List<ItemSlot> itemSlots = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public int GetComponent(ComponentType type) =>
        componentSlots.Find(s => s.ComponentType == type)?.Amount ?? 0;

    public int GetItem(ItemType type) =>
        itemSlots.Find(s => s.ItemType == type)?.Amount ?? 0;

    public void AddComponent(ComponentType type, int amount)
    {
        var slot = componentSlots.Find(s => s.ComponentType == type);
        if (slot != null) slot.AddAmount(amount);
        else componentSlots.Add(new ComponentSlot(type, amount));
        EventBus<ResourceManagerChangedEvent>.Raise(new ResourceManagerChangedEvent());
    }

    public void AddItem(ItemType type, int amount)
    {
        var slot = itemSlots.Find(s => s.ItemType == type);
        if (slot != null) slot.AddAmount(amount);
        else itemSlots.Add(new ItemSlot(type, amount));
        EventBus<ResourceManagerChangedEvent>.Raise(new ResourceManagerChangedEvent());
    }

    public bool TrySpendComponent(ComponentType type, int amount)
    {
        var slot = componentSlots.Find(s => s.ComponentType == type);
        if (slot == null || !slot.TrySpend(amount)) return false;
        EventBus<ResourceManagerChangedEvent>.Raise(new ResourceManagerChangedEvent());
        return true;
    }

    public bool CanSpendComponent(ComponentType type, int amount) =>
        (componentSlots.Find(s => s.ComponentType == type)?.Amount ?? 0) >= amount;

    public List<ItemSlot> GetAllItems() => itemSlots;
    public List<ComponentSlot> GetAllComponents() => componentSlots;
}