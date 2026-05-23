using UnityEngine;

[CreateAssetMenu(fileName = "DormitoryConfig", menuName = "youpzdev/dormitory/Dormitory Config")]
public class DormitoryConfig : ScriptableObject
{
    [Header("Upgrade")]
    public int initialCapacity = 5;
    public int capacityPerLevel = 2;
    public int baseLevelPrice = 5000;
    public int levelPriceStep = 5000;
    public int maxLevel = 60;

    [Header("Characters")]
    public CharacterData[] characters;

    public int GetUpgradePrice(int currentLevel) => baseLevelPrice + levelPriceStep * (currentLevel - 1);
    public int GetCapacity(int level) => initialCapacity + capacityPerLevel * (level - 1);

}