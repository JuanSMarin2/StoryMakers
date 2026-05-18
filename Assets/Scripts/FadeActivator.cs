using System.Collections;
using UnityEngine;

public class FadeActivator : MonoBehaviour
{
    [Header("Scale")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float minScaleFactor = 0.05f;

    private Vector3 targetScale;
    private Coroutine activeRoutine;

    private void Awake()
    {
        targetScale = transform.localScale;
    }

    private void OnEnable()
    {
        PlayIn();
    }

    public void Activate()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        else
        {
            PlayIn();
        }
    }

    public void Deactivate()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        PlayOut();
    }

    private void PlayIn()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        Vector3 startScale = targetScale * Mathf.Max(minScaleFactor, 0f);
        transform.localScale = startScale;
        activeRoutine = StartCoroutine(ScaleRoutine(startScale, targetScale, false));
    }

    private void PlayOut()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        Vector3 endScale = targetScale * Mathf.Max(minScaleFactor, 0f);
        activeRoutine = StartCoroutine(ScaleRoutine(transform.localScale, endScale, true));
    }

    private IEnumerator ScaleRoutine(Vector3 from, Vector3 to, bool deactivateAtEnd)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float easedT = easing != null ? easing.Evaluate(t) : t;

            transform.localScale = Vector3.LerpUnclamped(from, to, easedT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = to;
        activeRoutine = null;

        if (deactivateAtEnd)
        {
            gameObject.SetActive(false);
        }
    }
}
