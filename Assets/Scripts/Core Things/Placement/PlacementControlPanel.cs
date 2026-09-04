using UnityEngine;
using UnityEngine.UI;

public class PlacementControlPanel : MonoBehaviour
{
    [SerializeField] private UIPanel panel;
    [SerializeField] private Button rotateClockwiseButton;
    [SerializeField] private Button rotateCounterClockwiseButton;
    [SerializeField] private Button storageButton;
    [SerializeField] private Button confirmButton;

    private void Awake()
    {
        if (panel == null) panel = GetComponent<UIPanel>();

        rotateClockwiseButton.onClick.AddListener(OnRotateClockwise);
        rotateCounterClockwiseButton.onClick.AddListener(OnRotateCounterClockwise);
        storageButton.onClick.AddListener(OnSendToStorage);
        confirmButton.onClick.AddListener(OnConfirm);
    }

    public void Open() => panel.Show();

    public void Close() => panel.Hide();

    private void OnRotateClockwise()
    {
        if (PlacementManager.Instance != null) PlacementManager.Instance.RotateClockwise();
    }

    private void OnRotateCounterClockwise()
    {
        if (PlacementManager.Instance != null) PlacementManager.Instance.RotateCounterClockwise();
    }

    private void OnSendToStorage()
    {
        if (PlacementManager.Instance != null) PlacementManager.Instance.SendActiveToStorage();
    }

    private void OnConfirm()
    {
        if (PlacementManager.Instance != null) PlacementManager.Instance.ConfirmActive();
    }
}
