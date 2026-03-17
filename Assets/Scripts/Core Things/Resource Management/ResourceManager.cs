using System;
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
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public int GetResourceAmount(ResourceType resourceType)
    {
        ResourceSlot slot = resourceSlots.Find(rs => rs.ResourceType == resourceType);
        return slot != null ? slot.Amount : 0;
    }

    public void AddResource(ResourceType resourceType, int amount)
    {
        ResourceSlot slot = resourceSlots.Find(rs => rs.ResourceType == resourceType);
        if (slot != null)
        {
            slot.AddAmount(amount);
        }
        else
        {
            resourceSlots.Add(new ResourceSlot(resourceType, amount));
        }

        EventBus<ResourceManagerChangedEvent>.Raise(new ResourceManagerChangedEvent());
    }

    public bool SpendResource(ResourceType resourceType, int amount)
    {
        ResourceSlot slot = resourceSlots.Find(rs => rs.ResourceType == resourceType);
        if (slot != null && slot.TrySpend(amount))
        {
            EventBus<ResourceManagerChangedEvent>.Raise(new ResourceManagerChangedEvent());
            return true;
        }

        return false;
    }

    public bool CanSpendResource(ResourceType resourceType, int amount)
    {
        ResourceSlot slot = resourceSlots.Find(rs => rs.ResourceType == resourceType);

        if (slot != null && slot.Amount >= amount) return true;
        return false;
    }



}