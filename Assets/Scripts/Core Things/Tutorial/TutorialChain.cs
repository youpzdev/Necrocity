using UnityEngine;

[CreateAssetMenu(fileName = "tutorial chain", menuName = "youpzdev/tutorial/chain")]
public class TutorialChain : ScriptableObject
{
    [SerializeField] private string chainId;
    [SerializeField] private TutorialCondition startCondition = new TutorialCondition();
    [SerializeField] private TutorialStep[] steps;

    public string ChainId => string.IsNullOrEmpty(chainId) ? name : chainId;
    public TutorialCondition StartCondition => startCondition;
    public TutorialStep[] Steps => steps;
}
