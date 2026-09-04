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
    public int baseCharacterPrice = 10000;
    public int characterPriceStep = 10000;

    public int CharacterCount => characters != null ? characters.Length : 0;

    public int GetUpgradePrice(int currentLevel) => baseLevelPrice + levelPriceStep * (currentLevel - 1);
    public int GetCapacity(int level) => initialCapacity + capacityPerLevel * (level - 1);

    public int GetCharacterPrice(int purchasedCount)
    {
        if (purchasedCount < 0) purchasedCount = 0;
        return baseCharacterPrice + characterPriceStep * purchasedCount;
    }

    public bool HasCharacter(int index) => index >= 0 && index < CharacterCount && characters[index] != null;

    public CharacterData GetCharacter(int index) => HasCharacter(index) ? characters[index] : null;
}
