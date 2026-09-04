using System.Collections.Generic;
using UnityEngine;

public class WorkshopPanel : MonoBehaviour
{
    [Header("Tabs")]
    [SerializeField] private UIPanel workshopPanel;
    [SerializeField] private UIPanel componentsPanel;
    [SerializeField] private UIPanel laboratoryPanel;
    [SerializeField] private ButtonStyler workshopButton;
    [SerializeField] private ButtonStyler componentsButton;
    [SerializeField] private ButtonStyler laboratoryButton;

    [Header("Config")]
    [SerializeField] private CraftingConfig craftingConfig;

    private Dictionary<UIPanel, ButtonStyler> _panelButtons;

    private void Awake()
    {
        _panelButtons = new Dictionary<UIPanel, ButtonStyler>
        {
            { workshopPanel,    workshopButton    },
            { componentsPanel,  componentsButton  },
            { laboratoryPanel,  laboratoryButton  },
        };

        foreach (var (panel, button) in _panelButtons)
            button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => OpenPanel(panel));
    }

    private void OnEnable() => OpenPanel(workshopPanel);

    public void OpenPanel(UIPanel panel)
    {
        foreach (var (key, button) in _panelButtons)
        {
            bool isActive = key == panel;
            button.SwitchState(isActive);
            if (isActive) key.ShowInstant(); else key.HideInstant();
        }
    }

    public void OpenLaboratoryTab() => OpenPanel(laboratoryPanel);

    public ItemData[] GetItemDatas => craftingConfig.GetAllItems();
    public ComponentData GetComponentData(ComponentType component) => craftingConfig.GetComponentData(component);
    public ComponentData[] GetAllComponentDatas => craftingConfig.GetAllComponents();

}
