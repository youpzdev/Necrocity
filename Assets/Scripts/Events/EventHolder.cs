public struct ResourceManagerChangedEvent { }

public struct InventoryChangedEvent { }

public struct ResourcesChangedEvent
{
    public ResourceGainer Gainer;
}

public struct LevelChangedEvent
{
    public ResourceGainer Gainer;
}

public struct DormitoryChangedEvent { }

public struct CharacterPurchasedEvent
{
    public CharacterData Data;
    public int ResidentCount;
    public int Capacity;
}
