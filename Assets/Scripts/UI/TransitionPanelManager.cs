using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RectTransform))]
public class TransitionPanelManager : MonoBehaviour
{
    public static TransitionPanelManager Instance { get; private set; }

    [Header("Panel Target")]
    [SerializeField] private RectTransform transitionPanel;

    [Header("Transition In (Scene Change)")]
    [SerializeField] private float transitionInDuration = 0.45f;
    [SerializeField] private Ease transitionInEase = Ease.InCubic;

    [Header("Transition Out (Scene Start)")]
    [SerializeField] private float transitionOutDuration = 0.45f;
    [SerializeField] private Ease transitionOutEase = Ease.OutCubic;

    [Header("Offsets")]
    [SerializeField] private float offscreenPadding = 16f;
    [SerializeField] private bool startCovered = true;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private bool hasCachedOriginal;
    private Tween activeTween;
    private bool isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        rectTransform = ResolvePanelRect();
        if (rectTransform == null)
        {
            Debug.LogWarning("TransitionPanelManager: no transition panel assigned or found.");
            return;
        }
        EnsureOriginalPosition();

        if (startCovered)
        {
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }
        else
        {
            rectTransform.anchoredPosition = GetOffscreenUpPosition();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void ChangeScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        if (isTransitioning)
        {
            return;
        }

        StartCoroutine(ChangeSceneRoutine(() => SceneManager.LoadScene(sceneName)));
    }

    public void ChangeScene(int buildIndex)
    {
        if (isTransitioning)
        {
            return;
        }

        StartCoroutine(ChangeSceneRoutine(() => SceneManager.LoadScene(buildIndex)));
    }

    public void TransitionIn()
    {
        if (!EnsurePanelReady())
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        EnsureOriginalPosition();

        if (!rectTransform.gameObject.activeSelf)
        {
            rectTransform.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
        }

        KillActiveTween();

        rectTransform.anchoredPosition = GetOffscreenDownPosition();
        activeTween = rectTransform
            .DOAnchorPos(originalAnchoredPosition, transitionInDuration)
            .SetEase(transitionInEase);
    }

    public void TransitionOut()
    {
        if (!EnsurePanelReady())
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        EnsureOriginalPosition();

        if (!rectTransform.gameObject.activeSelf)
        {
            rectTransform.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
        }

        KillActiveTween();

        activeTween = rectTransform
            .DOAnchorPos(GetOffscreenUpPosition(), transitionOutDuration)
            .SetEase(transitionOutEase);
    }

    public void RefreshOriginalPosition()
    {
        rectTransform = ResolvePanelRect();
        if (rectTransform == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        originalAnchoredPosition = rectTransform.anchoredPosition;
        hasCachedOriginal = true;
    }

    private IEnumerator ChangeSceneRoutine(Action loadAction)
    {
        isTransitioning = true;
        TransitionIn();

        float waitTime = Mathf.Max(0f, transitionInDuration);
        if (waitTime > 0f)
        {
            yield return new WaitForSecondsRealtime(waitTime);
        }

        loadAction?.Invoke();
        isTransitioning = false;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TransitionOut();
    }

    private void EnsureOriginalPosition()
    {
        if (hasCachedOriginal)
        {
            return;
        }

        rectTransform = ResolvePanelRect();
        if (rectTransform == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
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
        if (rectHeight <= 0f && rectTransform != null)
        {
            rectHeight = Mathf.Abs(rectTransform.sizeDelta.y);
        }

        float baseHeight = Mathf.Max(canvasHeight, rectHeight);
        return baseHeight + rectHeight + offscreenPadding;
    }

    private float GetCanvasHeight()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null && rectTransform != null)
        {
            canvas = rectTransform.GetComponentInParent<Canvas>();
        }
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

    private bool EnsurePanelReady()
    {
        if (rectTransform != null)
        {
            return true;
        }

        rectTransform = ResolvePanelRect();
        return rectTransform != null;
    }

    private RectTransform ResolvePanelRect()
    {
        if (transitionPanel != null)
        {
            return transitionPanel;
        }

        if (transform.childCount > 0)
        {
            Transform child = transform.GetChild(0);
            return child != null ? child.GetComponent<RectTransform>() : null;
        }

        return null;
    }
}
