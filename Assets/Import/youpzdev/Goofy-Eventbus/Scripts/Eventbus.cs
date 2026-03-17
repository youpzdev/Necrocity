using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public static class EventBus<T>
{
    private static readonly List<(Action<T> callback, WeakReference<Object> target, bool once)> subscribers = new();

    public static void Subscribe(Action<T> callback, Object target = null)
    {
        if (subscribers.Exists(s => s.callback == callback)) return; // ayo no duplicates >:(
        subscribers.Add((callback, target != null ? new WeakReference<Object>(target) : null, false));
    }

    public static void SubscribeOnce(Action<T> callback, Object target = null)
    {
        if (subscribers.Exists(s => s.callback == callback)) return; // ayo no duplicates >:(
        subscribers.Add((callback, target != null ? new WeakReference<Object>(target) : null, true));
    }

    public static void Unsubscribe(Action<T> callback)
    {
        for (int i = subscribers.Count - 1; i >= 0; i--) if (subscribers[i].callback == callback) { subscribers.RemoveAt(i); return; }
    }

    public static bool HasSubscribers => subscribers.Count > 0;

    public static void Raise(T evt)
    {
        for (int i = subscribers.Count - 1; i >= 0; i--)
        {
            var (callback, weakRef, once) = subscribers[i];

            if (weakRef != null && (!weakRef.TryGetTarget(out var obj) || obj == null))
            {
                subscribers.RemoveAt(i);
                continue;
            }

            if (once) subscribers.RemoveAt(i);

            try { callback?.Invoke(evt); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    public static void Clear() => subscribers.Clear();
}