using System;
using Object = UnityEngine.Object;

public class TutorialWatcher
{
    private readonly TutorialCondition condition;
    private readonly Action met;

    private Action unsubscribe;
    private bool listening;

    public TutorialWatcher(TutorialCondition condition, Action met)
    {
        this.condition = condition;
        this.met = met;
    }

    public void Listen(Object owner)
    {
        if (listening || condition == null) return;
        if (condition.Kind == TutorialConditionKind.Immediate) return;

        listening = true;
        if (condition.Kind == TutorialConditionKind.BusEvent) SubscribeBus(owner);
    }

    public void Stop()
    {
        listening = false;
        unsubscribe?.Invoke();
        unsubscribe = null;
    }

    public void ReportSignal(string signal)
    {
        if (!listening) return;
        if (condition.Kind != TutorialConditionKind.Signal) return;
        if (string.IsNullOrEmpty(condition.Signal)) return;
        if (condition.Signal != signal) return;

        Fire();
    }

    private void SubscribeBus(Object owner)
    {
        switch (condition.BusEvent)
        {
            case TutorialBusEvent.ResourceManagerChanged: Bind<ResourceManagerChangedEvent>(owner); break;
            case TutorialBusEvent.ResourcesChanged: Bind<ResourcesChangedEvent>(owner); break;
            case TutorialBusEvent.InventoryChanged: Bind<InventoryChangedEvent>(owner); break;
            case TutorialBusEvent.LevelChanged: Bind<LevelChangedEvent>(owner); break;
            case TutorialBusEvent.DormitoryChanged: Bind<DormitoryChangedEvent>(owner); break;
            case TutorialBusEvent.CharacterPurchased: Bind<CharacterPurchasedEvent>(owner); break;
        }
    }

    private void Bind<T>(Object owner)
    {
        Action<T> handler = _ => Fire();
        EventBus<T>.Subscribe(handler, owner);
        unsubscribe = () => EventBus<T>.Unsubscribe(handler);
    }

    private void Fire()
    {
        Stop();
        met?.Invoke();
    }
}
