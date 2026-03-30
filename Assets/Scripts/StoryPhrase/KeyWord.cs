using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class KeyWord : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
{
    [Header("UI")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image background;

    [Header("Drag")]
    [SerializeField] private float returnDuration = 0.2f;
    [SerializeField] private float snapDuration = 0.14f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private LayoutElement layoutElement;

    private Transform originParent;
    private int originSiblingIndex;

    private Coroutine returnCoroutine;
    private Coroutine snapCoroutine;
    private bool droppedIntoValidSlot;

    public string WordText { get; private set; }
    public WordType Type { get; private set; }
    public WordSlot CurrentSlot { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
        layoutElement = GetComponent<LayoutElement>();

        originParent = transform.parent;
        originSiblingIndex = transform.GetSiblingIndex();
    }

    public void Initialize(string text, WordType type, Color color)
    {
        WordText = text;
        Type = type;

        if (label != null)
        {
            label.text = text;
        }

        if (background != null)
        {
            background.color = color;
        }

        originParent = transform.parent;
        originSiblingIndex = transform.GetSiblingIndex();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StopMoveAnimations();

        droppedIntoValidSlot = false;
        canvasGroup.blocksRaycasts = false;

        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }

        if (CurrentSlot != null)
        {
            WordSlot previousSlot = CurrentSlot;
            CurrentSlot = null;
            previousSlot.RemoveWord(this, true);
        }

        Vector2 currentSize = rectTransform.rect.size;
        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();
        PreserveSize(currentSize);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (!droppedIntoValidSlot)
        {
            ReturnToOriginSmooth();
        }
    }

    public void SnapIntoSlot(WordSlot slot)
    {
        droppedIntoValidSlot = true;
        CurrentSlot = slot;

        StopMoveAnimations();
        snapCoroutine = StartCoroutine(AnimateSnap(slot));
    }

    public void ReturnToOriginSmooth()
    {
        CurrentSlot = null;
        droppedIntoValidSlot = false;

        StopMoveAnimations();
        returnCoroutine = StartCoroutine(AnimateReturnToOrigin());
    }

    private IEnumerator AnimateSnap(WordSlot slot)
    {
        RectTransform slotRect = slot.transform as RectTransform;
        if (slotRect == null)
        {
            yield break;
        }

        Vector2 currentSize = rectTransform.rect.size;
        transform.SetParent(slot.transform, true);

        ConfigureCenteredRect();
        PreserveSize(currentSize);

        Vector3 start = rectTransform.position;
        Vector3 target = slotRect.TransformPoint(slotRect.rect.center);
        float elapsed = 0f;

        while (elapsed < snapDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / snapDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            rectTransform.position = Vector3.Lerp(start, target, eased);
            yield return null;
        }

        rectTransform.position = target;
        rectTransform.anchoredPosition = Vector2.zero;
        snapCoroutine = null;
    }

    private IEnumerator AnimateReturnToOrigin()
    {
        if (rootCanvas == null || originParent == null)
        {
            yield break;
        }

        Vector2 currentSize = rectTransform.rect.size;
        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();
        PreserveSize(currentSize);

        Vector3 start = rectTransform.position;
        Vector3 target = ComputeCurrentOriginWorldPosition();
        float elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / returnDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            rectTransform.position = Vector3.Lerp(start, target, eased);
            yield return null;
        }

        rectTransform.position = target;
        transform.SetParent(originParent, true);

        int clampedIndex = Mathf.Clamp(originSiblingIndex, 0, originParent.childCount - 1);
        transform.SetSiblingIndex(clampedIndex);

        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = false;
        }

        RectTransform originRect = originParent as RectTransform;
        if (originRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(originRect);
        }

        returnCoroutine = null;
    }

    private Vector3 ComputeCurrentOriginWorldPosition()
    {
        if (originParent == null)
        {
            return rectTransform.position;
        }

        Vector3 currentWorldPosition = rectTransform.position;
        transform.SetParent(originParent, true);

        int clampedIndex = Mathf.Clamp(originSiblingIndex, 0, originParent.childCount - 1);
        transform.SetSiblingIndex(clampedIndex);

        RectTransform originRect = originParent as RectTransform;
        if (originRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(originRect);
        }

        Vector3 targetWorldPosition = rectTransform.position;

        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();
        rectTransform.position = currentWorldPosition;

        return targetWorldPosition;
    }

    private void ConfigureCenteredRect()
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private void PreserveSize(Vector2 size)
    {
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
    }

    private void StopMoveAnimations()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }

        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }
    }
}
