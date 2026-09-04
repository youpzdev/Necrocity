using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CharacterSlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Button iconButton;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button callButton;
    [SerializeField] private TMP_Text callButtonText;
    [SerializeField] private GameObject ownedBadge;
    [SerializeField] private TMP_Text ownedCountText;

    [Header("Labels")]
    [SerializeField] private string callLabel = "Призвать";
    [SerializeField] private string fullLabel = "Нет мест";
    [SerializeField] private string ownedCountFormat = "x{0}";

    public void Init(CharacterData data, int price, int ownedCount, bool dormFull, UnityAction onBuy, UnityAction onSelect)
    {
        icon.sprite = data.icon;
        nameText.text = data.characterName;

        priceText.gameObject.SetActive(true);
        priceText.text = $"{price}";

        ownedBadge.SetActive(ownedCount > 0);
        if (ownedCountText != null) ownedCountText.text = string.Format(ownedCountFormat, ownedCount);

        callButton.onClick.RemoveAllListeners();
        callButton.interactable = !dormFull;
        callButtonText.text = dormFull ? fullLabel : callLabel;
        callButton.onClick.AddListener(onBuy);

        if (iconButton != null)
        {
            iconButton.onClick.RemoveAllListeners();
            iconButton.onClick.AddListener(onSelect);
        }
    }
}
