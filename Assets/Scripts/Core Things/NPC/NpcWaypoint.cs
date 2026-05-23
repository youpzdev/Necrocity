using UnityEngine;

public class NPCWaypoint : MonoBehaviour
{
    [Tooltip("Waypoints соединённые с этой точкой (мосты, тропы). NPC может перейти к ним напрямую.")]
    public NPCWaypoint[] connectedWaypoints;

    [Tooltip("Радиус случайного смещения от этой точки")]
    [SerializeField] private float wanderRadius = 2f;
    public float WanderRadius => wanderRadius;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        if (connectedWaypoints == null) return;
        Gizmos.color = Color.cyan;
        foreach (var wp in connectedWaypoints)
        {
            if (wp != null)
                Gizmos.DrawLine(transform.position, wp.transform.position);
        }
    }
#endif
}