using TMPro;
using UnityEngine;

public class ResidentCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text counterText;
    [SerializeField] private Dormitory dormitory;

    private void OnEnable()
    {
        EventBus<DormitoryChangedEvent>.Subscribe(OnChanged, this);
    }

    private void OnDisable()
    {
        EventBus<DormitoryChangedEvent>.Unsubscribe(OnChanged);
    }

    private void Start()
    {
        UpdateText();
    }

    private void OnChanged(DormitoryChangedEvent _) => UpdateText();

    private void UpdateText()
    {
        counterText.text = $"{dormitory.ResidentCount}/{dormitory.Capacity}";
    }
}