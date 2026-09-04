using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }
    [SerializeField] private List<ResourceSlot> resourceSlots = new List<ResourceSlot>();


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Load();
    }

    void Start()
    {
        StartCoroutine(RaiseChangedNextFrame());
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int GetResourceAmount(ResourceType resourceType)
    {
        ResourceSlot slot = resourceSlots.Find(rs => rs.ResourceType == resourceType);
        return slot != null ? slot.Amount : 0;
    }

    public void AddResource(ResourceType resourceType, int amount)
    {
        GetOrCreateSlot(resourceType).AddAmount(amount);
        SaveResources();
        EventBus<ResourceManagerChangedEvent>.Raise(new ResourceManagerChangedEvent());
    }

    public bool SpendResource(ResourceType resourceType, int amount)
    {
        if (!GetOrCreateSlot(resourceType).TrySpend(amount)) return false;

        SaveResources();
        EventBus<ResourceManagerChangedEvent>.Raise(new ResourceManagerChangedEvent());
        return true;
    }

    public bool CanSpendResource(ResourceType resourceType, int amount) =>
        GetResourceAmount(resourceType) >= amount;

    ResourceSlot GetOrCreateSlot(ResourceType resourceType)
    {
        ResourceSlot slot = resourceSlots.Find(rs => rs.ResourceType == resourceType);
        if (slot == null)
        {
            slot = new ResourceSlot(resourceType, 0);
            resourceSlots.Add(slot);
        }

        return slot;
    }

    void SaveResources()
    {
        var stored = new Dictionary<string, int>();
        foreach (var slot in resourceSlots) stored[slot.ResourceType.ToString()] = slot.Amount;
        GameSave.Set(GameSave.Keys.Resources, stored);
    }

    void Load()
    {
        var stored = GameSave.Get<Dictionary<string, int>>(GameSave.Keys.Resources, null);
        if (stored == null) return;

        foreach (var pair in stored)
        {
            if (!Enum.TryParse(pair.Key, out ResourceType type)) continue;
            if (!Enum.IsDefined(typeof(ResourceType), type)) continue;
            GetOrCreateSlot(type).SetAmount(pair.Value);
        }
    }

    IEnumerator RaiseChangedNextFrame()
    {
        yield return null;
        EventBus<ResourceManagerChangedEvent>.Raise(new ResourceManagerChangedEvent());
    }
}
