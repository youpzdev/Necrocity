using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DormitoryPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private UIPanel uiPanel;

    [Header("Titles")]
    [SerializeField] private TMP_Text charactersTitleText;
    [SerializeField] private TMP_Text upgradeTitleText;
    [SerializeField] private string charactersTitle = "Жители";
    [SerializeField] private string upgradeTitle = "Общежитие";

    [Header("Character List")]
    [SerializeField] private Transform characterGrid;
    [SerializeField] private GameObject characterSlotPrefab;
    [SerializeField] private GameObject charactersEmptyState;

    [Header("Selected Character")]
    [SerializeField] private ModelPreview modelPreview;
    [SerializeField] private TMP_Text selectedNameText;
    [SerializeField] private TMP_Text selectedPriceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    [Header("Upgrade")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text capacityText;
    [SerializeField] private TMP_Text nextCapacityText;
    [SerializeField] private TMP_Text upgradePriceText;
    [SerializeField] private Button doUpgradeButton;
    [SerializeField] private TMP_Text doUpgradeButtonText;

    [Header("Labels")]
    [SerializeField] private string buyLabel = "Призвать";
    [SerializeField] private string noSelectionLabel = "Выберите жителя";
    [SerializeField] private string noCharactersLabel = "Список пуст";
    [SerializeField] private string noSpaceLabel = "Нет мест";
    [SerializeField] private string noMoneyLabel = "Мало дублонов";
    [SerializeField] private string upgradeLabel = "Улучшить";
    [SerializeField] private string maxLevelLabel = "Макс. уровень";
    [SerializeField] private string noConfigLabel = "Нет данных";
    [SerializeField] private string emptySelectionName = "Никто не выбран";

    [Header("Formats")]
    [SerializeField] private string levelFormat = "Уровень {0}";
    [SerializeField] private string capacityFormat = "{0} / {1}";
    [SerializeField] private string nextCapacityFormat = "Станет {0}";
    [SerializeField] private string priceFormat = "{0} дублонов";

    [Header("Messages")]
    [SerializeField] private GameObject messageRoot;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageDuration = 2.5f;
    [SerializeField] private string noConfigMessage = "Жители пока не настроены";
    [SerializeField] private string unknownCharacterMessage = "Такого жителя нет в списке";
    [SerializeField] private string noFreeSpaceMessage = "Свободных мест нет, улучшите общежитие";
    [SerializeField] private string notEnoughDublonsMessage = "Не хватает дублонов";

    private readonly List<CharacterSlot> _slots = new();
    private readonly List<int> _slotIndices = new();

    private Dormitory _dormitory;
    private int _selectedIndex = -1;
    private Coroutine _messageRoutine;

    private void Awake()
    {
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClick);
        if (doUpgradeButton != null) doUpgradeButton.onClick.AddListener(OnUpgradeClick);

        if (charactersTitleText != null) charactersTitleText.text = charactersTitle;
        if (upgradeTitleText != null) upgradeTitleText.text = upgradeTitle;
    }

    private void OnEnable()
    {
        EventBus<DormitoryChangedEvent>.Subscribe(OnDormitoryChanged, this);
        EventBus<ResourceManagerChangedEvent>.Subscribe(OnResourcesChanged, this);
        Rebuild();
    }

    private void OnDisable()
    {
        EventBus<DormitoryChangedEvent>.Unsubscribe(OnDormitoryChanged);
        EventBus<ResourceManagerChangedEvent>.Unsubscribe(OnResourcesChanged);
    }

    public void Show(Dormitory dormitory)
    {
        _dormitory = dormitory;
        _selectedIndex = -1;

        HideMessage();
        if (uiPanel != null) uiPanel.Show();
        Rebuild();
    }

    public void Hide()
    {
        if (uiPanel != null) uiPanel.Hide();

        ClearPreview();
        HideMessage();
        _selectedIndex = -1;
        _dormitory = null;
    }

    private void OnDormitoryChanged(DormitoryChangedEvent _) => Refresh();

    private void OnResourcesChanged(ResourceManagerChangedEvent _) => Refresh();

    private void Rebuild()
    {
        ClearSlots();

        DormitoryConfig config = _dormitory != null ? _dormitory.Config : null;
        int count = config != null && characterGrid != null && characterSlotPrefab != null
            ? config.CharacterCount
            : 0;

        for (int i = 0; i < count; i++)
        {
            if (config.GetCharacter(i) == null) continue;

            GameObject instance = Pooling.Instantiate(characterSlotPrefab, characterGrid);
            if (instance == null) continue;

            CharacterSlot slot = instance.GetComponent<CharacterSlot>();
            if (slot == null) continue;

            int index = i;
            slot.Init(config.GetCharacter(i), () => SelectCharacter(index));

            _slots.Add(slot);
            _slotIndices.Add(index);
        }

        if (charactersEmptyState != null) charactersEmptyState.SetActive(_slots.Count == 0);

        if (_dormitory == null || _dormitory.GetCharacterData(_selectedIndex) == null)
            _selectedIndex = _slotIndices.Count > 0 ? _slotIndices[0] : -1;

        Refresh();
    }

    private void ClearSlots()
    {
        _slots.Clear();
        _slotIndices.Clear();

        if (characterGrid == null) return;

        for (int i = characterGrid.childCount - 1; i >= 0; i--)
        {
            GameObject child = characterGrid.GetChild(i).gameObject;
            if (child == charactersEmptyState) continue;

            Pooling.Destroy(child);
        }
    }

    private void Refresh()
    {
        RefreshSlots();
        RefreshSelected();
        RefreshUpgrade();
    }

    private void RefreshSlots()
    {
        int price = _dormitory != null ? _dormitory.NextCharacterPrice : 0;
        bool available = _dormitory != null && !_dormitory.IsFull && CanAfford(price);

        for (int i = 0; i < _slots.Count; i++)
        {
            int index = _slotIndices[i];
            int owned = _dormitory != null ? _dormitory.GetOwnedCount(index) : 0;

            _slots[i].SetState(owned, index == _selectedIndex, available);
        }
    }

    private void RefreshSelected()
    {
        CharacterData data = _dormitory != null ? _dormitory.GetCharacterData(_selectedIndex) : null;

        if (data == null)
        {
            ClearPreview();
            if (selectedPriceText != null) selectedPriceText.text = "";
            SetBuyButton(false, _slots.Count == 0 ? noCharactersLabel : noSelectionLabel);
            return;
        }

        if (selectedNameText != null) selectedNameText.text = data.characterName;
        if (modelPreview != null) modelPreview.Show(data.prefab3D);

        int price = _dormitory.NextCharacterPrice;
        if (selectedPriceText != null) selectedPriceText.text = string.Format(priceFormat, price);

        if (_dormitory.IsFull) SetBuyButton(false, noSpaceLabel);
        else if (!CanAfford(price)) SetBuyButton(false, noMoneyLabel);
        else SetBuyButton(true, buyLabel);
    }

    private void RefreshUpgrade()
    {
        DormitoryConfig config = _dormitory != null ? _dormitory.Config : null;

        if (config == null)
        {
            if (levelText != null) levelText.text = "";
            if (capacityText != null) capacityText.text = "";
            if (nextCapacityText != null) nextCapacityText.text = "";
            if (upgradePriceText != null) upgradePriceText.text = "";
            SetUpgradeButton(false, noConfigLabel);
            return;
        }

        int level = _dormitory.Level;

        if (levelText != null) levelText.text = string.Format(levelFormat, level);
        if (capacityText != null)
            capacityText.text = string.Format(capacityFormat, _dormitory.ResidentCount, _dormitory.Capacity);

        if (level >= config.maxLevel)
        {
            if (nextCapacityText != null) nextCapacityText.text = "";
            if (upgradePriceText != null) upgradePriceText.text = "";
            SetUpgradeButton(false, maxLevelLabel);
            return;
        }

        int price = config.GetUpgradePrice(level);
        bool canAfford = CanAfford(price);

        if (nextCapacityText != null)
            nextCapacityText.text = string.Format(nextCapacityFormat, config.GetCapacity(level + 1));
        if (upgradePriceText != null) upgradePriceText.text = string.Format(priceFormat, price);

        SetUpgradeButton(canAfford, canAfford ? upgradeLabel : noMoneyLabel);
    }

    private void SelectCharacter(int index)
    {
        if (_dormitory == null || _dormitory.GetCharacterData(index) == null) return;

        _selectedIndex = index;
        Refresh();
    }

    private void OnBuyClick()
    {
        if (_dormitory == null) return;

        CharacterData data = _dormitory.GetCharacterData(_selectedIndex);
        if (data == null)
        {
            ShowMessage(_slots.Count == 0 ? noConfigMessage : unknownCharacterMessage);
            return;
        }

        if (_dormitory.IsFull)
        {
            ShowMessage(noFreeSpaceMessage);
            return;
        }

        if (!CanAfford(_dormitory.NextCharacterPrice))
        {
            ShowMessage(notEnoughDublonsMessage);
            return;
        }

        CharacterPurchaseResult result = _dormitory.TryPurchaseCharacter(_selectedIndex, out _);
        if (result != CharacterPurchaseResult.Success) ShowMessage(GetMessage(result));

        Refresh();
    }

    private void OnUpgradeClick()
    {
        if (_dormitory == null) return;

        DormitoryConfig config = _dormitory.Config;
        if (config == null || _dormitory.Level >= config.maxLevel) return;

        if (!CanAfford(config.GetUpgradePrice(_dormitory.Level)))
        {
            ShowMessage(notEnoughDublonsMessage);
            return;
        }

        _dormitory.TryUpgrade();
        Refresh();
    }

    private bool CanAfford(int price) =>
        ResourceManager.Instance != null &&
        ResourceManager.Instance.CanSpendResource(ResourceType.Dublons, price);

    private void SetBuyButton(bool interactable, string label)
    {
        if (buyButton != null) buyButton.interactable = interactable;
        if (buyButtonText != null) buyButtonText.text = label;
    }

    private void SetUpgradeButton(bool interactable, string label)
    {
        if (doUpgradeButton != null) doUpgradeButton.interactable = interactable;
        if (doUpgradeButtonText != null) doUpgradeButtonText.text = label;
    }

    private string GetMessage(CharacterPurchaseResult result) => result switch
    {
        CharacterPurchaseResult.NoConfig => noConfigMessage,
        CharacterPurchaseResult.UnknownCharacter => unknownCharacterMessage,
        CharacterPurchaseResult.NoFreeSpace => noFreeSpaceMessage,
        _ => notEnoughDublonsMessage
    };

    private void ClearPreview()
    {
        if (selectedNameText != null) selectedNameText.text = emptySelectionName;
        if (modelPreview != null) modelPreview.Clear();
    }

    private void ShowMessage(string text)
    {
        if (messageText == null) return;

        messageText.text = text;
        if (messageRoot != null) messageRoot.SetActive(true);

        if (_messageRoutine != null) StopCoroutine(_messageRoutine);
        _messageRoutine = StartCoroutine(HideMessageAfterDelay());
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        _messageRoutine = null;
        HideMessage();
    }

    private void HideMessage()
    {
        if (_messageRoutine != null)
        {
            StopCoroutine(_messageRoutine);
            _messageRoutine = null;
        }

        if (messageText != null) messageText.text = "";
        if (messageRoot != null) messageRoot.SetActive(false);
    }
}
