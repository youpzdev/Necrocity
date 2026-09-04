using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class PlacementDragInput
{
    private const int MousePointerId = -1;
    private const int NoPointer = int.MinValue;

    private readonly Camera worldCamera;
    private readonly LayerMask groundLayers;
    private readonly float maxRayDistance;

    private int pointerId = NoPointer;
    private Vector3 grabOffset;

    public PlacementDragInput(Camera worldCamera, LayerMask groundLayers, float maxRayDistance)
    {
        this.worldCamera = worldCamera;
        this.groundLayers = groundLayers;
        this.maxRayDistance = maxRayDistance;
    }

    public void Reset()
    {
        pointerId = NoPointer;
        grabOffset = Vector3.zero;
    }

    public void Tick(Transform target)
    {
        if (target == null || worldCamera == null) return;

        if (Touchscreen.current != null) ReadTouches(target, Touchscreen.current);
        else if (Mouse.current != null) ReadMouse(target, Mouse.current);
    }

    private void ReadTouches(Transform target, Touchscreen screen)
    {
        var touches = screen.touches;

        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];
            TouchPhase phase = touch.phase.ReadValue();
            if (phase == TouchPhase.None) continue;

            int id = touch.touchId.ReadValue();
            Vector2 position = touch.position.ReadValue();

            switch (phase)
            {
                case TouchPhase.Began:
                    BeginDrag(target, id, position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    MoveDrag(target, id, position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    EndDrag(id);
                    break;
            }
        }
    }

    private void ReadMouse(Transform target, Mouse mouse)
    {
        Vector2 position = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame) BeginDrag(target, MousePointerId, position);
        else if (mouse.leftButton.wasReleasedThisFrame) EndDrag(MousePointerId);
        else if (mouse.leftButton.isPressed) MoveDrag(target, MousePointerId, position);
    }

    private void BeginDrag(Transform target, int id, Vector2 position)
    {
        if (pointerId != NoPointer) return;
        if (IsOverUI(id)) return;
        if (!RaycastGround(position, out Vector3 point)) return;

        pointerId = id;
        grabOffset = target.position - point;
        grabOffset.y = 0f;
    }

    private void MoveDrag(Transform target, int id, Vector2 position)
    {
        if (id != pointerId) return;
        if (!RaycastGround(position, out Vector3 point)) return;

        target.position = point + grabOffset;
    }

    private void EndDrag(int id)
    {
        if (id != pointerId) return;
        pointerId = NoPointer;
    }

    private bool RaycastGround(Vector2 screenPosition, out Vector3 point)
    {
        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    private static bool IsOverUI(int pointerId)
    {
        EventSystem events = EventSystem.current;
        return events != null && events.IsPointerOverGameObject(pointerId);
    }
}
