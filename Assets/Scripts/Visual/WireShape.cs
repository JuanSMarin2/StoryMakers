using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WireShape : MonoBehaviour
{
    public Vector3 WorldPosition => transform.position;
    public Vector3 LocalPosition => transform.localPosition;

    private LineRenderer lineRenderer;
    private Color baseColor;
    private Action<WireShape> onFinished;

    public void Setup(
        LineRenderer renderer,
        Vector3[] points,
        Color color,
        float width,
        Material material,
        string sortingLayer,
        int orderInLayer,
        float growTime,
        float holdTime,
        float decayTime,
        float targetScale,
        AnimationCurve growCurve,
        AnimationCurve decayCurve,
        Action<WireShape> finished)
    {
        lineRenderer = renderer != null ? renderer : GetComponent<LineRenderer>();
        onFinished = finished;
        baseColor = color;

        if (material != null)
        {
            lineRenderer.material = material;
        }
        else
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        lineRenderer.useWorldSpace = false;
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.loop = true;
        lineRenderer.positionCount = points != null ? points.Length : 0;
        lineRenderer.startWidth = Mathf.Max(0.001f, width);
        lineRenderer.endWidth = lineRenderer.startWidth;
        lineRenderer.numCapVertices = 2;
        lineRenderer.numCornerVertices = 2;
        lineRenderer.sortingLayerName = string.IsNullOrWhiteSpace(sortingLayer) ? "Default" : sortingLayer;
        lineRenderer.sortingOrder = orderInLayer;

        if (points != null)
        {
            lineRenderer.SetPositions(points);
        }

        StartCoroutine(Animate(growTime, holdTime, decayTime, targetScale, growCurve, decayCurve));
    }

    private IEnumerator Animate(
        float growTime,
        float holdTime,
        float decayTime,
        float targetScale,
        AnimationCurve growCurve,
        AnimationCurve decayCurve)
    {
        float safeScale = Mathf.Max(0.001f, targetScale);
        float initialScale = safeScale * 0.02f;

        transform.localScale = Vector3.one * initialScale;
        SetAlpha(0f);

        float elapsed = 0f;
        float safeGrow = Mathf.Max(0.01f, growTime);
        while (elapsed < safeGrow)
        {
            float t = elapsed / safeGrow;
            float curve = growCurve != null ? growCurve.Evaluate(t) : t;
            float clamped = Mathf.Clamp01(curve);

            transform.localScale = Vector3.one * Mathf.Lerp(initialScale, safeScale, clamped);
            SetAlpha(clamped);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = Vector3.one * safeScale;
        SetAlpha(1f);

        if (holdTime > 0f)
        {
            yield return new WaitForSeconds(holdTime);
        }

        elapsed = 0f;
        float safeDecay = Mathf.Max(0.01f, decayTime);
        while (elapsed < safeDecay)
        {
            float t = elapsed / safeDecay;
            float curve = decayCurve != null ? decayCurve.Evaluate(t) : (1f - t);
            float clamped = Mathf.Clamp01(curve);

            transform.localScale = Vector3.one * (safeScale * clamped);
            SetAlpha(clamped);

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetAlpha(0f);
        onFinished?.Invoke(this);
        Destroy(gameObject);
    }

    private void SetAlpha(float normalized)
    {
        float alpha = Mathf.Clamp01(normalized) * baseColor.a;
        Color color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}
