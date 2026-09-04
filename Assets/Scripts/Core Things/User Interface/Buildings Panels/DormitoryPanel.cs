using System.Collections;
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
    [SerializeField] private GameObject residentsEmptyState;

    [Header("Preview")]
    [SerializeField] private ModelPreview modelPreview;
    [SerializeField] private TMP_Text previewNameText;

    [Header("Upgrade Tab")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text capacityText;
    [SerializeField] private TMP_Text upgradePriceText;
    [SerializeField] private Button doUpgradeButton;
    [SerializeField] private TMP_Text doUpgradeButtonText;

    [Header("Messages")]
    [SerializeField] private GameObject messageRoot;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageDuration = 2.5f;
    [SerializeField] private string noConfigMessage = "Жители пока не настроены";
    [SerializeField] private string unknownCharacterMessage = "Такого жителя нет в списке";
    [SerializeField] private string noFreeSpaceMessage = "Свободных мест нет, улучшите общежитие";
    [SerializeField] private string notEnoughDublonsMessage = "Не хватает дублонов";

    private Dormitory _dormitory;
    private int _selectedIndex = -1;
    private Coroutine _messageRoutine;

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
        HideMessage();
        OpenTab(true);
    }

    public void Hide()
    {
        uiPanel.Hide();
        ClearPreview();
        HideMessage();
        _selectedIndex = -1;
        _dormitory = null;
    }

    private void OpenTab(bool isResidents)
    {
        residentsTab.SetActive(isResidents);
        upgradeTab.SetActive(!isResidents);
        residentsButton.SwitchState(isResidents);
        upgradeButton.SwitchState(!isResidents);

        if (isResidents) RefreshResidentsTab();
        else RefreshUpgradeTab();
    }

    private void RefreshResidentsTab()
    {
        foreach (Transform old in characterGrid) Pooling.Destroy(old.gameObject);

        DormitoryConfig config = _dormitory != null ? _dormitory.Config : null;
        int count = config != null ? config.CharacterCount : 0;

        if (residentsEmptyState != null) residentsEmptyState.SetActive(count == 0);

        if (count == 0)
        {
            ClearPreview();
            return;
        }

        int price = _dormitory.NextCharacterPrice;
        bool full = _dormitory.IsFull;
        int firstAvailable = -1;

        for (int i = 0; i < count; i++)
        {
            CharacterData data = config.GetCharacter(i);
            if (data == null) continue;

            if (firstAvailable < 0) firstAvailable = i;

            int index = i;
            var slot = Pooling.Instantiate(characterSlotPrefab, characterGrid)
                              .GetComponent<CharacterSlot>();

            slot.Init(data, price, _dormitory.GetOwnedCount(index), full,
                      () => OnCharacterBuy(index), () => ShowPreview(index));
        }

        int preview = config.GetCharacter(_selectedIndex) != null ? _selectedIndex : firstAvailable;
        if (preview >= 0) ShowPreview(preview);
        else ClearPreview();
    }

    private void OnCharacterBuy(int index)
    {
        CharacterPurchaseResult result = _dormitory.TryPurchaseCharacter(index, out _);

        if (result == CharacterPurchaseResult.Success)
        {
            Hide();
            return;
        }

        ShowMessage(GetMessage(result));
        RefreshResidentsTab();
    }

    private string GetMessage(CharacterPurchaseResult result) => result switch
    {
        CharacterPurchaseResult.NoConfig => noConfigMessage,
        CharacterPurchaseResult.UnknownCharacter => unknownCharacterMessage,
        CharacterPurchaseResult.NoFreeSpace => noFreeSpaceMessage,
        _ => notEnoughDublonsMessage
    };

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

    private void ShowPreview(int index)
    {
        CharacterData data = _dormitory != null ? _dormitory.GetCharacterData(index) : null;
        if (data == null)
        {
            ClearPreview();
            return;
        }

        _selectedIndex = index;

        if (previewNameText != null) previewNameText.text = data.characterName;
        if (modelPreview != null) modelPreview.Show(data.prefab3D);
    }

    private void ClearPreview()
    {
        if (previewNameText != null) previewNameText.text = "";
        if (modelPreview != null) modelPreview.Clear();
    }

    private void RefreshUpgradeTab()
    {
        DormitoryConfig config = _dormitory != null ? _dormitory.Config : null;
        if (config == null)
        {
            doUpgradeButton.interactable = false;
            doUpgradeButtonText.text = noConfigMessage;
            upgradePriceText.text = "";
            return;
        }

        int level = _dormitory.Level;
        int capacity = _dormitory.Capacity;
        bool maxed = level >= config.maxLevel;

        levelText.text = $"Уровень {level}";
        capacityText.text = $"{_dormitory.ResidentCount} / {capacity}";

        if (maxed)
        {
            doUpgradeButton.interactable = false;
            doUpgradeButtonText.text = "Макс. уровень";
            upgradePriceText.text = "";
            return;
        }

        int price = config.GetUpgradePrice(level);
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
