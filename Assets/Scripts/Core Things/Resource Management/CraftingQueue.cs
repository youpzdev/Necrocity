using System;
using UnityEngine;

public class CraftingQueue : MonoBehaviour
{
    public static CraftingQueue Instance { get; private set; }

    private const string KeyType = "craft_type";
    private const string KeyStartTime = "craft_start";
    private const string KeyDuration = "craft_duration";
    private const string KeyActive = "craft_active";
    private const string KeyReady = "craft_ready";

    public event Action OnCraftStarted;
    public event Action<ComponentType> OnCraftCompleted;
    public event Action OnCraftCancelled;

    public bool IsActive { get; private set; }
    public ComponentType CurrentType { get; private set; }
    public float Duration { get; private set; }
    public float Elapsed => IsActive ? (float)(DateTime.UtcNow - _startTime).TotalSeconds : 0f;
    public float Progress => IsActive ? Mathf.Clamp01(Elapsed / Duration) : 0f;
    public float Remaining => IsActive ? Mathf.Max(0f, Duration - Elapsed) : 0f;
    public bool IsCompleted => IsActive && Elapsed >= Duration;
    public bool IsReadyToCollect { get; private set; }

    public ComponentData CurrentData { get; private set; }

    private DateTime _startTime;
    private bool _completedOffline;

    private GameObject prefab;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadState();
    }

    private void Start()
    {
        if (_completedOffline) CompleteCraft();
    }

    private void Update()
    {
        if (IsActive && IsCompleted) CompleteCraft();
    }

    // ─── Публичная нечисть ────────────────────────────────────────────────

    public bool TryStartCraft(ComponentData data)
    {
        if (IsActive)
        {
            Debug.LogWarning("CraftingQueue: крафт уже идёт.");
            return false;
        }

        CurrentData = data;
        CurrentType = data.type;
        Duration = data.craftDuration;
        _startTime = DateTime.UtcNow;
        IsActive = true;

        SaveState();
        OnCraftStarted?.Invoke();
        return true;
    }

    public void CancelCraft()
    {
        if (!IsActive) return;
        IsActive = false;
        ClearState();
        OnCraftCancelled?.Invoke();
        CurrentData = null;
    }

    public void RestoreCurrentData(WorkshopPanel panel)
    {
        if (CurrentData != null) return;
        if (CurrentType == default) return;
        if (panel == null) panel = FindFirstObjectByType<WorkshopPanel>(FindObjectsInactive.Include);

        if (panel == null) return;

        CurrentData = panel.GetComponentData(CurrentType);
    }

    public void Redeem()
    {
        if (!IsReadyToCollect) return;
        InventoryManager.Instance.AddComponent(CurrentType, 1);
        IsReadyToCollect = false;
        Save.Delete(KeyReady);
        Save.Delete(KeyType);
        CurrentType = default;
        OnCraftCancelled?.Invoke();
        EventBus<InventoryChangedEvent>.Raise(new InventoryChangedEvent());
    }

    // ─── Крафты ──────────────────────────────────────────────────

    private void CompleteCraft()
    {
        IsActive = false;
        _completedOffline = false;
        IsReadyToCollect = true;
        Save.Set(KeyReady, true);
        Save.Delete(KeyActive);
        Save.Delete(KeyStartTime);
        Save.Delete(KeyDuration);
        OnCraftCompleted?.Invoke(CurrentType);
        CurrentData = null;
    }




    // ─── Стейты ───────────────────────────────────────────────

    private void SaveState()
    {
        Save.Set(KeyActive, true);
        Save.Set(KeyType, (int)CurrentType);
        Save.Set(KeyStartTime, _startTime.ToBinary().ToString());
        Save.Set(KeyDuration, Duration);
    }

    private void LoadState()
    {
        if (Save.Get(KeyReady, false))
        {
            CurrentType = (ComponentType)Save.Get(KeyType, 0);
            IsReadyToCollect = true;
            return;
        }

        if (!Save.Get(KeyActive, false)) return;

        CurrentType = (ComponentType)Save.Get(KeyType, 0);
        Duration = Save.Get(KeyDuration, 0f);
        _startTime = DateTime.FromBinary(long.Parse(Save.Get(KeyStartTime, "0")));
        IsActive = true;

        if (IsCompleted)
        {
            IsActive = false;
            _completedOffline = true;
        }
    }

    private void ClearState()
    {
        Save.Delete(KeyActive);
        Save.Delete(KeyType);
        Save.Delete(KeyStartTime);
        Save.Delete(KeyDuration);
    }
}