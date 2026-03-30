using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseComponentTab : MonoBehaviour
{
    [SerializeField] protected TMP_Text titleText;
    [SerializeField] protected TMP_Text descText;
    [SerializeField] protected Image iconImage;
    [SerializeField] protected Transform componentGrid;
    [SerializeField] protected GameObject componentPrefab;

    protected WorkshopPanel workshopPanel;

    protected virtual void Awake()
    {
        workshopPanel = GetComponentInParent<WorkshopPanel>();
    }

    protected virtual void Start() => UpdateUI();

    protected virtual void UpdateUI()
    {
        foreach (Transform old in componentGrid) Pooling.Destroy(old.gameObject);

        foreach (var compData in GetComponentDatas())
        {
            int amount = GetAmount(compData);

            WorkshopComponent obj = Pooling.Instantiate(componentPrefab, componentGrid)
                                           .GetComponent<WorkshopComponent>();
            obj.Init(
                icon: compData.icon,
                title: compData.name,
                amount: amount,
                clickAction: () => OnComponentClick(compData)
            );
        }
    }
    protected abstract ComponentData[] GetComponentDatas();
    protected abstract int GetAmount(ComponentData data);

    protected virtual void OnComponentClick(ComponentData data)
    {
        titleText.text = data.name;
        if (descText != null) descText.text = data.description;
        iconImage.sprite = data.icon;
    }
}