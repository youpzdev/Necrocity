using System;
using System.Collections.Generic;
using UnityEngine;

public enum CharacterPurchaseResult
{
    Success,
    NoConfig,
    UnknownCharacter,
    NoFreeSpace,
    NotEnoughDublons
}

public class Dormitory : MonoBehaviour, IClickableBuilding
{
    [Serializable]
    public class Resident
    {
        public int CharacterIndex;
        public int VariantIndex = -1;
    }

    [SerializeField] private DormitoryConfig config;

    [Header("Modules")]
    [SerializeField] private ModularBuilding modularBuilding;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private ParticleSystem moduleRevealVfx;

    private int _level = 1;
    private readonly List<Resident> _residents = new();

    public event Action<Resident> ResidentAdded;
    public event Action ModuleRevealed;

    public int Level => _level;
    public int ResidentCount => _residents.Count;
    public int Capacity => config != null ? config.GetCapacity(_level) : 0;
    public bool IsFull => ResidentCount >= Capacity;
    public DormitoryConfig Config => config;
    public IReadOnlyList<Resident> Residents => _residents;
    public int NextCharacterPrice => config != null ? config.GetCharacterPrice(ResidentCount) : 0;

    private void Awake()
    {
        Load();

        if (modularBuilding == null) modularBuilding = GetComponent<ModularBuilding>();
        if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();

        if (modularBuilding != null) modularBuilding.ModuleRevealed += OnModuleRevealed;
    }

    private void Start()
    {
        if (modularBuilding != null) modularBuilding.ApplyLevel(_level);
    }

    private void OnDestroy()
    {
        if (modularBuilding != null) modularBuilding.ModuleRevealed -= OnModuleRevealed;
    }

    public bool TryUpgrade()
    {
        if (config == null) return false;
        if (_level >= config.maxLevel) return false;

        int price = config.GetUpgradePrice(_level);
        if (!ResourceManager.Instance.SpendResource(ResourceType.Dublons, price)) return false;

        _level++;
        SaveData();
        EventBus<DormitoryChangedEvent>.Raise(new DormitoryChangedEvent());

        if (modularBuilding != null) modularBuilding.SetLevel(_level);
        return true;
    }

    public CharacterPurchaseResult TryPurchaseCharacter(int characterIndex, out Resident resident)
    {
        resident = null;

        if (config == null) return CharacterPurchaseResult.NoConfig;

        CharacterData data = config.GetCharacter(characterIndex);
        if (data == null) return CharacterPurchaseResult.UnknownCharacter;

        if (IsFull) return CharacterPurchaseResult.NoFreeSpace;

        int price = config.GetCharacterPrice(ResidentCount);
        if (!ResourceManager.Instance.SpendResource(ResourceType.Dublons, price))
            return CharacterPurchaseResult.NotEnoughDublons;

        resident = new Resident
        {
            CharacterIndex = characterIndex,
            VariantIndex = data.PickRandomVariantIndex()
        };
        _residents.Add(resident);
        SaveData();

        ResidentAdded?.Invoke(resident);

        EventBus<CharacterPurchasedEvent>.Raise(new CharacterPurchasedEvent
        {
            Data = data,
            ResidentCount = ResidentCount,
            Capacity = Capacity
        });
        EventBus<DormitoryChangedEvent>.Raise(new DormitoryChangedEvent());

        return CharacterPurchaseResult.Success;
    }

    public int GetOwnedCount(int characterIndex)
    {
        int count = 0;
        foreach (var resident in _residents)
        {
            if (resident.CharacterIndex == characterIndex) count++;
        }
        return count;
    }

    public CharacterData GetCharacterData(int index) => config != null ? config.GetCharacter(index) : null;

    public void OnClick()
    {
        UIManager.Instance.ShowDormitoryPanel(this);
    }

    private void OnModuleRevealed(int moduleIndex)
    {
        ModuleRevealed?.Invoke();

        if (moduleRevealVfx != null) moduleRevealVfx.Play();
        if (cameraController != null) cameraController.FocusOn(transform, modularBuilding.RevealDuration);
    }

    private void SaveData()
    {
        GameSave.Set(GameSave.Keys.DormitoryLevel, _level);
        GameSave.Set(GameSave.Keys.DormitoryResidents, _residents);
    }

    private void Load()
    {
        _level = Mathf.Max(1, GameSave.Get(GameSave.Keys.DormitoryLevel, 1));
        if (config != null) _level = Mathf.Min(_level, Mathf.Max(1, config.maxLevel));

        _residents.Clear();

        var stored = GameSave.Get<List<Resident>>(GameSave.Keys.DormitoryResidents, null);
        if (stored == null) return;

        foreach (var resident in stored)
        {
            if (resident == null) continue;
            if (config != null && !config.HasCharacter(resident.CharacterIndex)) continue;
            _residents.Add(resident);
        }
    }
}
