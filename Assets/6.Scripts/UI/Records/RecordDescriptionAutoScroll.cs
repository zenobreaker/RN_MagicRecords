using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Scrolls overflowing record details while leaving manual reading uninterrupted.</summary>
public class RecordDescriptionAutoScroll : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IEndDragHandler, IScrollHandler
{
    [SerializeField] private ScrollRect scroll;
    [SerializeField, Min(0f)] private float pixelsPerSecond = 18f;
    [SerializeField, Min(0f)] private float pauseSeconds = 3f;

    private float waitRemaining;
    private bool pointerInside;
    private bool dragging;
    private bool reachedBottom;

    private void OnEnable()
    {
        pointerInside = false;
        dragging = false;
        ResetScroll();
    }

    /// <summary>Starts each newly selected record at the beginning with a reading pause.</summary>
    public void ResetScroll()
    {
        waitRemaining = pauseSeconds;
        reachedBottom = false;
        if (scroll != null)
        {
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        }
    }

    private void Update()
    {
        if (scroll == null || scroll.content == null || scroll.viewport == null || pointerInside || dragging)
        {
            return;
        }

        float overflow = scroll.content.rect.height - scroll.viewport.rect.height;
        if (overflow <= 0f)
        {
            return;
        }

        waitRemaining -= Time.unscaledDeltaTime;
        if (waitRemaining > 0f)
        {
            return;
        }

        if (reachedBottom)
        {
            ResetScroll();
            return;
        }

        scroll.StopMovement();
        scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition
            - pixelsPerSecond * Time.unscaledDeltaTime / overflow);
        if (scroll.verticalNormalizedPosition <= 0f)
        {
            reachedBottom = true;
            waitRemaining = pauseSeconds;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        waitRemaining = pauseSeconds;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        reachedBottom = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        waitRemaining = pauseSeconds;
    }

    public void OnScroll(PointerEventData eventData)
    {
        reachedBottom = false;
        waitRemaining = pauseSeconds;
    }
}
