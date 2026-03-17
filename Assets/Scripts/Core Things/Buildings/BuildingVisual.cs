using System;
using UnityEngine;

public class BuildingVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ResourceGainer resourceGainer;

    [Space(15)]
    [SerializeField] private LevelObjects[] levelChanges;

    void Start()
    {
        EventBus<LevelChangedEvent>.Subscribe(OnLevelChanged, this);
    }

    void UpdateVisual()
    {
        foreach (var change in levelChanges)
        {
            bool active = change.level == resourceGainer.Level;
            foreach (var obj in change.buildingParts) obj.SetActive(active);
        }
    }

    void OnLevelChanged(LevelChangedEvent evt)
    {
        if (evt.Gainer != resourceGainer) return;
        UpdateVisual();
    }

#if UNITY_EDITOR
    void OnValidate()
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
