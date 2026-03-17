using System;
using UnityEngine;

public class ResourceGainer : MonoBehaviour
{
    [SerializeField] private BuildingData buildingData;

    private int gainLimit;
    private int gainTime;
    private float gainAmount = 0;
    private bool isProducing = true;
    private float produceTimer = 0;
    private int level = 1;

    public int GainLimit => gainLimit;
    public int GainAmount => Mathf.FloorToInt(gainAmount);
    public BuildingData Data => buildingData;
    public int Level => level;

    void Awake()
    {
        gainLimit = buildingData.gainResources[level - 1].gainLimit;
        gainTime = buildingData.gainResources[level - 1].gainTime;
    }

    void Update()
    {
        HandleProducing();
    }

    void HandleProducing()
    {
        if (!isProducing) return;

        produceTimer += Time.deltaTime;
        if (produceTimer >= 1f)
        {
            produceTimer = 0;
            AddProgress();
        }
    }

    void Reset()
    {
        produceTimer = 0f;
        gainAmount = 0;
        isProducing = true;
        EventBus<ResourcesChangedEvent>.Raise(new ResourcesChangedEvent { Gainer = this });
    }

    void AddProgress()
    {
        float producePerSec = (float)gainLimit / gainTime;
        gainAmount += producePerSec;
        EventBus<ResourcesChangedEvent>.Raise(new ResourcesChangedEvent { Gainer = this });

        if (gainAmount >= gainLimit)
        {
            gainAmount = gainLimit;
            isProducing = false;
        }
    }

    public void Redeem()
    {
        if (gainAmount <= 0) return;

        ResourceManager.Instance.AddResource(buildingData.gainResources[level - 1].resourceType, Mathf.FloorToInt(gainAmount));
        Reset();
    }

    public void AddLevel()
    {
        if (level >= buildingData.gainResources.Length) return;

        if (buildingData.gainResources[level - 1].resourceType != buildingData.gainResources[level].resourceType) Redeem();

        level++;
        gainLimit = buildingData.gainResources[level - 1].gainLimit;
        gainTime = buildingData.gainResources[level - 1].gainTime;
        EventBus<ResourcesChangedEvent>.Raise(new ResourcesChangedEvent { Gainer = this });
        EventBus<LevelChangedEvent>.Raise(new LevelChangedEvent { Gainer = this });
    }

    void OnMouseDown()
    {
        UIManager.Instance.ShowBuildingInfo(this);
    }
}