using System;
using UnityEngine;

[Serializable]
public class ItemSlot
{
    [SerializeField] private ItemType itemType;
    [SerializeField] private int amount;

    public ItemType ItemType => itemType;
    public int Amount => amount;

    public ItemSlot(ItemType itemType, int amount)
    {
        this.itemType = itemType;
        this.amount = amount;
    }

    public void AddAmount(int amount_) => amount += amount_;
    public void SetAmount(int amount_) => amount = amount_;
    public bool TrySpend(int amount_)
    {
        if (amount < amount_) return false;
        amount -= amount_;
        return true;
    }
}