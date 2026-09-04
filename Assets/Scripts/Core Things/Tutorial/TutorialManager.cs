using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [SerializeField] private TutorialChain[] chains;
    [SerializeField] private TutorialAnchor[] anchors;

    private readonly List<TutorialWatcher> startWatchers = new List<TutorialWatcher>();
    private readonly List<TutorialWatcher> signalBuffer = new List<TutorialWatcher>();
    private readonly Queue<TutorialChain> pendingChains = new Queue<TutorialChain>();

    private TutorialChain currentChain;
    private TutorialStep currentStep;
    private TutorialAnchor currentAnchor;
    private TutorialWatcher stepWatcher;
    private int stepIndex;
    private bool rebuilding;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RefreshStartWatchers();
    }

    private void OnDestroy()
    {
        StopStepWatcher();
        StopStartWatchers();

        if (Instance == this) Instance = null;
    }

    public void ReportSignal(string signal)
    {
        if (string.IsNullOrEmpty(signal)) return;

        stepWatcher?.ReportSignal(signal);

        signalBuffer.Clear();
        signalBuffer.AddRange(startWatchers);
        foreach (TutorialWatcher watcher in signalBuffer) watcher.ReportSignal(signal);
        signalBuffer.Clear();
    }

    private void RefreshStartWatchers()
    {
        if (rebuilding) return;
        rebuilding = true;

        StopStartWatchers();

        if (chains != null)
        {
            foreach (TutorialChain chain in chains)
            {
                if (chain == null || IsChainDone(chain)) continue;

                if (chain.StartCondition == null || chain.StartCondition.Kind == TutorialConditionKind.Immediate)
                {
                    EnqueueChain(chain);
                    continue;
                }

                TutorialChain captured = chain;
                TutorialWatcher watcher = new TutorialWatcher(chain.StartCondition, () => TriggerChain(captured));
                watcher.Listen(this);
                startWatchers.Add(watcher);
            }
        }

        rebuilding = false;
        StartNextChain();
    }

    private void StopStartWatchers()
    {
        foreach (TutorialWatcher watcher in startWatchers) watcher.Stop();
        startWatchers.Clear();
    }

    private void StopStepWatcher()
    {
        stepWatcher?.Stop();
        stepWatcher = null;
    }

    private void TriggerChain(TutorialChain chain)
    {
        EnqueueChain(chain);
        StartNextChain();
    }

    private void EnqueueChain(TutorialChain chain)
    {
        if (chain == null || chain == currentChain) return;
        if (pendingChains.Contains(chain)) return;

        pendingChains.Enqueue(chain);
    }

    private void StartNextChain()
    {
        if (currentChain != null) return;

        while (pendingChains.Count > 0)
        {
            TutorialChain chain = pendingChains.Dequeue();
            if (chain == null || IsChainDone(chain)) continue;

            currentChain = chain;
            stepIndex = -1;
            AdvanceStep();
            return;
        }
    }

    private void AdvanceStep()
    {
        TutorialStep[] steps = currentChain.Steps;

        while (true)
        {
            stepIndex++;
            if (steps == null || stepIndex >= steps.Length)
            {
                FinishChain();
                return;
            }

            TutorialStep step = steps[stepIndex];
            if (step == null || IsStepDone(step)) continue;

            ShowStep(step);
            return;
        }
    }

    private void ShowStep(TutorialStep step)
    {
        currentStep = step;
        currentAnchor = FindAnchor(step.AnchorId);

        ApplyBlock();

        stepWatcher = new TutorialWatcher(step.Completion, CompleteStep);
        stepWatcher.Listen(this);

        if (step.ShowDelay > 0f) Timer.After(step.ShowDelay, () => ShowHint(step), this);
        else ShowHint(step);
    }

    private void ShowHint(TutorialStep step)
    {
        if (currentStep != step || currentAnchor == null) return;

        currentAnchor.Show(step.Message);
    }

    private void CompleteStep()
    {
        if (currentStep == null) return;

        MarkStepDone(currentStep);
        HideCurrentHint();
        StopStepWatcher();

        currentStep = null;
        currentAnchor = null;

        AdvanceStep();
    }

    private void FinishChain()
    {
        currentChain = null;
        currentStep = null;
        currentAnchor = null;
        stepIndex = -1;

        ReleaseBlock();
        RefreshStartWatchers();
    }

    private void HideCurrentHint()
    {
        if (currentAnchor != null) currentAnchor.Hide();
    }

    private void ApplyBlock()
    {
        WorldClickRouter router = WorldClickRouter.Instance;
        if (router == null) return;

        if (!currentStep.BlockWorld || currentAnchor == null)
        {
            router.ClearClickFilter();
            return;
        }

        GameObject target = currentAnchor.ClickTarget;
        if (target != null) router.SetClickFilter(target);
        else router.SetClickFilter();
    }

    private void ReleaseBlock()
    {
        WorldClickRouter router = WorldClickRouter.Instance;
        if (router != null) router.ClearClickFilter();
    }

    private TutorialAnchor FindAnchor(string anchorId)
    {
        if (anchors == null || string.IsNullOrEmpty(anchorId)) return null;

        foreach (TutorialAnchor anchor in anchors)
        {
            if (anchor != null && anchor.AnchorId == anchorId) return anchor;
        }

        return null;
    }

    private static bool IsStepDone(TutorialStep step)
    {
        return GameSave.Get(GameSave.Keys.TutorialStep(step.StepId), false);
    }

    private static void MarkStepDone(TutorialStep step)
    {
        GameSave.Set(GameSave.Keys.TutorialStep(step.StepId), true);
    }

    private static bool IsChainDone(TutorialChain chain)
    {
        TutorialStep[] steps = chain.Steps;
        if (steps == null || steps.Length == 0) return true;

        foreach (TutorialStep step in steps)
        {
            if (step != null && !IsStepDone(step)) return false;
        }

        return true;
    }
}
