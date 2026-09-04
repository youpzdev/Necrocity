using UnityEngine;

public class TutorialAnchor : MonoBehaviour
{
    [SerializeField] private string anchorId;
    [SerializeField] private TutorialHintView hint;
    [SerializeField] private GameObject clickTarget;
    [SerializeField] private GameObject uiBlocker;

    public string AnchorId => string.IsNullOrEmpty(anchorId) ? name : anchorId;
    public GameObject ClickTarget => clickTarget;

    public void Show(string message)
    {
        if (hint != null) hint.Show(message);
        if (uiBlocker != null) uiBlocker.SetActive(true);
    }

    public void Hide()
    {
        if (hint != null) hint.Hide();
        if (uiBlocker != null) uiBlocker.SetActive(false);
    }
}
