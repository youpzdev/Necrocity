using UnityEngine;

[DefaultExecutionOrder(150)]
public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform baseTarget;

    [Header("FOV")]
    [SerializeField] private float fov = 45f;
    [SerializeField] private float minFov = 25f;
    [SerializeField] private float maxFov = 65f;

    [Header("Rotation")]
    [SerializeField] private float yaw;
    [SerializeField] private float pitch = 35f;
    [SerializeField] private float minPitch = 20f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private bool useYawLimits = false;
    [SerializeField] private float minYaw = -5f;
    [SerializeField] private float maxYaw = 35f;

    [Header("Pan")]
    [SerializeField] private float minVerticalOffset = -2f;
    [SerializeField] private float maxVerticalOffset = 6f;

    [Header("Sensitivity")]
    [SerializeField] private float rotateSensitivity = 0.035f;
    [SerializeField] private float panSensitivity = 0.04f;
    [SerializeField] private float scrollSensitivity = 20f;
    [SerializeField] private float pinchSensitivity = 0.15f;
    [SerializeField] private float fovSmooth = 10f;
    [SerializeField] private float positionSmooth = 0.08f;

    [Header("Limits")]
    [SerializeField] private bool useManualLimits = false;
    [SerializeField] private Vector2 xLimits = new(-10f, 10f);
    [SerializeField] private Vector2 yLimits = new(0f, 5f);


    private float _distance;
    private Vector3 _centerOffset;
    private float _verticalOffset;
    private Vector3 _velocity;


    private Vector2 _rotateDelta;
    private Vector2 _panDelta;


    private Transform _camTransform;


    private bool _rotateHeld;
    private bool _middleHeld;

    private float _prevPinchDist;
    private bool _pinchActive;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        _camTransform = cam.transform;
        cam.fieldOfView = fov;

        _distance = (transform.position - baseTarget.position).magnitude;
    }

    void OnEnable()
    {
        InputManager.Instance.OnLook += HandleLook;
        InputManager.Instance.OnPan += HandlePan;
        InputManager.Instance.OnScroll += HandleScroll;
        InputManager.Instance.OnPinch += HandlePinch;
        InputManager.Instance.OnRotateChanged += v => _rotateHeld = v;
        InputManager.Instance.OnMiddleMouseChanged += v => _middleHeld = v;
    }

    void OnDisable()
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.OnLook -= HandleLook;
        InputManager.Instance.OnPan -= HandlePan;
        InputManager.Instance.OnScroll -= HandleScroll;
        InputManager.Instance.OnPinch -= HandlePinch;
        InputManager.Instance.OnRotateChanged -= v => _rotateHeld = v;
        InputManager.Instance.OnMiddleMouseChanged -= v => _middleHeld = v;
    }

    private void HandleLook(Vector2 delta)
    {
        if (IsBlocked()) return;

        if (_middleHeld)
            _panDelta += delta * panSensitivity;
        else if (_rotateHeld || IsTouchSingleFinger())
            _rotateDelta += delta * rotateSensitivity;
    }

    // Pan fires only on touch second finger
    private void HandlePan(Vector2 delta)
    {
        if (IsBlocked() || _middleHeld) return;
        _panDelta += delta * panSensitivity;
    }

    private void HandleScroll(float scroll)
    {
        if (IsBlocked()) return;
        fov -= scroll * scrollSensitivity;
    }

    private void HandlePinch(float currDist)
    {
        if (IsBlocked()) return;

        if (_pinchActive && currDist > 0f)
            fov += (_prevPinchDist - currDist) * pinchSensitivity;

        _prevPinchDist = currDist;
        _pinchActive = currDist > 0f;
    }

    void Update()
    {
        if (_rotateDelta != Vector2.zero)
        {
            yaw += _rotateDelta.x;
            pitch -= _rotateDelta.y;
            _rotateDelta = Vector2.zero;
        }

        if (_panDelta != Vector2.zero)
        {
            ApplyPan(_panDelta);
            _panDelta = Vector2.zero;
        }

        ClampValues();
    }

    void LateUpdate()
    {
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, Time.deltaTime * fovSmooth);

        Vector3 center = baseTarget.position + _centerOffset + Vector3.up * _verticalOffset;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = center + rot * new Vector3(0f, 0f, -_distance);

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, positionSmooth);
        transform.rotation = rot;
    }

    private void ApplyPan(Vector2 delta)
    {
        Vector3 right = _camTransform.right;
        right.y = 0f;
        right.Normalize();

        _centerOffset += right * (delta.x * _distance);
        _verticalOffset = Mathf.Clamp(_verticalOffset + delta.y * _distance, minVerticalOffset, maxVerticalOffset);

        ClampToBounds();
    }

    private void ClampValues()
    {
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        fov = Mathf.Clamp(fov, minFov, maxFov);
        if (useYawLimits) yaw = Mathf.Clamp(yaw, minYaw, maxYaw);
    }

    private void ClampToBounds()
    {
        if (!useManualLimits) return;

        Vector3 pos = baseTarget.position + _centerOffset;
        pos.y = baseTarget.position.y + _verticalOffset;

        pos.x = Mathf.Clamp(pos.x, xLimits.x, xLimits.y);
        pos.y = Mathf.Clamp(pos.y, yLimits.x, yLimits.y);
        pos.z = baseTarget.position.z;

        _centerOffset = new Vector3(pos.x - baseTarget.position.x, 0f, 0f);
        _verticalOffset = pos.y - baseTarget.position.y;
    }

    private static bool IsBlocked() => UIManager.Instance.AreModalWindowOpened();
    private static bool IsTouchSingleFinger() => UnityEngine.InputSystem.Touchscreen.current != null;
}
