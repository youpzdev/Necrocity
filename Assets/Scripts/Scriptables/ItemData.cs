using System;
using UnityEngine;

[CreateAssetMenu(menuName = "youpzdev/crafting/item")]
public class ItemData : ScriptableObject
{
    [Serializable]
    public struct Ingredient
    {
        public ComponentType component;
        public int amount;
    }

    public ItemType type;
    public Sprite icon;
    [TextArea(5,10)] public string description;
    public Ingredient[] recipe;
}