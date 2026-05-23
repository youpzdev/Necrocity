using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CharacterSlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button callButton;
    [SerializeField] private TMP_Text callButtonText;
    [SerializeField] private GameObject ownedBadge;

    public void Init(CharacterData data, int price, bool owned, bool dormFull, UnityAction onBuy)
    {
        icon.sprite = data.icon;
        nameText.text = data.characterName;

        callButton.onClick.RemoveAllListeners();

        if (owned)
        {
            priceText.gameObject.SetActive(false);
            ownedBadge.SetActive(true);
            callButton.interactable = false;
            callButtonText.text = "Куплен";
        }
        else
        {
            priceText.gameObject.SetActive(true);
            priceText.text = $"{price}";
            ownedBadge.SetActive(false);
            callButton.interactable = !dormFull;
            callButtonText.text = dormFull ? "Нет мест" : "Призвать";
            callButton.onClick.AddListener(onBuy);
        }
    }
}