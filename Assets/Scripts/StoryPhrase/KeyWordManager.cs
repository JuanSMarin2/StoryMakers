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
    [SerializeField] private Transform wordsContainer;
    [SerializeField] private KeyWord keyWordPrefab;

    [Header("Colors by Type")]
    [SerializeField] private Color sujetoColor = new Color(0.95f, 0.72f, 0.40f, 1f);
    [SerializeField] private Color lugarColor = new Color(0.47f, 0.78f, 0.87f, 1f);
    [SerializeField] private Color accionColor = new Color(0.64f, 0.87f, 0.53f, 1f);

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
                return accionColor;
            default:
                return Color.white;
        }
    }

    public void SpawnWords()
    {
        if (wordsContainer == null || keyWordPrefab == null)
        {
            Debug.LogWarning("KeyWordManager is missing wordsContainer or keyWordPrefab reference.");
            return;
        }

        ClearSpawnedWords();

        foreach (KeyWordEntry entry in words)
        {
            KeyWord word = Instantiate(keyWordPrefab, wordsContainer);
            Color color = GetColorForType(entry.type);
            word.Initialize(entry.text, entry.type, color);
            spawnedWords.Add(word);
        }
    }

    private void ClearSpawnedWords()
    {
        for (int i = wordsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(wordsContainer.GetChild(i).gameObject);
        }

        spawnedWords.Clear();
    }
}
