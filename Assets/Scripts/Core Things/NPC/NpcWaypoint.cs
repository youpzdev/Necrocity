using System.Collections.Generic;
using UnityEngine;

public class NPCWaypoint : MonoBehaviour
{
    private static readonly List<NPCWaypoint> Registry = new List<NPCWaypoint>();
    public static IReadOnlyList<NPCWaypoint> All => Registry;

    [Tooltip("Радиус случайного смещения от этой точки")]
    [SerializeField] private float wanderRadius = 2f;

    [Tooltip("Чем больше вес, тем чаще персонажи выбирают эту точку")]
    [SerializeField] private float weight = 1f;

    public float WanderRadius => Mathf.Max(0f, wanderRadius);
    public float Weight => Mathf.Max(0f, weight);

    public Vector3 GetRandomPoint()
    {
        Vector2 offset = Random.insideUnitCircle * WanderRadius;
        return transform.position + new Vector3(offset.x, 0f, offset.y);
    }

    private void OnEnable()
    {
        if (!Registry.Contains(this)) Registry.Add(this);
    }

    private void OnDisable()
    {
        Registry.Remove(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, WanderRadius);
    }
#endif
}
