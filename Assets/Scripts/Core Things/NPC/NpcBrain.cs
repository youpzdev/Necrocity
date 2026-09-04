using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCBrain : MonoBehaviour
{
    private const float ArrivalCheckDelay = 0.25f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float angularSpeed = 240f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float stoppingDistance = 0.4f;

    [Header("Destinations")]
    [SerializeField] private float wanderRadius = 12f;
    [SerializeField] private float sampleRadius = 2f;
    [SerializeField] private int sampleAttempts = 8;

    [Header("Idle")]
    [SerializeField] private float idleTimeMin = 2f;
    [SerializeField] private float idleTimeMax = 6f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string movingParameter = "Moving";
    [SerializeField] private float movingSpeedThreshold = 0.1f;

    private NavMeshAgent _agent;
    private int _movingHash;
    private bool _canAnimate;
    private bool _moving;
    private bool _idle;
    private float _idleTimer;
    private float _arrivalGuard;
    private bool _restartPending;
    private bool _offNavMeshLogged;
    private NPCWaypoint _lastPoint;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = moveSpeed;
        _agent.avoidancePriority = Random.Range(35, 66);
        _agent.angularSpeed = angularSpeed;
        _agent.acceleration = acceleration;
        _agent.stoppingDistance = Mathf.Max(0.05f, stoppingDistance);
        _agent.autoBraking = true;
        _agent.updatePosition = true;
        _agent.updateRotation = true;

        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (!string.IsNullOrEmpty(movingParameter)) _movingHash = Animator.StringToHash(movingParameter);
        _canAnimate = HasMovingParameter();
    }

    private void OnEnable()
    {
        _restartPending = true;
    }

    private void Update()
    {
        if (!_agent.isOnNavMesh)
        {
            ApplyMoving(false);
            WarnOffNavMesh();
            return;
        }

        if (_restartPending)
        {
            _restartPending = false;
            _offNavMeshLogged = false;
            _lastPoint = null;
            _agent.isStopped = false;
            PickDestination();
        }

        ApplyMoving(_agent.velocity.sqrMagnitude > movingSpeedThreshold * movingSpeedThreshold);

        if (_idle)
        {
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f) PickDestination();
            return;
        }

        if (_arrivalGuard > 0f)
        {
            _arrivalGuard -= Time.deltaTime;
            return;
        }

        if (_agent.pathPending) return;

        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            StartIdle();
            return;
        }

        if (_agent.remainingDistance > _agent.stoppingDistance) return;
        if (_agent.hasPath && _agent.velocity.sqrMagnitude > 0.01f) return;

        StartIdle();
    }

    private void PickDestination()
    {
        Vector3 destination;
        if (!TryPickPointDestination(out destination) && !TryPickWanderDestination(out destination))
        {
            StartIdle();
            return;
        }

        _idle = false;
        _arrivalGuard = ArrivalCheckDelay;
        _agent.isStopped = false;
        _agent.SetDestination(destination);
    }

    private bool TryPickPointDestination(out Vector3 destination)
    {
        destination = transform.position;

        IReadOnlyList<NPCWaypoint> points = NPCWaypoint.All;
        if (points.Count == 0) return false;

        int attempts = Mathf.Max(1, sampleAttempts);
        for (int i = 0; i < attempts; i++)
        {
            NPCWaypoint point = PickWeighted(points);
            if (points.Count > 1 && point == _lastPoint) continue;

            float radius = Mathf.Max(sampleRadius, point.WanderRadius);
            if (!TrySample(point.GetRandomPoint(), radius, out Vector3 hit)) continue;

            _lastPoint = point;
            destination = hit;
            return true;
        }

        return false;
    }

    private bool TryPickWanderDestination(out Vector3 destination)
    {
        destination = transform.position;

        int attempts = Mathf.Max(1, sampleAttempts);
        for (int i = 0; i < attempts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * Mathf.Max(1f, wanderRadius);
            Vector3 candidate = transform.position + new Vector3(offset.x, 0f, offset.y);
            if (!TrySample(candidate, sampleRadius, out Vector3 hit)) continue;

            destination = hit;
            return true;
        }

        return false;
    }

    private static NPCWaypoint PickWeighted(IReadOnlyList<NPCWaypoint> points)
    {
        float total = 0f;
        for (int i = 0; i < points.Count; i++) total += points[i].Weight;

        if (total <= 0f) return points[Random.Range(0, points.Count)];

        float roll = Random.value * total;
        for (int i = 0; i < points.Count; i++)
        {
            roll -= points[i].Weight;
            if (roll <= 0f) return points[i];
        }

        return points[points.Count - 1];
    }

    private static bool TrySample(Vector3 origin, float radius, out Vector3 result)
    {
        if (NavMesh.SamplePosition(origin, out NavMeshHit hit, Mathf.Max(0.5f, radius), NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = origin;
        return false;
    }

    private void StartIdle()
    {
        _idle = true;
        _idleTimer = Random.Range(idleTimeMin, idleTimeMax);
        if (_agent.hasPath) _agent.ResetPath();
    }

    private bool HasMovingParameter()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        if (string.IsNullOrEmpty(movingParameter)) return false;

        foreach (var parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.nameHash == _movingHash)
                return true;
        }

        return false;
    }

    private void ApplyMoving(bool value)
    {
        if (_moving == value) return;
        _moving = value;
        if (!_canAnimate) return;
        animator.SetBool(_movingHash, value);
    }

    private void WarnOffNavMesh()
    {
        if (_offNavMeshLogged) return;
        _offNavMeshLogged = true;
        Debug.LogWarning($"[NPCBrain] {name}: точка появления вне навмеша, персонаж стоит на месте", this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_agent == null || !_agent.hasPath) return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_agent.destination, 0.15f);
        Gizmos.DrawLine(transform.position, _agent.destination);
    }
#endif
}
