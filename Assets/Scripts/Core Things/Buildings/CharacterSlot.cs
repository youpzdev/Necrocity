using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CharacterSlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject selectedHighlight;
    [SerializeField] private GameObject lockedMark;
    [SerializeField] private GameObject ownedBadge;
    [SerializeField] private TMP_Text ownedCountText;

    [Header("Labels")]
    [SerializeField] private string ownedCountFormat = "x{0}";

    [Header("Colors")]
    [SerializeField] private Color availableIconColor = Color.white;
    [SerializeField] private Color lockedIconColor = new Color(1f, 1f, 1f, 0.4f);

    public void Init(CharacterData data, UnityAction onSelect)
    {
        if (icon != null)
        {
            icon.sprite = data != null ? data.icon : null;
            icon.enabled = icon.sprite != null;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            if (onSelect != null) selectButton.onClick.AddListener(onSelect);
        }

        SetState(0, false, true);
    }

    public void SetState(int ownedCount, bool selected, bool available)
    {
        if (ownedBadge != null) ownedBadge.SetActive(ownedCount > 0);
        if (ownedCountText != null) ownedCountText.text = string.Format(ownedCountFormat, ownedCount);
        if (selectedHighlight != null) selectedHighlight.SetActive(selected);
        if (lockedMark != null) lockedMark.SetActive(!available);
        if (icon != null) icon.color = available ? availableIconColor : lockedIconColor;
    }
}
