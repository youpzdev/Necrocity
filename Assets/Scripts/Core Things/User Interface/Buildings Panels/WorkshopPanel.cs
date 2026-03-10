using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorkshopPanel : MonoBehaviour
{
    [SerializeField] private UIPanel workshopPanel;
    [SerializeField] private UIPanel componentsPanel;
    [SerializeField] private UIPanel laboratoryPanel;

    [Space(5)]
    [SerializeField] private ButtonStyler workshopButton;
    [SerializeField] private ButtonStyler componentsButton;
    [SerializeField] private ButtonStyler laboratoryButton;

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
            button.GetComponent<Button>().onClick.AddListener(() => OpenPanel(panel));
    }

    public void OpenPanel(UIPanel panel)
    {
        foreach (var (key, button) in _panelButtons)
        {
            bool isActive = key == panel;
            button.SwitchState(isActive);
            if (isActive) key.Show(); else key.Hide();
        }
    }
}
