using System;
using UnityEngine;

public enum CraftStartResult
{
    Started,
    AlreadyCrafting,
    AwaitingCollect,
    MissingData
}

public class CraftingQueue : MonoBehaviour
{
    public static CraftingQueue Instance { get; private set; }

    private const string KeyType = "craft_type";
    private const string KeyStartTime = "craft_start";
    private const string KeyDuration = "craft_duration";
    private const string KeyActive = "craft_active";
    private const string KeyReady = "craft_ready";
    private const string KeyLastSeen = "craft_last_seen";

    private const string ConfigResourcePath = "Craft/Crafting Config";

    private const float TickInterval = 0.25f;
    private const double MaxForwardJumpSeconds = 86400d;
    private const float MinDurationMultiplier = 0.1f;
    private const float MinDuration = 1f;

    public event Action OnCraftStarted;
    public event Action<ComponentType> OnCraftCompleted;
    public event Action OnCraftCancelled;
    public event Action<ComponentType> OnCraftRedeemed;

    public bool IsActive { get; private set; }
    public ComponentType CurrentType { get; private set; }
    public bool HasCurrentType { get; private set; }
    public float Duration { get; private set; }
    public float Elapsed => IsActive ? (float)_elapsedSeconds : 0f;
    public float Progress => IsActive && Duration > 0f ? Mathf.Clamp01(Elapsed / Duration) : 0f;
    public float Remaining => IsActive ? Mathf.Max(0f, Duration - Elapsed) : 0f;
    public bool IsCompleted => IsActive && Elapsed >= Duration;
    public bool IsReadyToCollect { get; private set; }

    public ComponentData CurrentData { get; private set; }

    public float NextCraftDurationMultiplier { get; private set; } = 1f;

    private DateTime _startTime;
    private DateTime _lastSeenTime;
    private double _elapsedSeconds;
    private float _nextTickTime;
    private bool _completedOffline;

    private CraftingConfig _config;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadState();
        RestoreCurrentData();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (_completedOffline) CompleteCraft();
    }

    private void Update()
    {
        if (!IsActive) return;
        if (Time.unscaledTime < _nextTickTime) return;

        _nextTickTime = Time.unscaledTime + TickInterval;
        AdvanceTime();

        if (IsCompleted) CompleteCraft();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) PersistLastSeen();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) PersistLastSeen();
    }

    private void OnApplicationQuit() => PersistLastSeen();

    // ─── Публичная нечисть ────────────────────────────────────────────────

    public CraftStartResult TryStartCraft(ComponentData data)
    {
        if (IsActive) return CraftStartResult.AlreadyCrafting;
        if (IsReadyToCollect) return CraftStartResult.AwaitingCollect;
        if (data == null) return CraftStartResult.MissingData;

        CurrentData = data;
        CurrentType = data.type;
        HasCurrentType = true;
        Duration = Mathf.Max(MinDuration, data.craftDuration * NextCraftDurationMultiplier);
        NextCraftDurationMultiplier = 1f;

        _startTime = DateTime.UtcNow;
        _lastSeenTime = _startTime;
        _elapsedSeconds = 0d;
        _nextTickTime = Time.unscaledTime + TickInterval;
        IsActive = true;

        SaveState();
        OnCraftStarted?.Invoke();
        return CraftStartResult.Started;
    }

    public void SetNextCraftDurationMultiplier(float multiplier)
    {
        NextCraftDurationMultiplier = Mathf.Clamp(multiplier, MinDurationMultiplier, 1f);
    }

    public void ResetNextCraftDurationMultiplier() => NextCraftDurationMultiplier = 1f;

    public void CancelCraft()
    {
        if (!IsActive) return;
        IsActive = false;
        HasCurrentType = false;
        _elapsedSeconds = 0d;
        ClearState();
        OnCraftCancelled?.Invoke();
        CurrentData = null;
    }

    public void RestoreCurrentData()
    {
        if (CurrentData != null) return;
        if (!HasCurrentType) return;

        var config = LoadConfig();
        if (config == null) return;

        CurrentData = config.GetComponentData(CurrentType);
    }

    public void Redeem()
    {
        if (!IsReadyToCollect) return;

        ComponentType redeemed = CurrentType;
        InventoryManager.Instance.AddComponent(redeemed, 1);

        IsReadyToCollect = false;
        HasCurrentType = false;
        CurrentData = null;
        Save.Delete(KeyReady);
        Save.Delete(KeyType);

        OnCraftRedeemed?.Invoke(redeemed);
        EventBus<InventoryChangedEvent>.Raise(new InventoryChangedEvent());
    }

    // ─── Крафты ──────────────────────────────────────────────────

    private void CompleteCraft()
    {
        IsActive = false;
        _completedOffline = false;
        IsReadyToCollect = true;
        _elapsedSeconds = Duration;
        Save.Set(KeyReady, true);
        Save.Delete(KeyActive);
        Save.Delete(KeyStartTime);
        Save.Delete(KeyDuration);
        Save.Delete(KeyLastSeen);
        OnCraftCompleted?.Invoke(CurrentType);
        CurrentData = null;
    }

    private void AdvanceTime()
    {
        DateTime now = DateTime.UtcNow;
        double delta = (now - _lastSeenTime).TotalSeconds;

        double correction = 0d;
        if (delta < 0d) correction = delta;
        else if (delta > MaxForwardJumpSeconds) correction = delta - MaxForwardJumpSeconds;

        _lastSeenTime = now;

        if (correction != 0d)
        {
            _startTime = _startTime.AddSeconds(correction);
            SaveState();
        }

        _elapsedSeconds = Math.Max(0d, (now - _startTime).TotalSeconds);
    }

    private CraftingConfig LoadConfig()
    {
        if (_config == null) _config = Resources.Load<CraftingConfig>(ConfigResourcePath);
        return _config;
    }

    // ─── Стейты ───────────────────────────────────────────────

    private void SaveState()
    {
        Save.Set(KeyActive, true);
        Save.Set(KeyType, (int)CurrentType);
        Save.Set(KeyStartTime, _startTime.ToBinary().ToString());
        Save.Set(KeyDuration, Duration);
        Save.Set(KeyLastSeen, _lastSeenTime.ToBinary().ToString());
    }

    private void PersistLastSeen()
    {
        if (!IsActive) return;
        Save.Set(KeyLastSeen, _lastSeenTime.ToBinary().ToString());
    }

    private void LoadState()
    {
        if (Save.Has(KeyType))
        {
            CurrentType = (ComponentType)Save.Get(KeyType, 0);
            HasCurrentType = true;
        }

        if (Save.Get(KeyReady, false))
        {
            IsReadyToCollect = true;
            return;
        }

        if (!Save.Get(KeyActive, false)) return;
        if (!HasCurrentType) { ClearState(); return; }

        Duration = Save.Get(KeyDuration, 0f);
        _startTime = ReadTime(KeyStartTime, DateTime.UtcNow);
        _lastSeenTime = ReadTime(KeyLastSeen, _startTime);
        IsActive = true;

        AdvanceTime();
        _nextTickTime = Time.unscaledTime + TickInterval;

        if (IsCompleted)
        {
            IsActive = false;
            _completedOffline = true;
        }
    }

    private static DateTime ReadTime(string key, DateTime fallback)
    {
        string raw = Save.Get(key, string.Empty);
        if (string.IsNullOrEmpty(raw)) return fallback;
        return long.TryParse(raw, out long binary) ? DateTime.FromBinary(binary) : fallback;
    }

    private void ClearState()
    {
        Save.Delete(KeyActive);
        Save.Delete(KeyType);
        Save.Delete(KeyStartTime);
        Save.Delete(KeyDuration);
        Save.Delete(KeyLastSeen);
    }
}
