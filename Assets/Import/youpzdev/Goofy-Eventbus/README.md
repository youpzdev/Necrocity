# 📡 Goofy EventBus

> Decouple everything. Zero dependencies. One file.

**by [youpzdev](https://github.com/youpzdev)**

---

## What is this

A type-safe event bus for Unity. No strings, no UnityEvents, no direct references between systems. Objects talk to each other without knowing each other exist.

```csharp
// Enemy just yells into the void
EventBus<EnemyDiedEvent>.Raise(new EnemyDiedEvent { score = 100 });

// UI, Audio, GameManager listen independently
EventBus<EnemyDiedEvent>.Subscribe(OnEnemyDied, this);
```

---

## Install

Install `goofy-eventbus.unitypackage` or just copy `EventBus.cs` anywhere into your `Assets/` folder. Done.

---

## Usage

### Define an event
```csharp
public struct EnemyDiedEvent
{
    public Vector3 position;
    public int score;
}
```

### Subscribe
```csharp
// Auto-unsubscribes when this object is destroyed
EventBus<EnemyDiedEvent>.Subscribe(OnEnemyDied, this);

// Subscribe without target — lives forever
EventBus<EnemyDiedEvent>.Subscribe(OnEnemyDied);
```

### Subscribe once
```csharp
// Fires once, then unsubscribes automatically
EventBus<LevelLoadedEvent>.SubscribeOnce(OnLevelLoaded, this);
```

### Raise
```csharp
EventBus<EnemyDiedEvent>.Raise(new EnemyDiedEvent
{
    position = transform.position,
    score = 100
});
```

### Unsubscribe manually
```csharp
EventBus<EnemyDiedEvent>.Unsubscribe(OnEnemyDied);
```

### Check before raising
```csharp
if (EventBus<EnemyDiedEvent>.HasSubscribers)
    EventBus<EnemyDiedEvent>.Raise(evt);
```

### Clear all subscribers
```csharp
EventBus<EnemyDiedEvent>.Clear();
```

---

## Real example

```csharp
// Enemy.cs — raises event, knows nothing about UI or audio
public class Enemy : MonoBehaviour
{
    [SerializeField] private int scoreValue = 100;

    public void Die()
    {
        EventBus<EnemyDiedEvent>.Raise(new EnemyDiedEvent
        {
            position = transform.position,
            score = scoreValue
        });

        Destroy(gameObject);
    }
}

// UIManager.cs — listens, knows nothing about Enemy
public class UIManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    private int totalScore;

    private void OnEnable()
    {
        EventBus<EnemyDiedEvent>.Subscribe(OnEnemyDied, this);
    }

    private void OnEnemyDied(EnemyDiedEvent evt)
    {
        totalScore += evt.score;
        scoreText.text = totalScore.ToString();
    }
}
```

No `UIManager` reference in `Enemy`. No `Enemy` reference in `UIManager`. Add new listeners anytime without touching existing code.

---

## How it works

- Generic static class — separate subscriber list per event type, zero lookup overhead
- `WeakReference` on target — auto-cleans dead subscribers on next `Raise`
- Duplicate guard on `Subscribe` and `SubscribeOnce` — same callback can't be added twice
- Iterates in reverse — safe to remove during iteration
- `try/catch` per callback — one broken listener doesn't kill the rest

---

## Part of the Goofy Tools collection

| | |
|---|---|
| [**goofy-pooling**](https://github.com/youpzz/goofy-pooling) | Zero-config object pooling |
| [**goofy-timers**](https://github.com/youpzz/unity-goofy-timers) | No-coroutine timer system |
| **goofy-eventbus** | You are here |

---

## License

MIT — do whatever you want.
