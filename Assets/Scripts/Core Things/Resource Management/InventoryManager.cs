using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private List<ComponentSlot> componentSlots = new();
    [SerializeField] private List<ItemSlot> itemSlots = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Load();
    }

    private void Start()
    {
        StartCoroutine(RaiseChangedNextFrame());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int GetComponent(ComponentType type) =>
        componentSlots.Find(s => s.ComponentType == type)?.Amount ?? 0;

    public int GetItem(ItemType type) =>
        itemSlots.Find(s => s.ItemType == type)?.Amount ?? 0;

    public void AddComponent(ComponentType type, int amount)
    {
        GetOrCreateComponentSlot(type).AddAmount(amount);
        SaveInventory();
        EventBus<InventoryChangedEvent>.Raise(new InventoryChangedEvent());
    }

    public void AddItem(ItemType type, int amount)
    {
        GetOrCreateItemSlot(type).AddAmount(amount);
        SaveInventory();
        EventBus<InventoryChangedEvent>.Raise(new InventoryChangedEvent());
    }

    public bool TrySpendComponent(ComponentType type, int amount)
    {
        if (!GetOrCreateComponentSlot(type).TrySpend(amount)) return false;

        SaveInventory();
        EventBus<InventoryChangedEvent>.Raise(new InventoryChangedEvent());
        return true;
    }

    public bool CanSpendComponent(ComponentType type, int amount) => GetComponent(type) >= amount;

    public List<ItemSlot> GetAllItems() => itemSlots;
    public List<ComponentSlot> GetAllComponents() => componentSlots;

    private ComponentSlot GetOrCreateComponentSlot(ComponentType type)
    {
        var slot = componentSlots.Find(s => s.ComponentType == type);
        if (slot == null)
        {
            slot = new ComponentSlot(type, 0);
            componentSlots.Add(slot);
        }

        return slot;
    }

    private ItemSlot GetOrCreateItemSlot(ItemType type)
    {
        var slot = itemSlots.Find(s => s.ItemType == type);
        if (slot == null)
        {
            slot = new ItemSlot(type, 0);
            itemSlots.Add(slot);
        }

        return slot;
    }

    private void SaveInventory()
    {
        var components = new Dictionary<string, int>();
        foreach (var slot in componentSlots) components[slot.ComponentType.ToString()] = slot.Amount;
        GameSave.Set(GameSave.Keys.Components, components);

        var items = new Dictionary<string, int>();
        foreach (var slot in itemSlots) items[slot.ItemType.ToString()] = slot.Amount;
        GameSave.Set(GameSave.Keys.Items, items);
    }

    private void Load()
    {
        var components = GameSave.Get<Dictionary<string, int>>(GameSave.Keys.Components, null);
        if (components != null)
        {
            foreach (var pair in components)
            {
                if (!Enum.TryParse(pair.Key, out ComponentType type)) continue;
                if (!Enum.IsDefined(typeof(ComponentType), type)) continue;
                GetOrCreateComponentSlot(type).SetAmount(pair.Value);
            }
        }

        var items = GameSave.Get<Dictionary<string, int>>(GameSave.Keys.Items, null);
        if (items == null) return;

        foreach (var pair in items)
        {
            if (!Enum.TryParse(pair.Key, out ItemType type)) continue;
            if (!Enum.IsDefined(typeof(ItemType), type)) continue;
            GetOrCreateItemSlot(type).SetAmount(pair.Value);
        }
    }

    private IEnumerator RaiseChangedNextFrame()
    {
        yield return null;
        EventBus<InventoryChangedEvent>.Raise(new InventoryChangedEvent());
    }
}
