using UnityEngine;

public class TutorialSignalEmitter : MonoBehaviour
{
    public enum EmitMoment
    {
        Manual,
        OnEnable,
        OnDisable
    }

    [SerializeField] private string signal;
    [SerializeField] private EmitMoment moment = EmitMoment.OnEnable;

    private void OnEnable()
    {
        if (moment == EmitMoment.OnEnable) Emit();
    }

    private void OnDisable()
    {
        if (moment == EmitMoment.OnDisable) Emit();
    }

    public void Emit()
    {
        if (TutorialManager.Instance == null) return;
        TutorialManager.Instance.ReportSignal(signal);
    }
}
