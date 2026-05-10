using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSetup : MonoBehaviour
{
    private static readonly float[] RotationYCycle = { 45f, 0f, -45f };

    [Serializable]
    private struct SpawnPoint
    {
        public Vector3 position;
        public Vector3 rotationEuler;
    }

    [Header("Settings")]
    [SerializeField] private int copiesPerCharacter = 6;

    [Header("Character 1")]
    [SerializeField] private Transform character1Parent;
    [SerializeField] private List<SpawnPoint> character1SpawnPoints = new List<SpawnPoint>();

    [Header("Character 2")]
    [SerializeField] private Transform character2Parent;
    [SerializeField] private List<SpawnPoint> character2SpawnPoints = new List<SpawnPoint>();

    private readonly List<GameObject> character1Copies = new List<GameObject>();
    private readonly List<GameObject> character2Copies = new List<GameObject>();
    private readonly List<int> character1RotationState = new List<int>();
    private readonly List<int> character2RotationState = new List<int>();

    public void GenerateCopies(int characterNumber, GameObject sourceRoot)
    {
        if (sourceRoot == null)
        {
            return;
        }

        int safeCopies = Mathf.Max(0, copiesPerCharacter);
        if (safeCopies <= 0)
        {
            return;
        }

        List<GameObject> targetCopies = characterNumber == 1 ? character1Copies : character2Copies;
        List<SpawnPoint> spawnPoints = characterNumber == 1 ? character1SpawnPoints : character2SpawnPoints;
        List<int> targetRotationState = characterNumber == 1 ? character1RotationState : character2RotationState;
        Transform parent = characterNumber == 1 ? character1Parent : character2Parent;

        ClearCopies(targetCopies);
        targetRotationState.Clear();

        for (int i = 0; i < safeCopies; i++)
        {
            GameObject copy = Instantiate(sourceRoot, parent);
            copy.name = string.Format("{0}_Copy_{1}", sourceRoot.name, i);

            CharacterDraggable draggable = copy.GetComponent<CharacterDraggable>();
            if (draggable == null)
            {
                draggable = copy.AddComponent<CharacterDraggable>();
            }

            draggable.SetDragIndex(i, Mathf.Max(0, characterNumber - 1));

            SpawnPoint point;
            if (TryGetSpawnPoint(spawnPoints, i, out point))
            {
                copy.transform.SetPositionAndRotation(point.position, Quaternion.Euler(point.rotationEuler));
            }
            else
            {
                copy.transform.SetPositionAndRotation(sourceRoot.transform.position, sourceRoot.transform.rotation);
                Debug.LogWarning(string.Format("CharacterSetup: falta SpawnPoint para personaje {0} en index {1}.", characterNumber, i));
            }

            copy.SetActive(true);
            targetCopies.Add(copy);
            targetRotationState.Add(GetClosestRotationStateIndex(copy.transform.eulerAngles.y));

            // Registrar Animator del hijo (si existe) en AnimationManager
            Animator foundAnimator = copy.GetComponentInChildren<Animator>();
            if (foundAnimator != null)
            {
                AnimationManager animManager = FindObjectOfType<AnimationManager>();
                if (animManager != null)
                {
                    animManager.SetAnimatorAt(i, foundAnimator, characterNumber == 1);
                }
            }
        }
    }

    public bool ToggleCopyRotation(int copyIndex, int characterNumber)
    {
        List<GameObject> copies = characterNumber == 1 ? character1Copies : character2Copies;

        if (copyIndex < 0 || copyIndex >= copies.Count)
        {
            return false;
        }

        GameObject copy = copies[copyIndex];
        if (copy == null)
        {
            return false;
        }

        Vector3 euler = copy.transform.eulerAngles;
        euler.y += 30f;
        copy.transform.rotation = Quaternion.Euler(euler);
        return true;
    }

    public void ResetAllCopiesToSpawn()
    {
        ResetCopiesToSpawn(character1Copies, character1SpawnPoints);
        ResetCopiesToSpawn(character2Copies, character2SpawnPoints);
    }

    public void SetCharacterZPosition(int copyIndex, int characterNumber, float zPosition)
    {
        List<GameObject> copies = characterNumber == 1 ? character1Copies : character2Copies;

        if (copyIndex < 0 || copyIndex >= copies.Count)
        {
            return;
        }

        GameObject copy = copies[copyIndex];
        if (copy == null)
        {
            return;
        }

        Vector3 position = copy.transform.position;
        position.z = zPosition;
        copy.transform.position = position;
    }

    private static void ResetCopiesToSpawn(List<GameObject> copies, List<SpawnPoint> spawnPoints)
    {
        if (copies == null)
        {
            return;
        }

        for (int i = 0; i < copies.Count; i++)
        {
            GameObject copy = copies[i];
            if (copy == null)
            {
                continue;
            }

            SpawnPoint point;
            if (TryGetSpawnPoint(spawnPoints, i, out point))
            {
                copy.transform.SetPositionAndRotation(point.position, Quaternion.Euler(point.rotationEuler));
            }
        }
    }

    private static int GetClosestRotationStateIndex(float currentY)
    {
        float normalizedY = NormalizeAngle(currentY);
        int bestIndex = 0;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < RotationYCycle.Length; i++)
        {
            float distance = Mathf.Abs(Mathf.DeltaAngle(normalizedY, RotationYCycle[i]));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static float NormalizeAngle(float value)
    {
        value %= 360f;
        if (value > 180f)
        {
            value -= 360f;
        }

        return value;
    }

    private static bool TryGetSpawnPoint(List<SpawnPoint> spawnPoints, int index, out SpawnPoint point)
    {
        point = default;

        if (spawnPoints == null || index < 0 || index >= spawnPoints.Count)
        {
            return false;
        }

        point = spawnPoints[index];
        return true;
    }

    private static void ClearCopies(List<GameObject> copies)
    {
        if (copies == null)
        {
            return;
        }

        for (int i = copies.Count - 1; i >= 0; i--)
        {
            GameObject copy = copies[i];
            if (copy != null)
            {
                Destroy(copy);
            }
        }

        copies.Clear();
    }
}
