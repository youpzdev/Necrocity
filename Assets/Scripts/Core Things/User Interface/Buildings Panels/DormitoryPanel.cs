using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DormitoryPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private UIPanel uiPanel;

    [Header("Tabs")]
    [SerializeField] private GameObject residentsTab;
    [SerializeField] private GameObject upgradeTab;
    [SerializeField] private ButtonStyler residentsButton;
    [SerializeField] private ButtonStyler upgradeButton;

    [Header("Residents Tab")]
    [SerializeField] private Transform characterGrid;
    [SerializeField] private GameObject characterSlotPrefab;

    [Header("Upgrade Tab")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text capacityText;
    [SerializeField] private TMP_Text upgradePriceText;
    [SerializeField] private Button doUpgradeButton;
    [SerializeField] private TMP_Text doUpgradeButtonText;

    private Dormitory _dormitory;

    private void Awake()
    {
        residentsButton.GetComponent<Button>().onClick.AddListener(() => OpenTab(true));
        upgradeButton.GetComponent<Button>().onClick.AddListener(() => OpenTab(false));
        doUpgradeButton.onClick.AddListener(OnUpgradeClick);
    }

    public void Show(Dormitory dormitory)
    {
        _dormitory = dormitory;
        uiPanel.Show();
        OpenTab(true);
    }

    public void Hide()
    {
        uiPanel.Hide();
        _dormitory = null;
    }

    // ─── Вкладки ─────────────────────────────────────────────────────────────

    private void OpenTab(bool isResidents)
    {
        residentsTab.SetActive(isResidents);
        upgradeTab.SetActive(!isResidents);
        residentsButton.SwitchState(isResidents);
        upgradeButton.SwitchState(!isResidents);

        if (isResidents) RefreshResidentsTab();
        else RefreshUpgradeTab();
    }

    // ─── Вкладка Жители ──────────────────────────────────────────────────────

    private void RefreshResidentsTab()
    {
        foreach (Transform old in characterGrid) Pooling.Destroy(old.gameObject);

        var chars = _dormitory.Config.characters;
        for (int i = 0; i < chars.Length; i++)
        {
            int index = i;
            var slot = Pooling.Instantiate(characterSlotPrefab, characterGrid)
                              .GetComponent<CharacterSlot>();

            bool owned = _dormitory.IsCharacterOwned(index);
            int price = _dormitory.GetCharacterPrice(index);
            slot.Init(chars[index], price, owned, _dormitory.IsFull, () => OnCharacterClick(index));
        }
    }

    private void OnCharacterClick(int index)
    {
        bool success = _dormitory.TryPurchaseCharacter(index);
        if (success) RefreshResidentsTab();
    }

    // ─── Вкладка Улучшение ───────────────────────────────────────────────────

    private void RefreshUpgradeTab()
    {
        int level = _dormitory.Level;
        int capacity = _dormitory.Capacity;
        bool maxed = level >= _dormitory.Config.maxLevel;

        levelText.text = $"Уровень {level}";
        capacityText.text = $"{_dormitory.ResidentCount} / {capacity}";

        if (maxed)
        {
            doUpgradeButton.interactable = false;
            doUpgradeButtonText.text = "Макс. уровень";
            upgradePriceText.text = "";
            return;
        }

        int price = _dormitory.Config.GetUpgradePrice(level);
        bool canAfford = ResourceManager.Instance.CanSpendResource(ResourceType.Dublons, price);

        upgradePriceText.text = $"{price} дублонов";
        doUpgradeButton.interactable = canAfford;
        doUpgradeButtonText.text = "Улучшить";
    }

    private void OnUpgradeClick()
    {
        if (_dormitory.TryUpgrade())
            RefreshUpgradeTab();
    }
}