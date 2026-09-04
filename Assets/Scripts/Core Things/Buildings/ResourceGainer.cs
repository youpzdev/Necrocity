using System;
using System.Collections;
using UnityEngine;

public class ResourceGainer : MonoBehaviour, IClickableBuilding
{
    [SerializeField] private BuildingData buildingData;
    [SerializeField] private string saveId;
    [SerializeField] private float maxOfflineHours = 8f;

    private int gainLimit;
    private int gainTime;
    private float gainAmount = 0;
    private bool isProducing = true;
    private float nextTickTime = 0;
    private long lastTickUtc;
    private int level = 1;
    private string saveKey;

    public int GainLimit => gainLimit;
    public int GainAmount => Mathf.FloorToInt(gainAmount);
    public BuildingData Data => buildingData;
    public int Level => level;

    void Awake()
    {
        saveKey = GameSave.Keys.Gainer(string.IsNullOrEmpty(saveId) ? HierarchyPath() : saveId);
        lastTickUtc = GameSave.NowUtc();
        ApplyLevel();
        Load();
    }

    void OnEnable()
    {
        nextTickTime = Time.time + 1f;
    }

    void Start()
    {
        StartCoroutine(RaiseStateNextFrame());
    }

    void Update()
    {
        if (!isProducing) return;
        if (Time.time < nextTickTime) return;

        nextTickTime = Time.time + 1f;

        ApplyElapsedProgress();
        SaveState();
        EventBus<ResourcesChangedEvent>.Raise(new ResourcesChangedEvent { Gainer = this });
    }

    public void Redeem()
    {
        if (gainAmount <= 0) return;

        ResourceManager.Instance.AddResource(buildingData.gainResources[level - 1].resourceType, Mathf.FloorToInt(gainAmount));
        ResetProduction();
    }

    public void AddLevel()
    {
        if (level >= buildingData.gainResources.Length) return;

        if (buildingData.gainResources[level - 1].resourceType != buildingData.gainResources[level].resourceType) Redeem();

        level++;
        ApplyLevel();
        SaveState();
        EventBus<ResourcesChangedEvent>.Raise(new ResourcesChangedEvent { Gainer = this });
        EventBus<LevelChangedEvent>.Raise(new LevelChangedEvent { Gainer = this });
    }

    public void OnClick()
    {
        UIManager.Instance.ShowGainerPanel(this);
    }

    float ProducePerSecond => (float)gainLimit / gainTime;

    void ResetProduction()
    {
        nextTickTime = Time.time + 1f;
        lastTickUtc = GameSave.NowUtc();
        gainAmount = 0;
        isProducing = true;
        SaveState();
        EventBus<ResourcesChangedEvent>.Raise(new ResourcesChangedEvent { Gainer = this });
    }

    void ApplyElapsedProgress()
    {
        double limit = Math.Max(0f, maxOfflineHours) * 3600d;
        double seconds = Math.Min(GameSave.SecondsSince(lastTickUtc), limit);
        if (seconds <= 0) return;

        lastTickUtc = GameSave.NowUtc();
        gainAmount += (float)(seconds * ProducePerSecond);

        if (gainAmount >= gainLimit)
        {
            gainAmount = gainLimit;
            isProducing = false;
        }
    }

    void ApplyLevel()
    {
        int index = Mathf.Clamp(level - 1, 0, buildingData.gainResources.Length - 1);
        gainLimit = buildingData.gainResources[index].gainLimit;
        gainTime = Mathf.Max(1, buildingData.gainResources[index].gainTime);
    }

    void Load()
    {
        var state = GameSave.Get<GameSave.GainerState>(saveKey, null);
        if (state == null)
        {
            SaveState();
            return;
        }

        level = Mathf.Clamp(state.Level, 1, buildingData.gainResources.Length);
        ApplyLevel();

        gainAmount = Mathf.Clamp(state.Progress, 0f, gainLimit);
        isProducing = state.Producing && gainAmount < gainLimit;
        if (state.SavedAtUtc > 0) lastTickUtc = state.SavedAtUtc;

        if (isProducing) ApplyElapsedProgress();
        SaveState();
    }

    void SaveState()
    {
        GameSave.Set(saveKey, new GameSave.GainerState
        {
            Level = level,
            Progress = gainAmount,
            Producing = isProducing,
            SavedAtUtc = lastTickUtc
        });
    }

    string HierarchyPath()
    {
        string path = name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    IEnumerator RaiseStateNextFrame()
    {
        yield return null;
        EventBus<ResourcesChangedEvent>.Raise(new ResourcesChangedEvent { Gainer = this });
        EventBus<LevelChangedEvent>.Raise(new LevelChangedEvent { Gainer = this });
    }
}
