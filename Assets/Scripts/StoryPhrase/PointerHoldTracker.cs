using System;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class PointerHoldTracker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler
{
    public event Action<bool> HoldChanged;

    public bool IsHeld { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetHeld(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        SetHeld(true);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SetHeld(false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetHeld(false);
    }

    private void OnDisable()
    {
        SetHeld(false);
    }

    private void SetHeld(bool held)
    {
        if (IsHeld == held)
        {
            return;
        }

        IsHeld = held;
        HoldChanged?.Invoke(IsHeld);
    }
}
