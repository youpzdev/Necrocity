using System;
using UnityEngine;

public enum TutorialConditionKind
{
    Immediate,
    Signal,
    BusEvent
}

public enum TutorialBusEvent
{
    ResourceManagerChanged,
    ResourcesChanged,
    InventoryChanged,
    LevelChanged,
    DormitoryChanged,
    CharacterPurchased
}

[Serializable]
public class TutorialCondition
{
    [SerializeField] private TutorialConditionKind kind = TutorialConditionKind.Signal;
    [SerializeField] private string signal;
    [SerializeField] private TutorialBusEvent busEvent;

    public TutorialConditionKind Kind => kind;
    public string Signal => signal;
    public TutorialBusEvent BusEvent => busEvent;
}
