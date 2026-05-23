using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WireShapeSpawner : MonoBehaviour
{
    public enum ShapeType
    {
        Triangle,
        Square,
        Diamond,
        Pentagon,
        Hexagon,
        Octagon,
        Star,
        Circle
    }

    [Header("Area")]
    [SerializeField] private Vector2 areaSize = new Vector2(10f, 6f);
    [SerializeField] private Vector2 areaCenterOffset = Vector2.zero;
    [SerializeField] private bool drawGizmos = true;

    [Header("Depth")]
    [SerializeField] private float zPosition = 0f;
    [SerializeField] private float zJitter = 0f;

    [Header("Population")]
    [SerializeField] private int maxShapes = 12;
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private Vector2 spawnIntervalRange = new Vector2(0.3f, 0.9f);
    [SerializeField] private int maxSpawnAttempts = 8;

    [Header("Lifetime")]
    [SerializeField] private Vector2 durationRange = new Vector2(0.5f, 5f);
    [SerializeField, Range(0.05f, 0.6f)] private float growPortion = 0.25f;
    [SerializeField, Range(0.05f, 0.6f)] private float decayPortion = 0.35f;

    [Header("Scale")]
    [SerializeField] private float baseScale = 1f;
    [SerializeField] private Vector2 scaleVariance = new Vector2(-0.25f, 0.35f);

    [Header("Look")]
    [SerializeField] private float lineWidth = 2f;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int orderInLayer = 0;
    [SerializeField] private List<Color> colors = new List<Color> { Color.cyan, Color.white };
    [SerializeField] private List<float> alphas = new List<float> { 0.35f, 0.6f, 0.85f };
    [SerializeField] private List<ShapeType> shapePool = new List<ShapeType>
    {
        ShapeType.Triangle,
        ShapeType.Square,
        ShapeType.Diamond,
        ShapeType.Pentagon,
        ShapeType.Hexagon,
        ShapeType.Octagon,
        ShapeType.Star,
        ShapeType.Circle
    };
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve decayCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Hierarchy")]
    [SerializeField] private Transform container;

    private readonly List<WireShape> activeShapes = new List<WireShape>();
    private Coroutine spawnRoutine;

    private void OnEnable()
    {
        if (spawnRoutine == null)
        {
            spawnRoutine = StartCoroutine(SpawnLoop());
        }
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (activeShapes.Count < maxShapes)
            {
                TrySpawnShape();
            }

            float wait = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
            yield return new WaitForSeconds(Mathf.Max(0.01f, wait));
        }
    }

    private void TrySpawnShape()
    {
        for (int attempt = 0; attempt < Mathf.Max(1, maxSpawnAttempts); attempt++)
        {
            Vector3 position = GetRandomPosition();
            if (!IsPositionValid(position))
            {
                continue;
            }

            SpawnShape(position);
            return;
        }
    }

    private Vector3 GetRandomPosition()
    {
        Vector2 half = areaSize * 0.5f;
        float x = Random.Range(-half.x, half.x);
        float y = Random.Range(-half.y, half.y);
        float z = zPosition + (zJitter > 0f ? Random.Range(-zJitter, zJitter) : 0f);

        Vector3 center = transform.position + new Vector3(areaCenterOffset.x, areaCenterOffset.y, 0f);
        return new Vector3(center.x + x, center.y + y, z);
    }

    private bool IsPositionValid(Vector3 position)
    {
        if (minDistance <= 0f)
        {
            return true;
        }

        float minDistanceSqr = minDistance * minDistance;
        for (int i = 0; i < activeShapes.Count; i++)
        {
            WireShape shape = activeShapes[i];
            if (shape == null)
            {
                continue;
            }

            Vector2 delta = new Vector2(shape.WorldPosition.x - position.x, shape.WorldPosition.y - position.y);
            float distanceSqr = delta.sqrMagnitude;
            if (distanceSqr < minDistanceSqr)
            {
                return false;
            }
        }

        return true;
    }

    private void SpawnShape(Vector3 position)
    {
        GameObject shapeObject = new GameObject("WireShape");
        Transform parent = container != null ? container : transform;
        shapeObject.transform.SetParent(parent, true);
        shapeObject.transform.position = position;
        shapeObject.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        LineRenderer lineRenderer = shapeObject.AddComponent<LineRenderer>();
        WireShape shape = shapeObject.AddComponent<WireShape>();

        ShapeType shapeType = GetRandomShapeType();
        Vector3[] points = BuildShapePoints(shapeType);

        float duration = Random.Range(durationRange.x, durationRange.y);
        duration = Mathf.Max(0.1f, duration);

        float growTime = Mathf.Clamp(duration * growPortion, 0.05f, duration);
        float decayTime = Mathf.Clamp(duration * decayPortion, 0.05f, duration);
        float total = growTime + decayTime;
        if (total > duration)
        {
            float scale = duration / total;
            growTime *= scale;
            decayTime *= scale;
        }

        float holdTime = Mathf.Max(0f, duration - growTime - decayTime);
        float variance = Random.Range(scaleVariance.x, scaleVariance.y);
        float targetScale = Mathf.Max(0.05f, baseScale + variance);

        Color color = PickColor();
        shape.Setup(lineRenderer, points, color, lineWidth, lineMaterial, sortingLayerName, orderInLayer, growTime, holdTime, decayTime, targetScale, growCurve, decayCurve, OnShapeFinished);

        activeShapes.Add(shape);
    }

    private ShapeType GetRandomShapeType()
    {
        if (shapePool == null || shapePool.Count == 0)
        {
            return ShapeType.Circle;
        }

        return shapePool[Random.Range(0, shapePool.Count)];
    }

    private Color PickColor()
    {
        Color baseColor = colors != null && colors.Count > 0
            ? colors[Random.Range(0, colors.Count)]
            : Color.white;

        float alpha = alphas != null && alphas.Count > 0
            ? alphas[Random.Range(0, alphas.Count)]
            : 1f;

        baseColor.a = Mathf.Clamp01(alpha);
        return baseColor;
    }

    private Vector3[] BuildShapePoints(ShapeType shapeType)
    {
        switch (shapeType)
        {
            case ShapeType.Triangle:
                return BuildPolygon(3, 0.5f);
            case ShapeType.Square:
                return BuildPolygon(4, 0.5f);
            case ShapeType.Diamond:
                return BuildPolygon(4, 0.5f, 45f);
            case ShapeType.Pentagon:
                return BuildPolygon(5, 0.5f);
            case ShapeType.Hexagon:
                return BuildPolygon(6, 0.5f);
            case ShapeType.Octagon:
                return BuildPolygon(8, 0.5f);
            case ShapeType.Star:
                return BuildStar(5, 0.5f, 0.22f);
            case ShapeType.Circle:
            default:
                return BuildPolygon(24, 0.5f);
        }
    }

    private static Vector3[] BuildPolygon(int sides, float radius, float startAngleDegrees = 0f)
    {
        int safeSides = Mathf.Max(3, sides);
        Vector3[] points = new Vector3[safeSides];
        float step = Mathf.PI * 2f / safeSides;
        float start = Mathf.Deg2Rad * startAngleDegrees;

        for (int i = 0; i < safeSides; i++)
        {
            float angle = start + step * i;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            points[i] = new Vector3(x, y, 0f);
        }

        return points;
    }

    private static Vector3[] BuildStar(int points, float outerRadius, float innerRadius)
    {
        int safePoints = Mathf.Max(3, points);
        int total = safePoints * 2;
        Vector3[] vertices = new Vector3[total];
        float step = Mathf.PI * 2f / total;

        for (int i = 0; i < total; i++)
        {
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            float angle = step * i;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            vertices[i] = new Vector3(x, y, 0f);
        }

        return vertices;
    }

    private void OnShapeFinished(WireShape shape)
    {
        if (shape == null)
        {
            return;
        }

        activeShapes.Remove(shape);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.4f);
        Vector3 worldCenter = transform.position + new Vector3(areaCenterOffset.x, areaCenterOffset.y, 0f);
        Gizmos.DrawWireCube(worldCenter, new Vector3(areaSize.x, areaSize.y, 0.02f));
    }
}
