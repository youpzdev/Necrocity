using System;
using UnityEngine;

[Serializable]
public class ComponentSlot
{
    [SerializeField] private ComponentType componentType;
    [SerializeField] private int amount;

    public ComponentType ComponentType => componentType;
    public int Amount => amount;

    public ComponentSlot(ComponentType componentType, int amount)
    {
        this.componentType = componentType;
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