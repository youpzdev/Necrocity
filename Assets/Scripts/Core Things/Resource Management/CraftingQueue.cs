using System;
using UnityEngine;

public class CraftingQueue : MonoBehaviour
{
    public static CraftingQueue Instance { get; private set; }

    private const string KeyType = "craft_type";
    private const string KeyStartTime = "craft_start";
    private const string KeyDuration = "craft_duration";
    private const string KeyActive = "craft_active";

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

    private DateTime _startTime;
    private bool _completedOffline;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadState();
    }

    private void Start()
    {
        if (_completedOffline)
            CompleteCraft();
    }

    private void Update()
    {
        if (IsActive && IsCompleted)
            CompleteCraft();
    }

    // ─── Public API ────────────────────────────────────────────────

    public bool TryStartCraft(ComponentData data)
    {
        if (IsActive)
        {
            Debug.LogWarning("CraftingQueue: крафт уже идёт.");
            return false;
        }

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
    }

    // ─── Internal ──────────────────────────────────────────────────

    private void CompleteCraft()
    {
        var type = CurrentType;
        IsActive = false;
        _completedOffline = false;
        ClearState();
        InventoryManager.Instance.AddComponent(type, 1);
        OnCraftCompleted?.Invoke(type);
    }

    // ─── Persistence ───────────────────────────────────────────────

    private void SaveState()
    {
        Save.Set(KeyActive, true);
        Save.Set(KeyType, (int)CurrentType);
        Save.Set(KeyStartTime, _startTime.ToBinary().ToString());
        Save.Set(KeyDuration, Duration);
    }

    private void LoadState()
    {
        if (!Save.Get(KeyActive, false)) return;

        CurrentType = (ComponentType)Save.Get(KeyType, 0);
        Duration = Save.Get(KeyDuration, 0f);
        _startTime = DateTime.FromBinary(long.Parse(Save.Get(KeyStartTime, "0")));
        IsActive = true;

        // Истекло оффлайн — откладываем до Start когда все синглтоны живы
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