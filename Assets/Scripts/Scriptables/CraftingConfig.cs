using System;
using UnityEngine;

[CreateAssetMenu(menuName = "youpzdev/crafting/cfg")]
public class CraftingConfig : ScriptableObject
{
    [SerializeField] private ComponentData[] components;
    [SerializeField] private ItemData[] items;

    private ComponentData[] _componentCache;
    private ItemData[] _itemCache;

    void OnEnable()
    {
        _componentCache = new ComponentData[Enum.GetValues(typeof(ComponentType)).Length];
        _itemCache = new ItemData[Enum.GetValues(typeof(ItemType)).Length];
        foreach (var c in components) _componentCache[(int)c.type] = c;
        foreach (var i in items) _itemCache[(int)i.type] = i;
    }

    public ComponentData GetComponentData(ComponentType type)
    {
        var index = (int)type;
        if (index >= _componentCache.Length) return null;
        return _componentCache[index];
    }
    
    public ItemData GetItemData(ItemType type) => _itemCache[(int)type];

    public ItemData[] GetAllItems() => items;
    public ComponentData[] GetAllComponents() => components;
    
}