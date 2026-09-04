using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Focus")]
    [SerializeField] private float focusMoveDuration = 0.6f;
    [SerializeField] private float focusReturnDuration = 0.6f;
    [SerializeField] private float focusDistance = 0f;
    [SerializeField] private float focusHeightOffset = 0f;
    [SerializeField] private Ease focusEase = Ease.InOutSine;

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

    private Transform _focusTarget;
    private Sequence _focusSequence;
    private bool _focusActive;
    private bool _releasing;
    private Vector3 _returnPosition;
    private Quaternion _returnRotation;

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
        InputManager.Instance.OnRotateChanged += HandleRotateChanged;
        InputManager.Instance.OnMiddleMouseChanged += HandleMiddleMouseChanged;
    }

    void OnDisable()
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.OnLook -= HandleLook;
        InputManager.Instance.OnPan -= HandlePan;
        InputManager.Instance.OnScroll -= HandleScroll;
        InputManager.Instance.OnPinch -= HandlePinch;
        InputManager.Instance.OnRotateChanged -= HandleRotateChanged;
        InputManager.Instance.OnMiddleMouseChanged -= HandleMiddleMouseChanged;
    }

    void OnDestroy()
    {
        DOTween.Kill(this);
    }

    public void FocusOn(Transform target, float duration)
    {
        if (target == null) return;

        KillFocusSequence();

        if (!_focusActive)
        {
            _returnPosition = transform.position;
            _returnRotation = transform.rotation;
            _focusActive = true;
        }

        _focusTarget = target;
        _releasing = false;

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        float distance = focusDistance > 0f ? focusDistance : _distance;
        Vector3 destination = target.position + Vector3.up * focusHeightOffset + rot * new Vector3(0f, 0f, -distance);

        _focusSequence = DOTween.Sequence().SetId(this).SetUpdate(true);
        _focusSequence.Join(transform.DOMove(destination, focusMoveDuration).SetEase(focusEase));
        _focusSequence.Join(transform.DORotateQuaternion(rot, focusMoveDuration).SetEase(focusEase));

        if (duration > 0f)
        {
            _focusSequence.AppendInterval(duration);
            _focusSequence.OnComplete(OnHoldFinished);
        }
    }

    public void ReleaseFocus()
    {
        if (!_focusActive) return;

        KillFocusSequence();
        _focusTarget = null;
        _releasing = true;

        _focusSequence = DOTween.Sequence().SetId(this).SetUpdate(true);
        _focusSequence.Join(transform.DOMove(_returnPosition, focusReturnDuration).SetEase(focusEase));
        _focusSequence.Join(transform.DORotateQuaternion(_returnRotation, focusReturnDuration).SetEase(focusEase));
        _focusSequence.OnComplete(FinishRelease);
    }

    private void OnHoldFinished()
    {
        _focusSequence = null;
        ReleaseFocus();
    }

    private void KillFocusSequence()
    {
        if (_focusSequence == null) return;
        _focusSequence.Kill();
        _focusSequence = null;
    }

    private void FinishRelease()
    {
        _focusSequence = null;
        _velocity = Vector3.zero;
        _releasing = false;
        _focusActive = false;
    }

    private bool InputLocked() => _focusActive || IsBlocked();

    private void HandleRotateChanged(bool held) => _rotateHeld = held;

    private void HandleMiddleMouseChanged(bool held) => _middleHeld = held;

    private void HandleLook(Vector2 delta)
    {
        if (InputLocked()) return;

        if (_middleHeld)
            _panDelta += delta * panSensitivity;
        else if (_rotateHeld || IsSingleFingerTouching())
            _rotateDelta += delta * rotateSensitivity;
    }

    // Pan fires only on touch second finger
    private void HandlePan(Vector2 delta)
    {
        if (InputLocked() || _middleHeld) return;
        _panDelta += delta * panSensitivity;
    }

    private void HandleScroll(float scroll)
    {
        if (InputLocked()) return;
        fov -= scroll * scrollSensitivity;
    }

    private void HandlePinch(float currDist)
    {
        if (InputLocked()) return;

        if (_pinchActive && currDist > 0f)
            fov += (_prevPinchDist - currDist) * pinchSensitivity;

        _prevPinchDist = currDist;
        _pinchActive = currDist > 0f;
    }

    void Update()
    {
        if (_focusActive && !_releasing && _focusTarget == null) ReleaseFocus();

        if (_pinchActive && PressedTouchCount() < 2)
        {
            _pinchActive = false;
            _prevPinchDist = 0f;
        }

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

        if (_focusActive) return;

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

    private static bool IsBlocked() => UIManager.Instance != null && UIManager.Instance.AreModalWindowOpened();

    private static int PressedTouchCount()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return 0;

        int count = 0;
        foreach (var touch in touchscreen.touches)
        {
            if (touch.press.isPressed) count++;
        }
        return count;
    }

    private static bool IsSingleFingerTouching() => PressedTouchCount() == 1;
}
