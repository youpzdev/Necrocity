using System;
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

    [SerializeField] private Button button;

    private ItemData _itemData;

    public void Init(Sprite icon = null, string title = "", int amount = -1, UnityAction clickAction = null, ItemData itemData = null)
    {
        _itemData = itemData;

        if (imageIcon) imageIcon.sprite = icon;
        if (titleText) titleText.text = title;
        if (button && clickAction != null) button.onClick.AddListener(clickAction);

        if (amount >= 0)
        {
            if (amountIcon) amountIcon.gameObject.SetActive(amount > 1);
            if (amountText) amountText.text = amount.ToString();
        }
    }

}
