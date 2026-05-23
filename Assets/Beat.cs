using UnityEngine;

public class Beat : MonoBehaviour
{
    [Header("Beat Settings")]
    [SerializeField] private float bpm = 60f;
    [SerializeField] private float growDuration = 0.2f;
    [SerializeField] private float decayDuration = 0.5f;
    [SerializeField] private float scaleAmplitude = 0.12f;
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve decayCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float safeBpm = Mathf.Max(1f, bpm);
        float period = 60f / safeBpm;
        float safeGrow = Mathf.Max(0.01f, growDuration);
        float safeDecay = Mathf.Max(0.01f, decayDuration);
        float cycleTime = safeGrow + safeDecay;
        float cycleScale = period / cycleTime;
        safeGrow *= cycleScale;
        safeDecay *= cycleScale;

        float phase = Time.time % period;
        float pulse = 0f;
        if (phase <= safeGrow)
        {
            float t = phase / safeGrow;
            float curveValue = growCurve != null ? growCurve.Evaluate(t) : t;
            pulse = Mathf.Clamp01(curveValue) * scaleAmplitude;
        }
        else
        {
            float t = (phase - safeGrow) / safeDecay;
            float curveValue = decayCurve != null ? decayCurve.Evaluate(t) : (1f - t);
            pulse = Mathf.Clamp01(curveValue) * scaleAmplitude;
        }

        transform.localScale = baseScale * (1f + pulse);
    }

    private void OnValidate()
    {
        bpm = Mathf.Max(1f, bpm);
        growDuration = Mathf.Max(0.01f, growDuration);
        decayDuration = Mathf.Max(0.01f, decayDuration);
        scaleAmplitude = Mathf.Max(0f, scaleAmplitude);
    }
}
