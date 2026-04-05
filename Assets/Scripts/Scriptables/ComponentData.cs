using UnityEngine;

[CreateAssetMenu(menuName = "youpzdev/crafting/component")]
public class ComponentData : ScriptableObject
{
    public ComponentType type;
    public Sprite icon;
    [TextArea(5,10)] public string description;
    public float craftDuration;

    public ResourceSlot SellPrice;
    
}
