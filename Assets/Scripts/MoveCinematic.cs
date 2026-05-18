using UnityEngine;

public class MoveCinematic : MonoBehaviour
{
    [Header("Cinematic Targets")]
    [SerializeField] private Transform[] objectsToMove;
    [SerializeField] private Transform target;

    [Header("Motion")]
    [SerializeField] private float duration = 2.5f;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3[] startPositions;
    private Quaternion[] startRotations;
    private Coroutine moveRoutine;
    private void Awake()
    {
        StartCinematic();
    }
    private void OnEnable()
    {
        StartCinematic();
    }

    public void StartCinematic()
    {
        if (objectsToMove == null || objectsToMove.Length == 0 || target == null)
        {
            return;
        }

        CacheStartPositions();

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveObjectsRoutine());
    }

    private void CacheStartPositions()
    {
        startPositions = new Vector3[objectsToMove.Length];
        startRotations = new Quaternion[objectsToMove.Length];

        for (int i = 0; i < objectsToMove.Length; i++)
        {
            if (objectsToMove[i] == null)
            {
                startPositions[i] = Vector3.zero;
                startRotations[i] = Quaternion.identity;
                continue;
            }

            startPositions[i] = objectsToMove[i].position;
            startRotations[i] = objectsToMove[i].rotation;
        }
    }

    private System.Collections.IEnumerator MoveObjectsRoutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float easedT = easing != null ? easing.Evaluate(t) : t;

            for (int i = 0; i < objectsToMove.Length; i++)
            {
                if (objectsToMove[i] == null)
                {
                    continue;
                }

                objectsToMove[i].position = Vector3.LerpUnclamped(startPositions[i], target.position, easedT);
                objectsToMove[i].rotation = Quaternion.SlerpUnclamped(startRotations[i], target.rotation, easedT);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < objectsToMove.Length; i++)
        {
            if (objectsToMove[i] == null)
            {
                continue;
            }

            objectsToMove[i].position = target.position;
            objectsToMove[i].rotation = target.rotation;
         
        }

        moveRoutine = null;
    }
}
