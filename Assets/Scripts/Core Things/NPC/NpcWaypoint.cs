using UnityEngine;

public class NPCWaypoint : MonoBehaviour
{
    [Tooltip("Waypoints соединённые с этой точкой (мосты, тропы). NPC может перейти к ним напрямую.")]
    public NPCWaypoint[] connectedWaypoints;

    [Tooltip("Радиус случайного смещения от этой точки")]
    [SerializeField] private float wanderRadius = 2f;
    public float WanderRadius => Mathf.Max(0f, wanderRadius);

    public NPCWaypoint GetRandomConnected()
    {
        if (connectedWaypoints == null || connectedWaypoints.Length == 0) return null;

        int start = Random.Range(0, connectedWaypoints.Length);
        for (int i = 0; i < connectedWaypoints.Length; i++)
        {
            NPCWaypoint candidate = connectedWaypoints[(start + i) % connectedWaypoints.Length];
            if (candidate != null && candidate != this) return candidate;
        }

        return null;
    }

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
