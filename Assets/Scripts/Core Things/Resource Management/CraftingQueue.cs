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

    private const string KeyType = GameSave.Keys.CraftType;
    private const string KeyStartTime = GameSave.Keys.CraftStart;
    private const string KeyDuration = GameSave.Keys.CraftDuration;
    private const string KeyActive = GameSave.Keys.CraftActive;
    private const string KeyReady = GameSave.Keys.CraftReady;
    private const string KeyLastSeen = GameSave.Keys.CraftLastSeen;

    private static readonly string[] LegacyKeys =
    {
        "craft_type",
        "craft_start",
        "craft_duration",
        "craft_active",
        "craft_ready",
        "craft_last_seen"
    };

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
    public bool CanRedeem => IsReadyToCollect && HasCurrentType && CurrentData != null;

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
        PurgeLegacyKeys();
        LoadState();
        RestoreCurrentData();
        DropStateIfBroken();
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
        ResetState();
        OnCraftCancelled?.Invoke();
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

        RestoreCurrentData();

        if (!HasCurrentType || CurrentData == null)
        {
            ResetState();
            OnCraftCancelled?.Invoke();
            return;
        }

        if (InventoryManager.Instance == null) return;

        ComponentType redeemed = CurrentType;
        InventoryManager.Instance.AddComponent(redeemed, 1);

        IsReadyToCollect = false;
        HasCurrentType = false;
        CurrentData = null;
        Duration = 0f;
        _elapsedSeconds = 0d;
        GameSave.Delete(KeyReady);
        GameSave.Delete(KeyType);

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
        GameSave.Set(KeyReady, true);
        GameSave.Delete(KeyActive);
        GameSave.Delete(KeyStartTime);
        GameSave.Delete(KeyDuration);
        GameSave.Delete(KeyLastSeen);
        RestoreCurrentData();

        if (!HasCurrentType || CurrentData == null)
        {
            ResetState();
            OnCraftCancelled?.Invoke();
            return;
        }

        OnCraftCompleted?.Invoke(CurrentType);
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
        GameSave.Set(KeyActive, true);
        GameSave.Set(KeyType, (int)CurrentType);
        GameSave.Set(KeyStartTime, _startTime.Ticks);
        GameSave.Set(KeyDuration, Duration);
        GameSave.Set(KeyLastSeen, _lastSeenTime.Ticks);
    }

    private void PersistLastSeen()
    {
        if (!IsActive) return;
        GameSave.Set(KeyLastSeen, _lastSeenTime.Ticks);
        GameSave.FlushNow();
    }

    private void LoadState()
    {
        if (GameSave.Has(KeyType))
        {
            int stored = GameSave.Get(KeyType, -1);
            if (Enum.IsDefined(typeof(ComponentType), stored))
            {
                CurrentType = (ComponentType)stored;
                HasCurrentType = true;
            }
        }

        if (GameSave.Get(KeyReady, false))
        {
            if (!HasCurrentType) { ClearState(); return; }

            IsReadyToCollect = true;
            GameSave.Delete(KeyActive);
            GameSave.Delete(KeyStartTime);
            GameSave.Delete(KeyDuration);
            GameSave.Delete(KeyLastSeen);
            return;
        }

        if (!GameSave.Get(KeyActive, false) || !HasCurrentType)
        {
            HasCurrentType = false;
            ClearState();
            return;
        }

        float duration = GameSave.Get(KeyDuration, 0f);
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < MinDuration)
        {
            HasCurrentType = false;
            ClearState();
            return;
        }

        Duration = duration;
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

    private void DropStateIfBroken()
    {
        if (!IsReadyToCollect && !IsActive && !_completedOffline) return;
        if (HasCurrentType && CurrentData != null) return;

        ResetState();
    }

    private static DateTime ReadTime(string key, DateTime fallback)
    {
        long ticks = GameSave.Get(key, 0L);
        if (ticks <= 0L || ticks > DateTime.MaxValue.Ticks) return fallback;
        return new DateTime(ticks, DateTimeKind.Utc);
    }

    private void ResetState()
    {
        IsActive = false;
        IsReadyToCollect = false;
        HasCurrentType = false;
        CurrentData = null;
        Duration = 0f;
        _elapsedSeconds = 0d;
        _completedOffline = false;
        ClearState();
    }

    private void ClearState()
    {
        GameSave.Delete(KeyActive);
        GameSave.Delete(KeyType);
        GameSave.Delete(KeyStartTime);
        GameSave.Delete(KeyDuration);
        GameSave.Delete(KeyLastSeen);
        GameSave.Delete(KeyReady);
    }

    private static void PurgeLegacyKeys()
    {
        foreach (string key in LegacyKeys)
            if (Save.Has(key)) Save.Delete(key);
    }
}
