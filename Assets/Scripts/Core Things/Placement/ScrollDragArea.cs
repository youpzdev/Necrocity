using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollDragArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    [SerializeField] private ScrollRect scrollRect;

    private void Awake()
    {
        if (scrollRect == null) scrollRect = GetComponentInParent<ScrollRect>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (scrollRect != null) scrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (scrollRect != null) scrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (scrollRect != null) scrollRect.OnEndDrag(eventData);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (scrollRect != null) scrollRect.OnScroll(eventData);
    }
}
