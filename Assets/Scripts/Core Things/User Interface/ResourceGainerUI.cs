using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ResourceGainerUI : MonoBehaviour
{
    [SerializeField] private Image slider;
    [SerializeField] private TMP_Text sliderText;
    [SerializeField] private float tweenDuration = 0.5f;
    [SerializeField] private Button gainButton;

    private ResourceGainer resourceGainer;
    private float currentFill;

    void Awake()
    {
        resourceGainer = GetComponentInParent<ResourceGainer>();
    }

    void Start()
    {
        if (gainButton) gainButton.onClick.AddListener(resourceGainer.Redeem);
        EventBus<ResourcesChangedEvent>.Subscribe(OnResourcesChanged, this);
        UpdateUI();
    }

    void OnResourcesChanged(ResourcesChangedEvent evt)
    {
        if (evt.Gainer != resourceGainer) return;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (!slider || !resourceGainer || !sliderText) { enabled = false; return; }

        sliderText.text = $"{resourceGainer.GainAmount}/{resourceGainer.GainLimit}";
        float targetFill = Mathf.Clamp01((float)resourceGainer.GainAmount / resourceGainer.GainLimit);

        if (!Mathf.Approximately(currentFill, targetFill))
        {
            DOTween.Kill(slider);
            slider.DOFillAmount(targetFill, tweenDuration).SetEase(Ease.OutCubic);
            currentFill = targetFill;
        }
    }
}