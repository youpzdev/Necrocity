using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

[DefaultExecutionOrder(200)]
public class WorldClickRouter : MonoBehaviour
{
    private const int MousePointerId = -1;
    private const int NoPointer = int.MinValue;

    public static WorldClickRouter Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera worldCamera;

    [Header("Raycast")]
    [SerializeField] private LayerMask clickableLayers = ~0;
    [SerializeField] private float maxRayDistance = 500f;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Click")]
    [SerializeField] private float dragThresholdPixels = 20f;

    private readonly HashSet<GameObject> allowedTargets = new HashSet<GameObject>();
    private bool filterActive;

    private int trackedPointerId = NoPointer;
    private Vector2 pressPosition;
    private bool pressStartedOverUI;
    private bool pressCanceled;

    public bool FilterActive => filterActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetClickFilter(params GameObject[] targets)
    {
        allowedTargets.Clear();

        if (targets != null)
        {
            foreach (GameObject target in targets)
            {
                if (target != null) allowedTargets.Add(target);
            }
        }

        filterActive = true;
        ResetPress();
    }

    public void SetClickFilter(IEnumerable<GameObject> targets)
    {
        allowedTargets.Clear();

        if (targets != null)
        {
            foreach (GameObject target in targets)
            {
                if (target != null) allowedTargets.Add(target);
            }
        }

        filterActive = true;
        ResetPress();
    }

    public void ClearClickFilter()
    {
        allowedTargets.Clear();
        filterActive = false;
    }

    private void Update()
    {
        if (Touchscreen.current != null) ReadTouches(Touchscreen.current);
        else if (Mouse.current != null) ReadMouse(Mouse.current);
    }

    private void ReadTouches(Touchscreen screen)
    {
        var touches = screen.touches;

        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];
            TouchPhase phase = touch.phase.ReadValue();
            if (phase == TouchPhase.None) continue;

            int pointerId = touch.touchId.ReadValue();

            switch (phase)
            {
                case TouchPhase.Began:
                    BeginPress(pointerId, touch.position.ReadValue());
                    break;
                case TouchPhase.Ended:
                    EndPress(pointerId, touch.position.ReadValue());
                    break;
                case TouchPhase.Canceled:
                    if (pointerId == trackedPointerId) ResetPress();
                    break;
            }
        }
    }

    private void ReadMouse(Mouse mouse)
    {
        if (mouse.leftButton.wasPressedThisFrame) BeginPress(MousePointerId, mouse.position.ReadValue());
        if (mouse.leftButton.wasReleasedThisFrame) EndPress(MousePointerId, mouse.position.ReadValue());
    }

    private void BeginPress(int pointerId, Vector2 position)
    {
        if (trackedPointerId != NoPointer && trackedPointerId != pointerId)
        {
            pressCanceled = true;
            return;
        }

        trackedPointerId = pointerId;
        pressPosition = position;
        pressStartedOverUI = IsOverUI(pointerId);
        pressCanceled = false;
    }

    private void EndPress(int pointerId, Vector2 position)
    {
        if (pointerId != trackedPointerId) return;

        bool canceled = pressCanceled;
        bool startedOverUI = pressStartedOverUI;
        Vector2 start = pressPosition;

        ResetPress();

        if (canceled || startedOverUI) return;
        if (IsOverUI(pointerId)) return;
        if ((position - start).sqrMagnitude > dragThresholdPixels * dragThresholdPixels) return;

        SendClick(position);
    }

    private void ResetPress()
    {
        trackedPointerId = NoPointer;
        pressStartedOverUI = false;
        pressCanceled = false;
    }

    private void SendClick(Vector2 position)
    {
        if (worldCamera == null) return;

        Ray ray = worldCamera.ScreenPointToRay(position);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, clickableLayers, triggerInteraction)) return;

        BuildingClickHandler handler = hit.collider.GetComponentInParent<BuildingClickHandler>();
        if (handler == null) return;
        if (!IsAllowed(handler.transform)) return;

        handler.HandleClick();
    }

    private bool IsAllowed(Transform hit)
    {
        if (!filterActive) return true;

        for (Transform current = hit; current != null; current = current.parent)
        {
            if (allowedTargets.Contains(current.gameObject)) return true;
        }

        return false;
    }

    private static bool IsOverUI(int pointerId)
    {
        EventSystem events = EventSystem.current;
        return events != null && events.IsPointerOverGameObject(pointerId);
    }
}
