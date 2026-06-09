using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SongSelectScrollbarHandle : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public SongSelectController controller;

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;
    private Camera eventCamera;
    private float pointerStartLocalY;
    private float handleStartY;
    private bool isDragging;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (controller == null)
        {
            return;
        }

        rectTransform = rectTransform != null ? rectTransform : GetComponent<RectTransform>();
        parentRectTransform = rectTransform != null ? rectTransform.parent as RectTransform : null;
        if (rectTransform == null || parentRectTransform == null)
        {
            return;
        }

        Image image = GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }

        eventCamera = eventData.pressEventCamera;
        if (!TryGetPointerLocalY(eventData.position, out pointerStartLocalY))
        {
            return;
        }

        handleStartY = rectTransform.anchoredPosition.y;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || controller == null)
        {
            return;
        }

        if (!TryGetPointerLocalY(eventData.position, out float currentLocalY))
        {
            return;
        }

        float deltaY = currentLocalY - pointerStartLocalY;
        float handleY = controller.ClampCustomHandleY(handleStartY + deltaY);
        controller.SetScrollFromCustomHandleY(handleY);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    private bool TryGetPointerLocalY(Vector2 screenPosition, out float localY)
    {
        localY = 0f;
        if (parentRectTransform == null)
        {
            return false;
        }

        bool hasPoint = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRectTransform,
            screenPosition,
            eventCamera,
            out Vector2 localPoint);

        if (!hasPoint)
        {
            return false;
        }

        localY = localPoint.y;
        return true;
    }
}
