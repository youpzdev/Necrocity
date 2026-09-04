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
    [SerializeField] private float groundProbeHeight = 0.5f;
    [SerializeField] private float edgeProbeDistance = 0.5f;
    [SerializeField] private float fallRecoveryDepth = 5f;
    [SerializeField] private float spawnGroundSearchDepth = 50f;
    [SerializeField] private LayerMask groundMask;

    private CharacterController _cc;
    private int _movingHash;
    private bool _canAnimate;
    private bool _moving;
    private Vector3 _target;
    private float _verticalVelocity;
    private float _waitTimer;
    private bool _waiting;

    private NPCWaypoint[] _waypoints;
    private NPCWaypoint _currentWaypoint;
    private Vector3 _lastPosition;
    private float _stuckTimer;
    private int _groundLayers;
    private float _edgeProbe;
    private bool _ready;
    private bool _initialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        _cc = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (!string.IsNullOrEmpty(movingParameter)) _movingHash = Animator.StringToHash(movingParameter);

        _canAnimate = HasMovingParameter();
        _groundLayers = groundMask.value != 0 ? groundMask.value : Physics.DefaultRaycastLayers;
        _edgeProbe = Mathf.Max(edgeProbeDistance, _cc.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) + 0.1f);
    }

    private void Start()
    {
        if (_ready) return;

        SetWaypoints(homeWaypoints);
        if (!_ready) Debug.LogWarning($"[NPCBrain] {name}: нет waypoints!", this);
    }

    public void SetWaypoints(NPCWaypoint[] waypoints)
    {
        EnsureInitialized();

        _waypoints = Compact(waypoints);
        _ready = _waypoints.Length > 0;

        ApplyMoving(false);

        if (!_ready)
        {
            _currentWaypoint = null;
            return;
        }

        _waiting = false;
        _stuckTimer = 0f;
        _verticalVelocity = 0f;
        _lastPosition = transform.position;

        _currentWaypoint = GetNearestWaypoint();
        if (!IsOverGround(transform.position, spawnGroundSearchDepth)) Teleport(_currentWaypoint.transform.position);

        PickNextTarget();
    }

    private void Update()
    {
        ApplyGravity();

        if (!_ready)
        {
            _cc.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
            return;
        }

        if (RecoverFromFall()) return;

        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;
            SetMoving(false);
            if (_waitTimer <= 0f) PickNextTarget();
            return;
        }

        if (HorizontalDistance(transform.position, _target) <= Mathf.Max(0.05f, arrivalDistance))
        {
            SetMoving(false);
            StartWaiting();
            return;
        }

        bool moved = MoveToTarget();
        SetMoving(moved);

        if (moved) CheckStuck();
    }

    // ─── Движение ────────────────────────────────────────────────────────────

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

    private void SetMoving(bool value)
    {
        if (_moving == value) return;
        ApplyMoving(value);
    }

    private void ApplyMoving(bool value)
    {
        _moving = value;
        if (!_canAnimate) return;
        animator.SetBool(_movingHash, value);
    }

    private bool MoveToTarget()
    {
        Vector3 dir = (_target - transform.position);
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return false;

        dir.Normalize();

        if (!IsOverGround(transform.position + dir * _edgeProbe))
        {
            BlockAtEdge();
            return false;
        }

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);

        Vector3 move = dir * moveSpeed;
        move.y = _verticalVelocity;
        _cc.Move(move * Time.deltaTime);
        return true;
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

        if (_currentWaypoint == null) _currentWaypoint = GetNearestWaypoint();
        if (_currentWaypoint == null)
        {
            _ready = false;
            return;
        }

        NPCWaypoint next = Random.value < chanceToWanderLocally ? null : _currentWaypoint.GetRandomConnected();

        if (next == null)
        {
            TryPickLocalWander();
            return;
        }

        // переход к соседнему waypoint (другой остров или просто связанная точка)
        _currentWaypoint = next;
        _target = next.transform.position;
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
        return IsOverGround(point, groundCheckDistance);
    }

    private bool IsOverGround(Vector3 point, float depth)
    {
        float height = Mathf.Max(0.1f, groundProbeHeight);
        Vector3 rayOrigin = point + Vector3.up * height;
        float distance = height + Mathf.Max(0.1f, depth);

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, distance, _groundLayers, QueryTriggerInteraction.Ignore))
            return false;

        return hit.collider != _cc;
    }

    private void BlockAtEdge()
    {
        _currentWaypoint = GetNearestWaypoint();
        StartWaiting();
    }

    private bool RecoverFromFall()
    {
        if (_currentWaypoint == null) return false;

        float limit = _currentWaypoint.transform.position.y - Mathf.Max(1f, fallRecoveryDepth);
        if (transform.position.y > limit) return false;

        _currentWaypoint = GetNearestWaypoint();
        Teleport(_currentWaypoint.transform.position);
        PickNextTarget();
        return true;
    }

    private void Teleport(Vector3 position)
    {
        bool wasEnabled = _cc.enabled;
        _cc.enabled = false;
        transform.position = position;
        _cc.enabled = wasEnabled;

        _verticalVelocity = 0f;
        _lastPosition = position;
        _stuckTimer = 0f;
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

    private static NPCWaypoint[] Compact(NPCWaypoint[] source)
    {
        if (source == null) return System.Array.Empty<NPCWaypoint>();

        int count = 0;
        foreach (var wp in source)
        {
            if (wp != null) count++;
        }

        if (count == 0) return System.Array.Empty<NPCWaypoint>();

        var result = new NPCWaypoint[count];
        int index = 0;
        foreach (var wp in source)
        {
            if (wp != null) result[index++] = wp;
        }

        return result;
    }

    private NPCWaypoint GetNearestWaypoint()
    {
        NPCWaypoint best = null;
        float bestDist = float.MaxValue;

        foreach (var wp in _waypoints)
        {
            if (wp == null) continue;

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
        if (!_ready) return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_target, 0.15f);
        Gizmos.DrawLine(transform.position, _target);
    }
#endif
}
