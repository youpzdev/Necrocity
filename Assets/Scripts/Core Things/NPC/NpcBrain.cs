using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class NPCBrain : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotateSpeed = 8f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float stuckTimeout = 3f;      // сек без прогресса → смена цели

    [Header("Waypoints")]
    [SerializeField] private NPCWaypoint[] homeWaypoints;  // waypoints этого острова
    [SerializeField] private float arrivalDistance = 0.4f;

    [Header("Wandering")]
    [SerializeField] private float waitTimeMin = 1f;
    [SerializeField] private float waitTimeMax = 4f;
    [SerializeField][Range(0f, 1f)] private float chanceToWanderLocally = 0.6f; // vs перейти к другому waypoint

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string movingParameter = "Moving";

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 1.5f;   // raycast вниз с целевой точки
    [SerializeField] private LayerMask groundMask;

    private CharacterController _cc;
    private int _movingHash;
    private bool _moving;
    private Vector3 _target;
    private float _verticalVelocity;
    private float _waitTimer;
    private bool _waiting;

    private NPCWaypoint _currentWaypoint;
    private Vector3 _lastPosition;
    private float _stuckTimer;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (!string.IsNullOrEmpty(movingParameter)) _movingHash = Animator.StringToHash(movingParameter);
    }

    private void Start()
    {
        if (homeWaypoints == null || homeWaypoints.Length == 0)
        {
            enabled = false;
            Debug.LogWarning($"[NPCBrain] {name}: нет waypoints!", this);
            return;
        }

        _currentWaypoint = homeWaypoints[Random.Range(0, homeWaypoints.Length)];
        SnapToNearestWaypoint();
        PickNextTarget();
    }

    private void Update()
    {
        ApplyGravity();

        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;
            SetMoving(false);
            if (_waitTimer <= 0f) PickNextTarget();
            return;
        }

        MoveToTarget();
        CheckStuck();
        SetMoving(true);

        float distXZ = HorizontalDistance(transform.position, _target);
        if (distXZ <= arrivalDistance)
            StartWaiting();
    }

    // ─── Движение ────────────────────────────────────────────────────────────

    private void SetMoving(bool value)
    {
        if (animator == null || _movingHash == 0) return;
        if (_moving == value) return;
        _moving = value;
        animator.SetBool(_movingHash, value);
    }

    private void MoveToTarget()
    {
        Vector3 dir = (_target - transform.position);
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        dir.Normalize();

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);

        Vector3 move = dir * moveSpeed;
        move.y = _verticalVelocity;
        _cc.Move(move * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (_cc.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;
        else
            _verticalVelocity += gravity * Time.deltaTime;
    }

    // ─── Выбор следующей точки ───────────────────────────────────────────────

    private void PickNextTarget()
    {
        _waiting = false;
        _stuckTimer = 0f;
        _lastPosition = transform.position;

        bool goLocal = Random.value < chanceToWanderLocally
                       || _currentWaypoint.connectedWaypoints == null
                       || _currentWaypoint.connectedWaypoints.Length == 0;

        if (goLocal)
        {
            TryPickLocalWander();
        }
        else
        {
            // переход к соседнему waypoint (другой остров или просто связанная точка)
            var connected = _currentWaypoint.connectedWaypoints;
            NPCWaypoint next = connected[Random.Range(0, connected.Length)];
            _currentWaypoint = next;
            _target = next.transform.position;
        }
    }

    private void TryPickLocalWander()
    {
        // пробуем несколько раз найти точку над землёй
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 rnd = Random.insideUnitCircle * _currentWaypoint.WanderRadius;
            Vector3 candidate = _currentWaypoint.transform.position + new Vector3(rnd.x, 0f, rnd.y);

            if (IsOverGround(candidate))
            {
                _target = candidate;
                return;
            }
        }

        _target = _currentWaypoint.transform.position;
    }

    // ─── Антипропасть ────────────────────────────────────────────────────────

    private bool IsOverGround(Vector3 point)
    {
        Vector3 rayOrigin = point + Vector3.up * 0.5f;
        return Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance, groundMask);
    }

    // ─── Застревание ─────────────────────────────────────────────────────────

    private void CheckStuck()
    {
        if (HorizontalDistance(transform.position, _lastPosition) > 0.05f)
        {
            _lastPosition = transform.position;
            _stuckTimer = 0f;
            return;
        }

        _stuckTimer += Time.deltaTime;
        if (_stuckTimer >= stuckTimeout)
        {
            _stuckTimer = 0f;
            // сброс к ближайшему waypoint и смена цели
            _currentWaypoint = GetNearestWaypoint();
            PickNextTarget();
        }
    }

    // ─── Ожидание ────────────────────────────────────────────────────────────

    private void StartWaiting()
    {
        _waiting = true;
        _waitTimer = Random.Range(waitTimeMin, waitTimeMax);
    }

    // ─── Хелперы ─────────────────────────────────────────────────────────────

    private void SnapToNearestWaypoint()
    {
        _currentWaypoint = GetNearestWaypoint();
    }

    private NPCWaypoint GetNearestWaypoint()
    {
        NPCWaypoint best = homeWaypoints[0];
        float bestDist = float.MaxValue;

        foreach (var wp in homeWaypoints)
        {
            float d = HorizontalDistance(transform.position, wp.transform.position);
            if (d < bestDist) { bestDist = d; best = wp; }
        }

        return best;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_target, 0.15f);
        Gizmos.DrawLine(transform.position, _target);
    }
#endif
}