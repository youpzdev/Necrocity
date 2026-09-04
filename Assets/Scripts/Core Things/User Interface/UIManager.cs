using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private IconsConfig resourceIcons;
    [SerializeField] private BuildingInfoPanel buildingInfo;
    [SerializeField] private UIPanel workshopPanel;
    [SerializeField] private WorkshopPanel workshopTabs;
    [Space(10)]
    [SerializeField] private TMP_Text dublonsText;
    [SerializeField] private TMP_Text loveText;
    [Space(25)]
    [SerializeField] private GameObject[] uiPanels;
    [Space(10)]
    [SerializeField] private DormitoryPanel dormitoryPanel;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (workshopTabs == null && workshopPanel != null)
            workshopTabs = workshopPanel.GetComponentInChildren<WorkshopPanel>(true);
    }

    void OnEnable()
    {
        EventBus<ResourceManagerChangedEvent>.Subscribe(OnResourcesChanged, this);
    }

    void OnDisable()
    {
        EventBus<ResourceManagerChangedEvent>.Unsubscribe(OnResourcesChanged);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
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


    // публичная нечисть

    public bool AreModalWindowOpened()
    {
        foreach (var item in uiPanels)
        {
            if (item == null || !item.activeSelf) continue;
            if (item.TryGetComponent(out UIPanel panel) && !panel.IsOpen) continue;
            return true;
        }
        return false;
    }

    public Sprite GetIcon(ResourceType type) => resourceIcons.GetIcon(type);

    public void ShowGainerPanel(ResourceGainer gainer)
    {
        if (AreModalWindowOpened()) return;

        buildingInfo.Show(gainer);
    }

    public void ShowWorkshopPanel()
    {
        if (AreModalWindowOpened()) return;
        workshopPanel.Show();
    }

    public void ShowWorkshopLaboratory()
    {
        if (AreModalWindowOpened()) return;

        workshopPanel.Show();
        if (workshopTabs != null) workshopTabs.OpenLaboratoryTab();
    }

    public void ShowDormitoryPanel(Dormitory dormitory)
    {
        if (AreModalWindowOpened()) return;
        dormitoryPanel.Show(dormitory);
    }


#if UNITY_EDITOR
    void OnValidate()
    {
        if (resourceIcons == null) resourceIcons = GetComponent<IconsConfig>();
        if (buildingInfo == null) buildingInfo = FindFirstObjectByType<BuildingInfoPanel>();
    }
#endif

}
