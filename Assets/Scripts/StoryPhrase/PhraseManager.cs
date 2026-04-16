using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhraseManager : MonoBehaviour
{
    [System.Serializable]
    private class SceneDefinition
    {
        public string name;
        [TextArea]
        public string template;
        public List<WordType> slots = new List<WordType>();
        public List<string> pronombres = new List<string>();
        public List<string> sujetos = new List<string>();
        public List<string> lugares = new List<string>();
        public List<string> acciones = new List<string>();
        public List<string> accionesP1 = new List<string>();
        public List<string> accionesP2 = new List<string>();
    }

    private struct CharacterSelection
    {
        public string tipo1;
        public string tipo2;
        public string tipo3;
        public string personaje;
    }

    [Header("Phrase Source")]
    [TextArea]
    [SerializeField] private string phraseTemplate = "Habia una vez un _ que vivia en _ y buscaba _.";
    [SerializeField] private List<WordType> slotTypes = new List<WordType>();

    [Header("Narrative Scenes")]
    [SerializeField] private List<SceneDefinition> scenes = new List<SceneDefinition>();

    [Header("UI Prefabs")]
    [SerializeField] private Transform phraseContainer;
    [SerializeField] private TMP_Text textBlockPrefab;
    [SerializeField] private WordSlot slotPrefab;

    [Header("Wrapped Layout")]
    [SerializeField] private bool wrapInsideScreen = true;
    [SerializeField] private float startOffsetX = 0f;
    [SerializeField] private float startOffsetY = 16f;
    [SerializeField] private float rightPadding = 16f;
    [SerializeField] private float horizontalSpacing = 8f;
    [SerializeField] private float minLineBreakStep = 70f;
    [SerializeField] private float lineSpacing = 8f;
    [SerializeField] private bool forceSingleLineLayout = true;
    [SerializeField] private int widthStepCharacters = 15;

    [Header("Dependencies")]
    [SerializeField] private KeyWordManager keyWordManager;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text finalPhraseOutputText;
    [SerializeField] private GameObject guionPanel;

    private readonly List<WordSlot> slots = new List<WordSlot>();
    private string[] parsedSegments;
    private bool isCompleted;
    private int currentSceneIndex;
    private CharacterSelection character1;
    private CharacterSelection character2;
    private bool persistentSelectionsReady;

    private void Awake()
    {
        EnsureDefaultScenes();
        currentSceneIndex = 0;
        ApplyScene(currentSceneIndex);

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinuePressed);
            continueButton.interactable = false;
        }
    }

    private void Start()
    {
        BuildPhrase();
        NotifySlotStateChanged();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinuePressed);
        }
    }

    public void BuildPhrase()
    {
        if (phraseContainer == null || textBlockPrefab == null || slotPrefab == null)
        {
            Debug.LogWarning("PhraseManager is missing phrase UI references.");
            return;
        }

        ClearPhraseContainer();
        slots.Clear();

        parsedSegments = phraseTemplate.Split('_');
        int placeholderCount = Mathf.Max(0, parsedSegments.Length - 1);

        if (slotTypes.Count != placeholderCount)
        {
            Debug.LogWarning($"Slot type count ({slotTypes.Count}) does not match placeholders ({placeholderCount}).");
        }

        for (int i = 0; i < parsedSegments.Length; i++)
        {
            string segment = parsedSegments[i];
            if (!string.IsNullOrEmpty(segment))
            {
                TMP_Text textBlock = Instantiate(textBlockPrefab, phraseContainer);
                textBlock.text = segment;
                ConfigureTextBlockSingleLine(textBlock, segment);
            }

            if (i >= placeholderCount)
            {
                continue;
            }

            WordType requiredType = i < slotTypes.Count ? slotTypes[i] : WordType.Sujeto;
            WordSlot slot = Instantiate(slotPrefab, phraseContainer);

            Color slotColor = keyWordManager != null
                ? keyWordManager.GetColorForType(requiredType)
                : Color.white;

            slot.Initialize(requiredType, this, slotColor);
            slots.Add(slot);
        }

        RefreshWrappedLayout();
    }

    public void NotifySlotStateChanged()
    {
        bool allFilled = true;

        foreach (WordSlot slot in slots)
        {
            if (slot.CurrentWord == null)
            {
                allFilled = false;
                break;
            }
        }

        if (continueButton != null)
        {
            continueButton.interactable = allFilled;
        }

        RefreshWrappedLayout();

        if (allFilled && !isCompleted)
        {
            Debug.Log("Todos los espacios estan completos.");
        }

        isCompleted = allFilled;
    }

    public string BuildFinalPhrase()
    {
        if (parsedSegments == null || parsedSegments.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < parsedSegments.Length; i++)
        {
            builder.Append(parsedSegments[i]);

            if (i < slots.Count)
            {
                string value = slots[i].CurrentWord != null
                    ? slots[i].CurrentWord.WordText
                    : "_";

                builder.Append(value);
            }
        }

        return builder.ToString();
    }

    private void ClearPhraseContainer()
    {
        if (phraseContainer == null)
        {
            return;
        }

        for (int i = phraseContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = phraseContainer.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    private void OnContinuePressed()
    {
        if (!isCompleted)
        {
            return;
        }

        string finalResult = BuildFinalPhrase();
        if (finalPhraseOutputText != null)
        {
            finalPhraseOutputText.text = finalResult;
        }

        if (currentSceneIndex == 0)
        {
            CapturePersistentSelectionsFromSceneOne();
        }

        if (currentSceneIndex >= scenes.Count - 1)
        {
            if (guionPanel != null)
            {
                guionPanel.SetActive(false);
            }

            Debug.Log($"Historia finalizada: {finalResult}");
            return;
        }

        currentSceneIndex++;
        ApplyScene(currentSceneIndex);
    }

    private void RefreshWrappedLayout()
    {
        if (!wrapInsideScreen)
        {
            return;
        }

        RectTransform containerRect = phraseContainer as RectTransform;
        if (containerRect == null)
        {
            return;
        }

        LayoutGroup group = containerRect.GetComponent<LayoutGroup>();
        if (group != null && group.enabled)
        {
            group.enabled = false;
        }

        float containerWidth = containerRect.rect.width;
        if (containerWidth <= 0f)
        {
            Canvas canvas = containerRect.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect != null)
            {
                containerWidth = canvasRect.rect.width;
            }
        }

        if (containerWidth <= 0f)
        {
            return;
        }

        float maxX = containerWidth - rightPadding;
        float cursorX = 0f;
        float cursorY = -startOffsetY;
        float currentLineHeight = 0f;
        bool hasItemsInLine = false;

        for (int i = 0; i < containerRect.childCount; i++)
        {
            RectTransform child = containerRect.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf)
            {
                continue;
            }

            float width = LayoutUtility.GetPreferredWidth(child);
            if (width <= 0f)
            {
                width = child.rect.width;
            }

            float height = LayoutUtility.GetPreferredHeight(child);
            if (height <= 0f)
            {
                height = child.rect.height;
            }

            bool wouldOverflow = cursorX + width > maxX;
            if (!forceSingleLineLayout && wouldOverflow && hasItemsInLine)
            {
                float lineStep = Mathf.Max(minLineBreakStep, currentLineHeight + lineSpacing);
                cursorX = 0f;
                cursorY -= lineStep;
                currentLineHeight = 0f;
                hasItemsInLine = false;
            }

            child.anchorMin = new Vector2(0f, 1f);
            child.anchorMax = new Vector2(0f, 1f);
            child.pivot = new Vector2(0f, 1f);
            child.anchoredPosition = new Vector2(cursorX, cursorY);

            cursorX += width + horizontalSpacing;
            currentLineHeight = Mathf.Max(currentLineHeight, height);
            hasItemsInLine = true;
        }
    }

    private void ConfigureTextBlockSingleLine(TMP_Text textBlock, string content)
    {
        if (textBlock == null)
        {
            return;
        }

        textBlock.enableWordWrapping = false;
        textBlock.overflowMode = TextOverflowModes.Overflow;
        textBlock.ForceMeshUpdate();

        RectTransform rect = textBlock.rectTransform;
        if (rect == null)
        {
            return;
        }

        int safeStep = Mathf.Max(1, widthStepCharacters);
        int charCount = string.IsNullOrEmpty(content) ? 0 : content.Length;
        int steps = Mathf.Max(1, Mathf.CeilToInt((float)charCount / safeStep));

        float baseWidth = rect.rect.width;
        if (baseWidth <= 0f)
        {
            baseWidth = textBlock.preferredWidth;
        }

        float expandedWidth = Mathf.Max(textBlock.preferredWidth, baseWidth * steps);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, expandedWidth);

        LayoutElement layoutElement = textBlock.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.preferredWidth = expandedWidth;
            layoutElement.minWidth = expandedWidth;
        }
    }

    private void EnsureDefaultScenes()
    {
        if (scenes != null && scenes.Count > 0)
        {
            return;
        }

        scenes = new List<SceneDefinition>
        {
            new SceneDefinition
            {
                name = "Escena 1",
                template = "Un dia, _ _ y _ _ estaban en _.",
                slots = new List<WordType>
                {
                    WordType.Pronombre,
                    WordType.Sujeto,
                    WordType.Pronombre,
                    WordType.Sujeto,
                    WordType.Lugar,
                },
                pronombres = new List<string> { "un", "una" },
                sujetos = new List<string> { "estudiante", "policia", "deportista", "artista" },
                lugares = new List<string> { "parque", "banco", "calle", "escuela" }
            },
            new SceneDefinition
            {
                name = "Escena 2",
                template = "De pronto, {C1_T2} {C1_P} _ a {C2_T2} {C2_P} y {C2_T2} {C2_P} _.",
                slots = new List<WordType> { WordType.AccionP1, WordType.AccionP2 },
                accionesP1 = new List<string> { "empuja", "acusa", "sigue", "bloquea" },
                accionesP2 = new List<string> { "responde", "huye", "grita", "se aparta" }
            },
            new SceneDefinition
            {
                name = "Escena 3",
                template = "La tension crece: {C1_T2} {C1_P} _ mientras {C2_T2} {C2_P} _.",
                slots = new List<WordType> { WordType.AccionP1, WordType.AccionP2 },
                accionesP1 = new List<string> { "insiste", "grita", "persigue", "amenaza" },
                accionesP2 = new List<string> { "discute", "retrocede", "observa", "se defiende" }
            },
            new SceneDefinition
            {
                name = "Escena 4",
                template = "Mas tarde, en _, {C1_T2} {C1_P} y {C2_T2} {C2_P} se encuentran otra vez.",
                slots = new List<WordType> { WordType.Lugar },
                lugares = new List<string> { "estacion", "hospital", "casa", "oficina" }
            },
            new SceneDefinition
            {
                name = "Escena 5",
                template = "Alli, {C1_T2} {C1_P} _, mientras {C2_T2} {C2_P} _.",
                slots = new List<WordType> { WordType.AccionP1, WordType.AccionP2 },
                accionesP1 = new List<string> { "explica", "confronta", "insiste", "observa" },
                accionesP2 = new List<string> { "escucha", "duda", "responde", "se disculpa" }
            },
            new SceneDefinition
            {
                name = "Escena 6",
                template = "Al final, {C1_T2} {C1_P} _ a {C2_T2} {C2_P} y {C2_T2} {C2_P} _.",
                slots = new List<WordType> { WordType.AccionP1, WordType.AccionP2 },
                accionesP1 = new List<string> { "perdona", "ayuda", "ignora", "enfrenta" },
                accionesP2 = new List<string> { "acepta", "rechaza", "responde", "se aleja" }
            }
        };
    }

    private void ApplyScene(int sceneIndex)
    {
        if (scenes == null || scenes.Count == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(sceneIndex, 0, scenes.Count - 1);
        SceneDefinition scene = scenes[clampedIndex];

        phraseTemplate = ResolvePersistentTokens(scene.template);
        slotTypes = new List<WordType>(scene.slots);

        if (keyWordManager != null)
        {
            keyWordManager.SetWords(CreateEntriesForScene(scene));
        }

        BuildPhrase();
        NotifySlotStateChanged();

        Debug.Log($"Escena cargada: {scene.name}");
    }

    private List<KeyWordManager.KeyWordEntry> CreateEntriesForScene(SceneDefinition scene)
    {
        List<KeyWordManager.KeyWordEntry> entries = new List<KeyWordManager.KeyWordEntry>();
        Dictionary<WordType, int> typeUsageCount = CountTypeUsage(scene.slots);

        AddEntriesRepeated(entries, scene.pronombres, WordType.Pronombre, GetUsageCount(typeUsageCount, WordType.Pronombre));
        AddEntriesRepeated(entries, scene.sujetos, WordType.Sujeto, GetUsageCount(typeUsageCount, WordType.Sujeto));
        AddEntriesRepeated(entries, scene.lugares, WordType.Lugar, GetUsageCount(typeUsageCount, WordType.Lugar));
        AddEntriesRepeated(entries, scene.acciones, WordType.Accion, GetUsageCount(typeUsageCount, WordType.Accion));
        AddEntriesRepeated(entries, scene.accionesP1, WordType.AccionP1, GetUsageCount(typeUsageCount, WordType.AccionP1));
        AddEntriesRepeated(entries, scene.accionesP2, WordType.AccionP2, GetUsageCount(typeUsageCount, WordType.AccionP2));

        return entries;
    }

    private void CapturePersistentSelectionsFromSceneOne()
    {
        if (slots.Count < 4)
        {
            return;
        }

        string c1Tipo1 = GetSlotValue(0, "un");
        string c1Personaje = GetSlotValue(1, "estudiante");
        string c2Tipo1 = GetSlotValue(2, "un");
        string c2Personaje = GetSlotValue(3, "estudiante");

        character1 = BuildCharacterSelection(c1Tipo1, c1Personaje);
        character2 = BuildCharacterSelection(c2Tipo1, c2Personaje);
        persistentSelectionsReady = true;
    }

    private string ResolvePersistentTokens(string template)
    {
        if (string.IsNullOrEmpty(template) || !persistentSelectionsReady)
        {
            return template;
        }

        return template
            .Replace("{C1_T1}", character1.tipo1)
            .Replace("{C1_T2}", character1.tipo2)
            .Replace("{C1_T3}", character1.tipo3)
            .Replace("{C1_P}", character1.personaje)
            .Replace("{C2_T1}", character2.tipo1)
            .Replace("{C2_T2}", character2.tipo2)
            .Replace("{C2_T3}", character2.tipo3)
            .Replace("{C2_P}", character2.personaje);
    }

    private static CharacterSelection BuildCharacterSelection(string tipo1, string personaje)
    {
        string normalizedTipo1 = string.IsNullOrWhiteSpace(tipo1) ? "un" : tipo1.Trim().ToLowerInvariant();
        bool isFeminine = normalizedTipo1 == "una";

        return new CharacterSelection
        {
            tipo1 = isFeminine ? "una" : "un",
            tipo2 = isFeminine ? "la" : "el",
            tipo3 = isFeminine ? "ella" : "él",
            personaje = string.IsNullOrWhiteSpace(personaje) ? "estudiante" : personaje.Trim().ToLowerInvariant()
        };
    }

    private string GetSlotValue(int index, string fallback)
    {
        if (index < 0 || index >= slots.Count)
        {
            return fallback;
        }

        KeyWord word = slots[index].CurrentWord;
        if (word == null || string.IsNullOrWhiteSpace(word.WordText))
        {
            return fallback;
        }

        return word.WordText;
    }

    private static Dictionary<WordType, int> CountTypeUsage(List<WordType> types)
    {
        Dictionary<WordType, int> counts = new Dictionary<WordType, int>();
        if (types == null)
        {
            return counts;
        }

        foreach (WordType type in types)
        {
            if (!counts.ContainsKey(type))
            {
                counts[type] = 0;
            }

            counts[type]++;
        }

        return counts;
    }

    private static int GetUsageCount(Dictionary<WordType, int> counts, WordType type)
    {
        int count;
        if (counts != null && counts.TryGetValue(type, out count))
        {
            return Mathf.Max(0, count);
        }

        return 0;
    }

    private static void AddEntriesRepeated(List<KeyWordManager.KeyWordEntry> target, List<string> values, WordType type, int repeats)
    {
        if (repeats <= 0)
        {
            return;
        }

        for (int i = 0; i < repeats; i++)
        {
            AddEntries(target, values, type);
        }
    }

    private static void AddEntries(List<KeyWordManager.KeyWordEntry> target, List<string> values, WordType type)
    {
        if (values == null)
        {
            return;
        }

        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            target.Add(new KeyWordManager.KeyWordEntry
            {
                text = value,
                type = type
            });
        }
    }
}
