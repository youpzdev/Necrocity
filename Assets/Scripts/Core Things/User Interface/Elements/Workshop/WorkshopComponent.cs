using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WorkshopComponent : MonoBehaviour
{
    [SerializeField] private Image imageIcon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private GameObject amountIcon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Image rarityFrame;

    [SerializeField] private Button button;

    [Header("Availability")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float availableAlpha = 1f;
    [SerializeField] private float unavailableAlpha = 0.4f;
    [SerializeField] private Color availableTint = Color.white;
    [SerializeField] private Color unavailableTint = new Color(0.55f, 0.55f, 0.6f, 1f);

    [Header("Amount")]
    [SerializeField] private string requiredAmountFormat = "{0}/{1}";
    [SerializeField] private Color enoughAmountColor = Color.white;
    [SerializeField] private Color missingAmountColor = new Color(0.9f, 0.25f, 0.25f, 1f);

    private ItemData _itemData;

    public void Init(Sprite icon = null, string title = "", int amount = -1, UnityAction clickAction = null,
                     ItemData itemData = null, bool available = true, int requiredAmount = -1, Color? rarityColor = null)
    {
        _itemData = itemData;

        if (imageIcon)
        {
            imageIcon.sprite = icon;
            imageIcon.enabled = icon != null;
        }
        if (titleText) titleText.text = title;
        if (button)
        {
            button.onClick.RemoveAllListeners();
            if (clickAction != null) button.onClick.AddListener(clickAction);
        }

        if (rarityFrame && rarityColor.HasValue) rarityFrame.color = rarityColor.Value;

        ApplyAvailability(available);
        ApplyAmount(amount, requiredAmount);
    }

    private void ApplyAvailability(bool available)
    {
        if (canvasGroup) canvasGroup.alpha = available ? availableAlpha : unavailableAlpha;
        if (imageIcon) imageIcon.color = available ? availableTint : unavailableTint;
    }

    private void ApplyAmount(int amount, int requiredAmount)
    {
        if (requiredAmount >= 0)
        {
            if (amountIcon) amountIcon.SetActive(true);
            if (amountText)
            {
                int owned = Mathf.Max(amount, 0);
                amountText.text = string.Format(requiredAmountFormat, owned, requiredAmount);
                amountText.color = owned >= requiredAmount ? enoughAmountColor : missingAmountColor;
            }
            return;
        }

        if (amount < 0) return;

        if (amountIcon) amountIcon.SetActive(amount > 0);
        if (amountText)
        {
            amountText.text = amount.ToString();
            amountText.color = enoughAmountColor;
        }
    }
}
