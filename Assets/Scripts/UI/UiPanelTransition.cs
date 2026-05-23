using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UiPanelTransition : MonoBehaviour
{
    [Header("Transition In")]
    [SerializeField] private float transitionInDuration = 0.45f;
    [SerializeField] private Ease transitionInEase = Ease.OutCubic;

    [Header("Transition Out")]
    [SerializeField] private float transitionOutDuration = 0.35f;
    [SerializeField] private Ease transitionOutEase = Ease.InCubic;

    [Header("Offsets")]
    [SerializeField] private float offscreenPadding = 16f;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private bool hasCachedOriginal;
    private Tween activeTween;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void TransitionIn()
    {
        EnsureOriginalPosition();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
        }

        KillActiveTween();

        rectTransform.anchoredPosition = GetOffscreenUpPosition();
        activeTween = rectTransform
            .DOAnchorPos(originalAnchoredPosition, transitionInDuration)
            .SetEase(transitionInEase);
    }

    public void TransitionOut()
    {
        EnsureOriginalPosition();
        if (!gameObject.activeSelf)
        {
            return;
        }

        KillActiveTween();

        activeTween = rectTransform
            .DOAnchorPos(GetOffscreenDownPosition(), transitionOutDuration)
            .SetEase(transitionOutEase)
            .OnComplete(() => gameObject.SetActive(false));
    }

    public void RefreshOriginalPosition()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        originalAnchoredPosition = rectTransform.anchoredPosition;
        hasCachedOriginal = true;
    }

    private void EnsureOriginalPosition()
    {
        if (hasCachedOriginal)
        {
            return;
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        originalAnchoredPosition = rectTransform.anchoredPosition;
        hasCachedOriginal = true;
    }

    private Vector2 GetOffscreenUpPosition()
    {
        float offset = GetOffscreenOffset();
        return originalAnchoredPosition + Vector2.up * offset;
    }

    private Vector2 GetOffscreenDownPosition()
    {
        float offset = GetOffscreenOffset();
        return originalAnchoredPosition - Vector2.up * offset;
    }

    private float GetOffscreenOffset()
    {
        float canvasHeight = GetCanvasHeight();
        float rectHeight = rectTransform != null ? rectTransform.rect.height : 0f;
        return canvasHeight * 0.5f + rectHeight * 0.5f + offscreenPadding;
    }

    private float GetCanvasHeight()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.transform is RectTransform canvasRect)
        {
            float height = canvasRect.rect.height;
            if (height > 0f)
            {
                return height;
            }
        }

        return Screen.height;
    }

    private void KillActiveTween()
    {
        if (activeTween == null)
        {
            return;
        }

        activeTween.Kill(false);
        activeTween = null;
    }
}
