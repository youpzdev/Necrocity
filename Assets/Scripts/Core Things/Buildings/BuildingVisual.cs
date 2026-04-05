using System;
using UnityEngine;

public class BuildingVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ResourceGainer resourceGainer;

    [Space(15)]
    [SerializeField] private LevelObjects[] levelChanges;

    private void Start()
    {
        EventBus<LevelChangedEvent>.Subscribe(OnLevelChanged, this);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        int currentLevel = resourceGainer.Level;
        foreach (var change in levelChanges)
        {
            bool active = change.level <= currentLevel;
            foreach (var obj in change.buildingParts) obj.SetActive(active);
        }
    }

    private void OnLevelChanged(LevelChangedEvent evt)
    {
        if (evt.Gainer != resourceGainer) return;
        UpdateVisual();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (resourceGainer == null) resourceGainer = GetComponent<ResourceGainer>();
    }
#endif

    [Serializable]
    public class LevelObjects
    {
        public GameObject[] buildingParts;
        public int level;
    }
}