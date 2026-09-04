using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

[DefaultExecutionOrder(150)]
public class CameraController : MonoBehaviour
{
    private const int MousePointerId = -1;

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

    [Header("Mouse")]
    [SerializeField] private float mousePanSensitivity = 1f;
    [SerializeField] private float mouseRotateSensitivity = 0.2f;
    [SerializeField] private float mouseZoomStep = 3f;

    [Header("Touch")]
    [SerializeField] private float touchPanSensitivity = 1f;
    [SerializeField] private bool useTouchRotation = true;
    [SerializeField] private float touchRotateSensitivity = 1f;
    [SerializeField] private float touchZoomSensitivity = 70f;

    [Header("Gestures")]
    [SerializeField] private float panStartThresholdPixels = 6f;
    [SerializeField] private float pinchStartThresholdPixels = 12f;
    [SerializeField] private float twistStartThresholdDegrees = 4f;
    [SerializeField] private bool ignoreInputOverUI = true;

    [Header("Smoothing")]
    [SerializeField] private float fovSmooth = 10f;
    [SerializeField] private float positionSmooth = 0.08f;

    [Header("Inertia")]
    [SerializeField] private bool usePanInertia = true;
    [SerializeField] private float panInertiaDamping = 6f;
    [SerializeField] private float panInertiaStopSpeed = 0.05f;

    [Header("Focus")]
    [SerializeField] private float focusMoveDuration = 0.6f;
    [SerializeField] private float focusReturnDuration = 0.6f;
    [SerializeField] private float focusDistance = 0f;
    [SerializeField] private float focusHeightOffset = 0f;
    [SerializeField] private Ease focusEase = Ease.InOutSine;

    [Header("Limits")]
    [SerializeField] private bool useManualLimits = false;
    [SerializeField] private Vector2 xLimits = new(-10f, 10f);
    [SerializeField] private Vector2 zLimits = new(-10f, 10f);

    private float _distance;
    private Vector3 _pivotOffset;
    private Vector3 _velocity;
    private Vector3 _panInertia;
    private bool _pannedThisFrame;

    private bool _singleActive;
    private int _singleId;
    private Vector2 _singlePressPosition;
    private Vector2 _singlePrevPosition;
    private bool _singlePanStarted;
    private bool _singleBlocked;

    private bool _twoFingerActive;
    private int _twoFingerIdA;
    private int _twoFingerIdB;
    private Vector2 _prevTouchA;
    private Vector2 _prevTouchB;
    private float _pinchStartGap;
    private float _twistStartAngle;
    private bool _pinchStarted;
    private bool _twistStarted;
    private bool _twoFingerBlocked;

    private bool _mouseTracked;
    private Vector2 _mousePrevPosition;
    private Vector2 _mousePressPosition;
    private bool _mousePanActive;
    private bool _mousePanStarted;
    private bool _mouseRotateActive;

    private Transform _focusTarget;
    private Sequence _focusSequence;
    private bool _focusActive;
    private bool _releasing;
    private Vector3 _returnPosition;
    private Quaternion _returnRotation;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        cam.fieldOfView = fov;

        _distance = (transform.position - baseTarget.position).magnitude;
    }

    void OnDisable()
    {
        ResetGestures();
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

        ResetGestures();

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
        ResetGestures();
    }

    private bool InputLocked() => _focusActive || IsBlocked();

    void Update()
    {
        if (_focusActive && !_releasing && _focusTarget == null) ReleaseFocus();

        _pannedThisFrame = false;

        if (InputLocked()) ResetGestures();
        else ReadInput();

        ApplyInertia();
        ClampValues();
    }

    void LateUpdate()
    {
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, Time.deltaTime * fovSmooth);

        if (_focusActive) return;

        Vector3 center = baseTarget.position + _pivotOffset;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = center + rot * new Vector3(0f, 0f, -_distance);

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, positionSmooth);
        transform.rotation = rot;
    }

    private void ReadInput()
    {
        if (ReadTouches() > 0)
        {
            ResetMouseGestures();
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null) ReadMouse(mouse);
    }

    private int ReadTouches()
    {
        Touchscreen screen = Touchscreen.current;
        if (screen == null)
        {
            ResetTouchGestures();
            return 0;
        }

        int count = 0;
        int idA = 0;
        int idB = 0;
        Vector2 posA = Vector2.zero;
        Vector2 posB = Vector2.zero;

        var touches = screen.touches;
        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];
            if (!touch.press.isPressed) continue;

            TouchPhase phase = touch.phase.ReadValue();
            if (phase != TouchPhase.Began && phase != TouchPhase.Moved && phase != TouchPhase.Stationary) continue;

            if (count == 0)
            {
                idA = touch.touchId.ReadValue();
                posA = touch.position.ReadValue();
            }
            else if (count == 1)
            {
                idB = touch.touchId.ReadValue();
                posB = touch.position.ReadValue();
            }

            count++;
        }

        if (count >= 2) HandleTwoFingers(idA, posA, idB, posB);
        else if (count == 1) HandleOneFinger(idA, posA);
        else ResetTouchGestures();

        return count;
    }

    private void HandleOneFinger(int id, Vector2 position)
    {
        _twoFingerActive = false;

        if (!_singleActive || _singleId != id)
        {
            _singleActive = true;
            _singleId = id;
            _singlePressPosition = position;
            _singlePrevPosition = position;
            _singlePanStarted = false;
            _singleBlocked = ignoreInputOverUI && IsPointerOverUI(id);
            _panInertia = Vector3.zero;
            return;
        }

        Vector2 delta = position - _singlePrevPosition;
        _singlePrevPosition = position;

        if (_singleBlocked) return;

        if (!_singlePanStarted)
        {
            if ((position - _singlePressPosition).magnitude < panStartThresholdPixels) return;
            _singlePanStarted = true;
            return;
        }

        Pan(delta * touchPanSensitivity);
    }

    private void HandleTwoFingers(int idA, Vector2 posA, int idB, Vector2 posB)
    {
        _singleActive = false;
        _singlePanStarted = false;

        if (!_twoFingerActive || _twoFingerIdA != idA || _twoFingerIdB != idB)
        {
            _twoFingerActive = true;
            _twoFingerIdA = idA;
            _twoFingerIdB = idB;
            _prevTouchA = posA;
            _prevTouchB = posB;
            _pinchStartGap = (posB - posA).magnitude;
            _twistStartAngle = ScreenAngle(posB - posA);
            _pinchStarted = false;
            _twistStarted = false;
            _twoFingerBlocked = ignoreInputOverUI && (IsPointerOverUI(idA) || IsPointerOverUI(idB));
            _panInertia = Vector3.zero;
            return;
        }

        Vector2 prevVector = _prevTouchB - _prevTouchA;
        Vector2 currentVector = posB - posA;
        _prevTouchA = posA;
        _prevTouchB = posB;

        if (_twoFingerBlocked) return;

        float prevGap = prevVector.magnitude;
        float gap = currentVector.magnitude;
        if (prevGap < 1f || gap < 1f) return;

        if (!_pinchStarted && Mathf.Abs(gap - _pinchStartGap) >= pinchStartThresholdPixels) _pinchStarted = true;
        if (_pinchStarted) fov -= (gap - prevGap) / ViewportHeight() * touchZoomSensitivity;

        if (!useTouchRotation) return;

        float angle = ScreenAngle(currentVector);
        if (!_twistStarted && Mathf.Abs(Mathf.DeltaAngle(_twistStartAngle, angle)) >= twistStartThresholdDegrees) _twistStarted = true;
        if (_twistStarted) Rotate(Mathf.DeltaAngle(ScreenAngle(prevVector), angle) * touchRotateSensitivity, 0f);
    }

    private void ReadMouse(Mouse mouse)
    {
        Vector2 position = mouse.position.ReadValue();
        Vector2 delta = _mouseTracked ? position - _mousePrevPosition : Vector2.zero;
        _mousePrevPosition = position;
        _mouseTracked = true;

        bool overUI = ignoreInputOverUI && IsPointerOverUI(MousePointerId);

        if (mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame) _mouseRotateActive = !overUI;
        if (!mouse.rightButton.isPressed && !mouse.middleButton.isPressed) _mouseRotateActive = false;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            _mousePanActive = !overUI;
            _mousePanStarted = false;
            _mousePressPosition = position;
            _panInertia = Vector3.zero;
        }
        if (!mouse.leftButton.isPressed) _mousePanActive = false;

        if (_mouseRotateActive)
        {
            Rotate(delta.x * mouseRotateSensitivity, delta.y * mouseRotateSensitivity);
        }
        else if (_mousePanActive)
        {
            if (!_mousePanStarted)
            {
                if ((position - _mousePressPosition).magnitude >= panStartThresholdPixels) _mousePanStarted = true;
            }
            else
            {
                Pan(delta * mousePanSensitivity);
            }
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (!overUI && Mathf.Abs(scroll) > 0.01f) fov -= Mathf.Sign(scroll) * mouseZoomStep;
    }

    private void Rotate(float yawDelta, float pitchDelta)
    {
        yaw += yawDelta;
        pitch -= pitchDelta;
    }

    private void Pan(Vector2 pixels)
    {
        _pannedThisFrame = true;

        Vector3 move = PixelsToWorld(pixels);
        _pivotOffset -= move;
        ClampToBounds();

        if (!usePanInertia)
        {
            _panInertia = Vector3.zero;
            return;
        }

        Vector3 speed = -move / Mathf.Max(Time.deltaTime, 0.0001f);
        _panInertia = Vector3.Lerp(_panInertia, speed, 0.5f);
    }

    private void ApplyInertia()
    {
        if (!usePanInertia)
        {
            _panInertia = Vector3.zero;
            return;
        }

        if (_pannedThisFrame) return;

        if (_panInertia.sqrMagnitude <= panInertiaStopSpeed * panInertiaStopSpeed)
        {
            _panInertia = Vector3.zero;
            return;
        }

        _pivotOffset += _panInertia * Time.deltaTime;
        ClampToBounds();
        _panInertia = Vector3.Lerp(_panInertia, Vector3.zero, Mathf.Clamp01(panInertiaDamping * Time.deltaTime));
    }

    private Vector3 PixelsToWorld(Vector2 pixels)
    {
        float worldPerPixel = 2f * _distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / ViewportHeight();
        float tilt = Mathf.Max(0.2f, Mathf.Sin(pitch * Mathf.Deg2Rad));

        Quaternion flatRotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 right = flatRotation * Vector3.right;
        Vector3 forward = flatRotation * Vector3.forward;

        return (right * pixels.x + forward * (pixels.y / tilt)) * worldPerPixel;
    }

    private float ViewportHeight() => Mathf.Max(1f, cam.pixelHeight);

    private void ResetGestures()
    {
        ResetTouchGestures();
        ResetMouseGestures();
        _panInertia = Vector3.zero;
    }

    private void ResetTouchGestures()
    {
        _singleActive = false;
        _singlePanStarted = false;
        _singleBlocked = false;
        _twoFingerActive = false;
        _pinchStarted = false;
        _twistStarted = false;
        _twoFingerBlocked = false;
    }

    private void ResetMouseGestures()
    {
        _mouseTracked = false;
        _mousePanActive = false;
        _mousePanStarted = false;
        _mouseRotateActive = false;
    }

    private void ClampValues()
    {
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        fov = Mathf.Clamp(fov, minFov, maxFov);
        if (useYawLimits) yaw = Mathf.Clamp(yaw, minYaw, maxYaw);
    }

    private void ClampToBounds()
    {
        _pivotOffset.y = 0f;

        if (!useManualLimits) return;

        Vector3 pivot = baseTarget.position + _pivotOffset;
        float clampedX = Mathf.Clamp(pivot.x, xLimits.x, xLimits.y);
        float clampedZ = Mathf.Clamp(pivot.z, zLimits.x, zLimits.y);

        if (!Mathf.Approximately(clampedX, pivot.x)) _panInertia.x = 0f;
        if (!Mathf.Approximately(clampedZ, pivot.z)) _panInertia.z = 0f;

        _pivotOffset = new Vector3(clampedX - baseTarget.position.x, 0f, clampedZ - baseTarget.position.z);
    }

    private static bool IsPointerOverUI(int pointerId)
    {
        EventSystem events = EventSystem.current;
        return events != null && events.IsPointerOverGameObject(pointerId);
    }

    private static bool IsBlocked() => UIManager.Instance != null && UIManager.Instance.AreModalWindowOpened();

    private static float ScreenAngle(Vector2 vector) => Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
}
