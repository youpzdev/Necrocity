using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [Header("References")]
    [SerializeField] private ResourceIconsConfig resourceIcons;
    [SerializeField] private BuildingInfoPanel buildingInfo;
    [Space(10)]
    [SerializeField] private TMP_Text dublonsText;
    [SerializeField] private TMP_Text loveText;
    [Space(25)]
    [SerializeField] private GameObject[] uiPanels;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EventBus<ResourceManagerChangedEvent>.Subscribe(OnResourcesChanged, this);
    }

    void UpdateResourcesText()
    {
        dublonsText.text = $"{ResourceManager.Instance.GetResourceAmount(ResourceType.Dublons)}";
        loveText.text = $"{ResourceManager.Instance.GetResourceAmount(ResourceType.Love)}";
    }

    void OnResourcesChanged(ResourceManagerChangedEvent evt)
    {
        UpdateResourcesText();
    }


    // ========= публичная нечисть ========= 

    public bool AreModalWindowOpened()
    {
        foreach (var item in uiPanels) if (item.activeSelf) return true;
        return false;
    }

    public Sprite GetIcon(ResourceType type) => resourceIcons.GetIcon(type);

    public void ShowBuildingInfo(ResourceGainer gainer)
    {
        if (AreModalWindowOpened()) return;

        buildingInfo.Show(gainer);
    }


#if UNITY_EDITOR
    void OnValidate()
    {
        if (resourceIcons == null) resourceIcons = GetComponent<ResourceIconsConfig>();
        if (buildingInfo == null) buildingInfo = FindFirstObjectByType<BuildingInfoPanel>();
    }
#endif

}
