using UnityEngine;

[RequireComponent(typeof(ModularBuilding))]
public class BuildingVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ResourceGainer resourceGainer;
    [SerializeField] private ModularBuilding modularBuilding;

    private bool _applied;

    private void Awake()
    {
        if (resourceGainer == null) resourceGainer = GetComponent<ResourceGainer>();
        if (modularBuilding == null) modularBuilding = GetComponent<ModularBuilding>();
    }

    private void OnEnable()
    {
        EventBus<LevelChangedEvent>.Subscribe(OnLevelChanged, this);
    }

    private void OnDisable()
    {
        EventBus<LevelChangedEvent>.Unsubscribe(OnLevelChanged);
    }

    private void Start()
    {
        modularBuilding.ApplyLevel(resourceGainer.Level);
        _applied = true;
    }

    private void OnLevelChanged(LevelChangedEvent evt)
    {
        if (evt.Gainer != resourceGainer) return;

        if (_applied)
        {
            modularBuilding.SetLevel(resourceGainer.Level);
            return;
        }

        modularBuilding.ApplyLevel(resourceGainer.Level);
        _applied = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (resourceGainer == null) resourceGainer = GetComponent<ResourceGainer>();
        if (modularBuilding == null) modularBuilding = GetComponent<ModularBuilding>();
    }
#endif
}
