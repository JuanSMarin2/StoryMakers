using System;
using System.Collections.Generic;
using UnityEngine;

public class KeyWordManager : MonoBehaviour
{
    [Serializable]
    public class KeyWordEntry
    {
        public string text;
        public WordType type;
    }

    [Header("Data")]
    [SerializeField] private List<KeyWordEntry> words = new List<KeyWordEntry>();

    [Header("UI")]
    [SerializeField] private Transform sujetoContainer;
    [SerializeField] private Transform accionContainer;
    [SerializeField] private Transform lugarContainer;
    [SerializeField] private Transform fallbackContainer;
    [SerializeField] private KeyWord keyWordPrefab;

    [Header("Colors by Type")]
    [SerializeField] private Color sujetoColor = new Color(0.95f, 0.72f, 0.40f, 1f);
    [SerializeField] private Color lugarColor = new Color(0.47f, 0.78f, 0.87f, 1f);
    [SerializeField] private Color accionColor = new Color(0.64f, 0.87f, 0.53f, 1f);
    [SerializeField] private Color pronombreColor = new Color(0.63f, 0.46f, 0.95f, 1f);

    private readonly List<KeyWord> spawnedWords = new List<KeyWord>();

    private void Start()
    {
        SpawnWords();
    }

    public Color GetColorForType(WordType type)
    {
        switch (type)
        {
            case WordType.Sujeto:
                return sujetoColor;
            case WordType.Lugar:
                return lugarColor;
            case WordType.Accion:
            case WordType.AccionP1:
            case WordType.AccionP2:
                return accionColor;
            case WordType.Pronombre:
                return pronombreColor;
            default:
                return Color.white;
        }
    }

    public void SpawnWords()
    {
        if (keyWordPrefab == null)
        {
            Debug.LogWarning("KeyWordManager is missing keyWordPrefab reference.");
            return;
        }

        if (sujetoContainer == null || accionContainer == null || lugarContainer == null)
        {
            if (fallbackContainer == null)
            {
                Debug.LogWarning("KeyWordManager is missing one or more type containers and fallbackContainer.");
                return;
            }

            Debug.LogWarning("KeyWordManager is missing one or more type containers. Using fallbackContainer.");
        }

        ClearSpawnedWords();

        foreach (KeyWordEntry entry in words)
        {
            Transform targetContainer = GetContainerForType(entry.type);
            if (targetContainer == null)
            {
                continue;
            }

            KeyWord word = Instantiate(keyWordPrefab, targetContainer);
            Color color = GetColorForType(entry.type);
            word.Initialize(entry.text, entry.type, color);
            spawnedWords.Add(word);
        }
    }

    public void SetWords(List<KeyWordEntry> newWords)
    {
        words = newWords != null ? new List<KeyWordEntry>(newWords) : new List<KeyWordEntry>();

        if (isActiveAndEnabled)
        {
            SpawnWords();
        }
    }

    private Transform GetContainerForType(WordType type)
    {
        switch (type)
        {
            case WordType.Sujeto:
                return sujetoContainer != null ? sujetoContainer : fallbackContainer;
            case WordType.Accion:
            case WordType.AccionP1:
            case WordType.AccionP2:
            case WordType.Pronombre:
                return accionContainer != null ? accionContainer : fallbackContainer;
            case WordType.Lugar:
                return lugarContainer != null ? lugarContainer : fallbackContainer;
            default:
                return fallbackContainer;
        }
    }

    private void ClearSpawnedWords()
    {
        for (int i = spawnedWords.Count - 1; i >= 0; i--)
        {
            KeyWord spawnedWord = spawnedWords[i];
            if (spawnedWord != null)
            {
                Destroy(spawnedWord.gameObject);
            }
        }

        spawnedWords.Clear();
    }
}
