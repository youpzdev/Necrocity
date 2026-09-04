using UnityEngine;

[CreateAssetMenu(fileName = "tutorial step", menuName = "youpzdev/tutorial/step")]
public class TutorialStep : ScriptableObject
{
    [SerializeField] private string stepId;
    [SerializeField] private string anchorId;
    [SerializeField, TextArea(2, 4)] private string message;
    [Space(10)]
    [SerializeField] private float showDelay;
    [SerializeField] private bool blockWorld = true;
    [Space(10)]
    [SerializeField] private TutorialCondition completion = new TutorialCondition();

    public string StepId => string.IsNullOrEmpty(stepId) ? name : stepId;
    public string AnchorId => anchorId;
    public string Message => message;
    public float ShowDelay => showDelay;
    public bool BlockWorld => blockWorld;
    public TutorialCondition Completion => completion;
}
