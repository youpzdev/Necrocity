using System.Collections.Generic;
using UnityEngine;

public class Dormitory : MonoBehaviour, IClickableBuilding
{
    private const string KeyLevel = "dormitory_level";
    private const string KeyResidents = "dormitory_residents";

    [SerializeField] private DormitoryConfig config;

    private int _level = 1;
    private List<int> _residentIndices = new();  // индексы купленных CharacterData

    public int Level => _level;
    public int ResidentCount => _residentIndices.Count;
    public int Capacity => config.GetCapacity(_level);
    public bool IsFull => ResidentCount >= Capacity;
    public DormitoryConfig Config => config;

    private void Awake()
    {
        Load();
    }

    // ─── Публичное ───────────────────────────────────────────────────────────

    public bool TryUpgrade()
    {
        if (_level >= config.maxLevel) return false;

        int price = config.GetUpgradePrice(_level);
        if (!ResourceManager.Instance.SpendResource(ResourceType.Dublons, price)) return false;

        _level++;
        SaveData();
        EventBus<DormitoryChangedEvent>.Raise(new DormitoryChangedEvent());
        EventBus<LevelChangedEvent>.Raise(new LevelChangedEvent { Gainer = null });
        return true;
    }

    public bool TryPurchaseCharacter(int characterIndex)
    {
        if (IsFull) return false;
        if (_residentIndices.Contains(characterIndex)) return false;

        CharacterData data = config.characters[characterIndex];
        if (!ResourceManager.Instance.SpendResource(ResourceType.Dublons, data.price)) return false;

        _residentIndices.Add(characterIndex);
        SaveData();

        EventBus<CharacterPurchasedEvent>.Raise(new CharacterPurchasedEvent
        {
            Data = data,
            ResidentCount = ResidentCount,
            Capacity = Capacity
        });
        EventBus<DormitoryChangedEvent>.Raise(new DormitoryChangedEvent());
        return true;
    }

    public bool IsCharacterOwned(int characterIndex) => _residentIndices.Contains(characterIndex);

    public int GetCharacterPrice(int index)
    {
        if (index == 0) return config.characters[0].price;
        // каждый следующий на 10к дороже предыдущего
        return config.characters[0].price + 10000 * index;
    }

    public void OnClick()
    {
        UIManager.Instance.ShowDormitoryPanel(this);
    }

    // ─── Save / Load ──────────────────────────────────────────────────────────

    private void SaveData()
    {
        Save.Set(KeyLevel, _level);
        Save.Set(KeyResidents, string.Join(",", _residentIndices));
    }

    private void Load()
    {
        _level = Save.Get(KeyLevel, 1);

        string raw = Save.Get(KeyResidents, "");
        _residentIndices.Clear();
        if (!string.IsNullOrEmpty(raw))
        {
            foreach (var part in raw.Split(','))
            {
                if (int.TryParse(part, out int idx))
                    _residentIndices.Add(idx);
            }
        }
    }
}