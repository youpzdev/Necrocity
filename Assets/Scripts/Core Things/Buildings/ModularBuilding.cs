using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ModularBuilding : MonoBehaviour
{
    [Serializable]
    public class Module
    {
        public int unlockLevel = 1;
        public GameObject[] parts;
    }

    [SerializeField]
    private Module[] modules =
    {
        new Module { unlockLevel = 1 },
        new Module { unlockLevel = 10 },
        new Module { unlockLevel = 15 }
    };

    [Header("Reveal")]
    [SerializeField] private float revealDuration = 1.2f;
    [SerializeField] private float revealOffset = 4f;
    [SerializeField] private Ease revealEase = Ease.OutBack;

    public event Action<int> ModuleRevealed;

    private readonly Dictionary<Transform, Vector3> _basePositions = new();
    private int _level = 1;

    public float RevealDuration => revealDuration;

    private void Awake()
    {
        CacheBasePositions();
    }

    private void OnDestroy()
    {
        foreach (var partTransform in _basePositions.Keys)
        {
            DOTween.Kill(partTransform);
        }
    }

    public void ApplyLevel(int level)
    {
        _level = level;

        if (modules == null) return;

        foreach (var module in modules)
        {
            SetModuleActive(module, module.unlockLevel <= _level);
        }
    }

    public void SetLevel(int level)
    {
        if (modules == null)
        {
            _level = level;
            return;
        }

        int previous = _level;
        _level = level;

        for (int i = 0; i < modules.Length; i++)
        {
            Module module = modules[i];
            bool unlocked = module.unlockLevel <= _level;

            if (unlocked && module.unlockLevel > previous)
            {
                Reveal(module);
                ModuleRevealed?.Invoke(i);
                continue;
            }

            SetModuleActive(module, unlocked);
        }
    }

    private void CacheBasePositions()
    {
        if (modules == null) return;

        foreach (var module in modules)
        {
            if (module.parts == null) continue;
            foreach (var part in module.parts)
            {
                if (part == null) continue;
                _basePositions[part.transform] = part.transform.localPosition;
            }
        }
    }

    private void SetModuleActive(Module module, bool active)
    {
        if (module.parts == null) return;

        foreach (var part in module.parts)
        {
            if (part == null) continue;

            Transform partTransform = part.transform;
            DOTween.Kill(partTransform);
            if (_basePositions.TryGetValue(partTransform, out Vector3 basePosition)) partTransform.localPosition = basePosition;

            part.SetActive(active);
        }
    }

    private void Reveal(Module module)
    {
        if (module.parts == null) return;

        foreach (var part in module.parts)
        {
            if (part == null) continue;

            Transform partTransform = part.transform;
            if (!_basePositions.TryGetValue(partTransform, out Vector3 basePosition))
            {
                basePosition = partTransform.localPosition;
                _basePositions[partTransform] = basePosition;
            }

            DOTween.Kill(partTransform);
            part.SetActive(true);
            partTransform.localPosition = basePosition - Vector3.up * revealOffset;
            partTransform.DOLocalMove(basePosition, revealDuration).SetEase(revealEase);
        }
    }
}
