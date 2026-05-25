using System;
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
        public string tipo2Capital;
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
    [SerializeField] private TMP_Text sceneNumberText;

    [Header("Wrapped Layout")]
    [SerializeField] private bool wrapInsideScreen = true;
    [SerializeField] private float startOffsetX = 0f;
    [SerializeField] private float startOffsetY = 16f;
    [SerializeField] private float textVerticalOffset = 0f;
    [SerializeField] private float slotVerticalOffset = 0f;
    [SerializeField] private float rightPadding = 16f;
    [SerializeField] private float horizontalSpacing = 0f;
    [SerializeField] private float postSlotSpacing = 4f;
    [SerializeField] private float minLineBreakStep = 70f;
    [SerializeField] private float lineSpacing = 8f;
    [SerializeField] private bool forceSingleLineLayout = true;
    [SerializeField] private int widthStepCharacters = 15;

    [Header("Dependencies")]
    [SerializeField] private KeyWordManager keyWordManager;
    [SerializeField] private Button decideAssetsButton;
    [SerializeField] private Button continueButton;
  
    [SerializeField] private GameObject guionPanel;
    [SerializeField] private UiPanelTransition guionPanelTransition;

    private readonly List<WordSlot> slots = new List<WordSlot>();
    private string[] parsedSegments;
    private bool isCompleted;
    private int currentSceneIndex;
    private CharacterSelection character1;
    private CharacterSelection character2;
    private bool character1Ready;
    private bool character2Ready;
    private string selectedLugarScene1 = string.Empty;
    private string selectedLugarScene2 = string.Empty;
    private CharacterSelection decidedCharacter1;
    private CharacterSelection decidedCharacter2;
    private string decidedLugarScene1 = string.Empty;
    private string decidedLugarScene2 = string.Empty;
    private bool hasDecidedAssets;
    private readonly List<string> sceneFinalPhrases = new List<string>();

    private void Awake()
    {
        EnsureDefaultScenes();
        EnsureScenePhraseStorage();
        currentSceneIndex = 0;
        ApplyScene(currentSceneIndex);

        if (decideAssetsButton != null)
        {
            decideAssetsButton.onClick.AddListener(OnDecideAssetsPressed);
        }

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
        if (decideAssetsButton != null)
        {
            decideAssetsButton.onClick.RemoveListener(OnDecideAssetsPressed);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinuePressed);
        }
    }

    public void OnDecideAssetsPressed()
    {
        ConfirmDecidedAssetsFromCurrentScene();
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
            if (i > 0)
            {
                segment = segment.TrimStart();
            }
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

    public bool TryPlaceWordInFirstCompatibleSlot(KeyWord word)
    {
        if (word == null)
        {
            return false;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            WordSlot slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            if (slot.RequiredType != word.Type && !IsActionTypePair(slot.RequiredType, word.Type))
            {
                continue;
            }

            if (slot.CurrentWord == null)
            {
                if (slot.TryPlaceWord(word))
                {
                    return true;
                }
            }
            else if (slot.TryReplaceWord(word))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsActionTypePair(WordType requiredType, WordType incomingType)
    {
        bool incomingIsAction = incomingType == WordType.Accion
            || incomingType == WordType.AccionP1
            || incomingType == WordType.AccionP2;

        bool requiredIsAction = requiredType == WordType.Accion
            || requiredType == WordType.AccionP1
            || requiredType == WordType.AccionP2;

        return incomingIsAction && requiredIsAction;
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

        return PostProcessFinalPhrase(builder.ToString());
    }

    public bool TryGetCharacterDescriptor(int characterNumber, out string tipo1, out string personaje)
    {
        if (!hasDecidedAssets)
        {
            tipo1 = string.Empty;
            personaje = string.Empty;
            return false;
        }

        CharacterSelection selection;

        if (characterNumber == 1)
        {
            selection = decidedCharacter1;
        }
        else if (characterNumber == 2)
        {
            selection = decidedCharacter2;
        }
        else
        {
            tipo1 = string.Empty;
            personaje = string.Empty;
            return false;
        }

        tipo1 = selection.tipo1;
        personaje = selection.personaje;
        return !string.IsNullOrWhiteSpace(tipo1) && !string.IsNullOrWhiteSpace(personaje);
    }

    public bool TryGetSceneryPlace(int sceneNumber, out string place)
    {
        place = string.Empty;

        if (!hasDecidedAssets)
        {
            return false;
        }

        if (sceneNumber == 1)
        {
            if (!string.IsNullOrWhiteSpace(decidedLugarScene1))
            {
                place = decidedLugarScene1;
                return true;
            }

            return false;
        }

        if (sceneNumber == 2)
        {
            if (!string.IsNullOrWhiteSpace(decidedLugarScene2))
            {
                place = decidedLugarScene2;
                return true;
            }

            return false;
        }

        return false;
    }

    public bool TryGetSceneryPlaceOption(int sceneNumber, out PlaceOption placeOption)
    {
        placeOption = PlaceOption.None;

        string placeText;
        if (!TryGetSceneryPlace(sceneNumber, out placeText))
        {
            return false;
        }

        return TryParsePlaceOption(placeText, out placeOption) && placeOption != PlaceOption.None;
    }

    public bool TryGetSceneFinalPhrase(int sceneNumber, out string phrase)
    {
        phrase = string.Empty;

        int sceneIndex = sceneNumber - 1;
        if (sceneIndex < 0 || sceneIndex >= sceneFinalPhrases.Count)
        {
            return false;
        }

        string storedPhrase = sceneFinalPhrases[sceneIndex];
        if (string.IsNullOrWhiteSpace(storedPhrase))
        {
            return false;
        }

        phrase = storedPhrase;
        return true;
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
        StoreFinalPhraseForScene(currentSceneIndex, finalResult);

        if (currentSceneIndex == 0)
        {
            CaptureSelectionsFromSceneOne();
        }

        if (currentSceneIndex == 1)
        {
            CaptureSelectionsFromSceneTwo();
        }

        if (currentSceneIndex == 3)
        {
            selectedLugarScene2 = NormalizeWordValue(GetSlotValue(0, string.Empty));
        }

        if (currentSceneIndex >= scenes.Count - 1)
        {
            if (guionPanel != null && guionPanelTransition != null)
            {
                guionPanelTransition.TransitionOut();
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
        float cursorX = Mathf.Max(0f, startOffsetX);
        float cursorY = -startOffsetY;
        float currentLineHeight = 0f;
        bool hasItemsInLine = false;
        bool previousWasSlot = false;

        for (int i = 0; i < containerRect.childCount; i++)
        {
            RectTransform child = containerRect.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf)
            {
                continue;
            }

            float width = GetSafePreferredWidth(child);
            float height = GetSafePreferredHeight(child);

            bool wouldOverflow = cursorX + width > maxX;
            if (wouldOverflow && hasItemsInLine)
            {
                float lineStep = Mathf.Max(minLineBreakStep, currentLineHeight + lineSpacing);
                cursorX = Mathf.Max(0f, startOffsetX);
                cursorY -= lineStep;
                currentLineHeight = 0f;
                hasItemsInLine = false;
            }

            float extraOffsetY = 0f;
            bool isText = child.GetComponent<TMP_Text>() != null;
            bool isSlot = !isText && child.GetComponent<WordSlot>() != null;
            if (isText)
            {
                extraOffsetY = textVerticalOffset;
            }
            else if (isSlot)
            {
                extraOffsetY = slotVerticalOffset;
            }

            if (previousWasSlot && isText)
            {
                cursorX += Mathf.Max(0f, postSlotSpacing);
            }

            child.anchorMin = new Vector2(0f, 1f);
            child.anchorMax = new Vector2(0f, 1f);
            child.pivot = new Vector2(0f, 1f);
            child.anchoredPosition = new Vector2(cursorX, cursorY + extraOffsetY);

            cursorX += width + horizontalSpacing;
            currentLineHeight = Mathf.Max(currentLineHeight, height);
            hasItemsInLine = true;
            previousWasSlot = isSlot;
        }
    }

    private static float GetSafePreferredWidth(RectTransform child)
    {
        if (child == null)
        {
            return 0f;
        }

        WordSlot slot = child.GetComponent<WordSlot>();
        if (slot != null)
        {
            float slotWidth = slot.GetVisualWidth();
            if (slotWidth > 0f)
            {
                return slotWidth;
            }
        }

        float width = LayoutUtility.GetPreferredWidth(child);
        if (width > 0f)
        {
            return width;
        }

        return Mathf.Max(1f, child.rect.width);
    }

    private static float GetSafePreferredHeight(RectTransform child)
    {
        if (child == null)
        {
            return 0f;
        }

        float height = LayoutUtility.GetPreferredHeight(child);
        if (height <= 0f)
        {
            height = child.rect.height;
        }

        TMP_Text tmp = child.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            height = Mathf.Max(height, tmp.preferredHeight);
        }

        return Mathf.Max(1f, height);
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

        float targetWidth = textBlock.preferredWidth;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);

        LayoutElement layoutElement = textBlock.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.preferredWidth = targetWidth;
            layoutElement.minWidth = targetWidth;
        }
    }

    private void EnsureDefaultScenes()
    {
        if (scenes != null && scenes.Count > 0 && !LooksLikeLegacyDefaultScenes(scenes))
        {
            return;
        }

        scenes = new List<SceneDefinition>
        {
            new SceneDefinition
            {
                name = "Escena 1",
                template = "Un día, _ estaba caminando por _.",
                slots = new List<WordType>
                {
                    WordType.Sujeto,
                    WordType.Lugar,
                },
                sujetos = new List<string> { "David", "Alex", "Andrés", "María", "Ana", "Paula"},
                lugares = new List<string> { "el parque", "el colegio", "la tienda" }
            },
            new SceneDefinition
            {
                name = "Escena 2",
                template = "Cuando de repente _ llega y le _ a {C1_T1}.",
                slots = new List<WordType>
                {
                    WordType.Sujeto,
                    WordType.Accion,
                },
                sujetos = new List<string> { "David", "Alex", "Andrés", "María", "Ana", "Paula"},
                acciones = new List<string> { "pega", "roba", "grita" }
            },
            new SceneDefinition
            {
                name = "Escena 3",
                template = "{C1_MOLESTO} por lo ocurrido, {C1_T1} decide _ a {C2_T1}.",
                slots = new List<WordType> { WordType.Accion },
                acciones = new List<string> { "gritarle", "perseguir", "golpear", "hablarle" }
            },
            new SceneDefinition
            {
                name = "Escena 4",
                template = "Después del conflicto, {C2_T1} decide irse a _.",
                slots = new List<WordType> { WordType.Lugar },
                lugares = new List<string> { "el parque", "el colegio", "la tienda" }
            },
            new SceneDefinition
            {
                name = "Escena 5",
                template = "Tiempo después, {C2_T1} estaba _ en {L2}, cuando se reencuentra con {C1_T1}.",
                slots = new List<WordType> { WordType.Accion },
                acciones = new List<string> { "bailando", "cantando", "hablando", "saltando" }
            },
            new SceneDefinition
            {
                name = "Escena 6",
                template = "Al final, {C2_T1} y {C1_T1} terminan _ el uno al otro.",
                slots = new List<WordType> { WordType.Accion },
                acciones = new List<string> { "gritando", "golpeando", "hablando", "besando" }
            }
        };
    }

    private static bool LooksLikeLegacyDefaultScenes(List<SceneDefinition> existingScenes)
    {
        if (existingScenes == null || existingScenes.Count != 6)
        {
            return false;
        }

        string s1 = existingScenes[0].template;
        string s2 = existingScenes[1].template;

        if (string.IsNullOrWhiteSpace(s1) || string.IsNullOrWhiteSpace(s2))
        {
            return false;
        }

        // Heuristic: previous hardcoded defaults started with these fragments.
        return s1.Contains("Un dia") && s2.Contains("De pronto");
    }

    private void EnsureScenePhraseStorage()
    {
        int requiredCount = scenes != null ? scenes.Count : 0;
        if (requiredCount <= 0)
        {
            sceneFinalPhrases.Clear();
            return;
        }

        while (sceneFinalPhrases.Count < requiredCount)
        {
            sceneFinalPhrases.Add(string.Empty);
        }

        if (sceneFinalPhrases.Count > requiredCount)
        {
            sceneFinalPhrases.RemoveRange(requiredCount, sceneFinalPhrases.Count - requiredCount);
        }
    }

    private void StoreFinalPhraseForScene(int sceneIndex, string phrase)
    {
        EnsureScenePhraseStorage();

        if (sceneIndex < 0 || sceneIndex >= sceneFinalPhrases.Count)
        {
            return;
        }

        sceneFinalPhrases[sceneIndex] = string.IsNullOrWhiteSpace(phrase)
            ? string.Empty
            : phrase.Trim();
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
        SetSceneNumberText(clampedIndex + 1);

        Debug.Log($"Escena cargada: {scene.name}");
    }

    private void SetSceneNumberText(int sceneNumber)
    {
        if (sceneNumberText == null)
        {
            return;
        }

        sceneNumberText.text = string.Format("Escena {0}", sceneNumber);
    }

    private List<KeyWordManager.KeyWordEntry> CreateEntriesForScene(SceneDefinition scene)
    {
        List<KeyWordManager.KeyWordEntry> entries = new List<KeyWordManager.KeyWordEntry>();
        Dictionary<WordType, int> typeUsageCount = CountTypeUsage(scene.slots);

        AddEntriesRepeated(entries, scene.pronombres, WordType.Pronombre, GetUsageCount(typeUsageCount, WordType.Pronombre));
        AddEntriesRepeated(entries, GetFilteredSubjects(scene), WordType.Sujeto, GetUsageCount(typeUsageCount, WordType.Sujeto));
        AddEntriesRepeated(entries, scene.lugares, WordType.Lugar, GetUsageCount(typeUsageCount, WordType.Lugar));
        AddEntriesRepeated(entries, scene.acciones, WordType.Accion, GetUsageCount(typeUsageCount, WordType.Accion));
        AddEntriesRepeated(entries, scene.accionesP1, WordType.AccionP1, GetUsageCount(typeUsageCount, WordType.AccionP1));
        AddEntriesRepeated(entries, scene.accionesP2, WordType.AccionP2, GetUsageCount(typeUsageCount, WordType.AccionP2));

        return entries;
    }

    private List<string> GetFilteredSubjects(SceneDefinition scene)
    {
        if (scene == null || scene.sujetos == null)
        {
            return scene != null ? scene.sujetos : null;
        }

        if (currentSceneIndex != 1)
        {
            return scene.sujetos;
        }

        string selectedName = character1Ready ? character1.personaje : string.Empty;
        if (string.IsNullOrWhiteSpace(selectedName))
        {
            return scene.sujetos;
        }

        List<string> filtered = new List<string>();
        foreach (string sujeto in scene.sujetos)
        {
            if (string.IsNullOrWhiteSpace(sujeto))
            {
                continue;
            }

            if (string.Equals(sujeto.Trim(), selectedName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            filtered.Add(sujeto);
        }

        return filtered;
    }

    private void CaptureSelectionsFromSceneOne()
    {
        if (slots.Count < 2)
        {
            return;
        }

        string c1Personaje = GetSlotValue(0, "Chris");
        selectedLugarScene1 = NormalizeWordValue(GetSlotValue(1, string.Empty));

        character1 = BuildCharacterSelection(c1Personaje);
        character1Ready = true;
    }

    private void CaptureSelectionsFromSceneTwo()
    {
        if (slots.Count < 2)
        {
            return;
        }

        string c2Personaje = GetSlotValue(0, "Chris");

        character2 = BuildCharacterSelection(c2Personaje);
        character2Ready = true;
    }

    private bool TryGetLiveCharacterSelectionFromCurrentScene(int characterNumber, out CharacterSelection selection)
    {
        selection = default;

        if (characterNumber == 1)
        {
            if (currentSceneIndex != 0 || slots.Count < 1)
            {
                return false;
            }

            string personaje;
            if (!TryGetSlotSelectedValue(0, out personaje))
            {
                return false;
            }

            selection = BuildCharacterSelection(personaje);
            return true;
        }

        if (characterNumber == 2)
        {
            if (currentSceneIndex != 1 || slots.Count < 1)
            {
                return false;
            }

            string personaje;
            if (!TryGetSlotSelectedValue(0, out personaje))
            {
                return false;
            }

            selection = BuildCharacterSelection(personaje);
            return true;
        }

        return false;
    }

    private bool TryGetSlotSelectedValue(int index, out string value)
    {
        value = string.Empty;

        if (index < 0 || index >= slots.Count)
        {
            return false;
        }

        KeyWord word = slots[index].CurrentWord;
        if (word == null || string.IsNullOrWhiteSpace(word.WordText))
        {
            return false;
        }

        value = word.WordText.Trim();
        return true;
    }

    private void ConfirmDecidedAssetsFromCurrentScene()
    {
        CharacterSelection sourceCharacter1;
        CharacterSelection sourceCharacter2;
        string sourceLugar1 = string.Empty;
        string sourceLugar2 = string.Empty;

        if (character1Ready)
        {
            sourceCharacter1 = character1;
        }
        else
        {
            if (!TryGetLiveCharacterSelectionFromCurrentScene(1, out sourceCharacter1))
            {
                Debug.LogWarning("Decide Assets requiere una seleccion valida para el personaje 1.");
                return;
            }
        }

        if (character2Ready)
        {
            sourceCharacter2 = character2;
        }
        else
        {
            if (!TryGetLiveCharacterSelectionFromCurrentScene(2, out sourceCharacter2))
            {
                Debug.LogWarning("Decide Assets requiere una seleccion valida para el personaje 2 (se decide en la Escena 2).");
                return;
            }
        }

        sourceLugar1 = NormalizeWordValue(selectedLugarScene1);
        sourceLugar2 = NormalizeWordValue(selectedLugarScene2);

        if (string.IsNullOrWhiteSpace(sourceLugar1))
        {
            GetFirstTwoSelectedLugares(out sourceLugar1, out sourceLugar2);
        }

        if (string.IsNullOrWhiteSpace(sourceLugar1))
        {
            sourceLugar1 = NormalizeWordValue(selectedLugarScene1);
        }

        if (string.IsNullOrWhiteSpace(sourceLugar2))
        {
            sourceLugar2 = NormalizeWordValue(selectedLugarScene2);
        }

        if (string.IsNullOrWhiteSpace(sourceLugar1))
        {
            Debug.LogWarning("Decide Assets requiere al menos un lugar seleccionado.");
            return;
        }

        if (string.IsNullOrWhiteSpace(sourceLugar2))
        {
            sourceLugar2 = sourceLugar1;
        }

        decidedCharacter1 = sourceCharacter1;
        decidedCharacter2 = sourceCharacter2;
        decidedLugarScene1 = sourceLugar1;
        decidedLugarScene2 = sourceLugar2;
        hasDecidedAssets = true;
    }

    private void GetFirstTwoSelectedLugares(out string firstLugar, out string secondLugar)
    {
        firstLugar = string.Empty;
        secondLugar = string.Empty;

        int max = Mathf.Min(slotTypes.Count, slots.Count);
        for (int i = 0; i < max; i++)
        {
            if (slotTypes[i] != WordType.Lugar)
            {
                continue;
            }

            string selectedValue;
            if (!TryGetSlotSelectedValue(i, out selectedValue))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(firstLugar))
            {
                firstLugar = selectedValue;
            }
            else
            {
                secondLugar = selectedValue;
                return;
            }
        }
    }

    private string ResolvePersistentTokens(string template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        string resolved = template;

        if (character1Ready)
        {
            resolved = resolved
                .Replace("{C1_T1}", character1.tipo1)
                .Replace("{C1_T2}", character1.tipo2)
                .Replace("{C1_T2C}", character1.tipo2Capital)
                .Replace("{C1_T3}", character1.tipo3)
                .Replace("{C1_P}", character1.personaje);
        }

        resolved = resolved.Replace("{C1_MOLESTO}", GetCharacter1MolestoToken());

        if (character2Ready)
        {
            resolved = resolved
                .Replace("{C2_T1}", character2.tipo1)
                .Replace("{C2_T2}", character2.tipo2)
                .Replace("{C2_T2C}", character2.tipo2Capital)
                .Replace("{C2_T3}", character2.tipo3)
                .Replace("{C2_P}", character2.personaje);
        }

        if (!string.IsNullOrWhiteSpace(selectedLugarScene1))
        {
            resolved = resolved.Replace("{L1}", selectedLugarScene1);
        }

        if (!string.IsNullOrWhiteSpace(selectedLugarScene2))
        {
            resolved = resolved.Replace("{L2}", selectedLugarScene2);
        }

        return resolved;
    }

    private string GetCharacter1MolestoToken()
    {
        string name = character1Ready ? character1.personaje : string.Empty;
        if (IsFemaleName(name))
        {
            return "Molesta";
        }

        return "Molesto";
    }

    private static bool IsFemaleName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        string normalized = NormalizeNameForGender(name);
        return normalized == "maria" || normalized == "ana" || normalized == "paula";
    }

    private static string NormalizeNameForGender(string value)
    {
        string normalized = value.Trim().ToLowerInvariant();
        return normalized
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u");
    }

    private static CharacterSelection BuildCharacterSelection(string personaje)
    {
        string normalizedName = string.IsNullOrWhiteSpace(personaje) ? "Chris" : personaje.Trim();

        return new CharacterSelection
        {
            tipo1 = normalizedName,
            tipo2 = string.Empty,
            tipo2Capital = string.Empty,
            tipo3 = string.Empty,
            personaje = normalizedName
        };
    }

    private static string PostProcessFinalPhrase(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return string.Empty;
        }

        string result = phrase;

        // Spanish contractions.
        result = result.Replace(" a el ", " al ");
        result = result.Replace(" A el ", " Al ");
        result = result.Replace(" de el ", " del ");
        result = result.Replace(" De el ", " Del ");

        return result;
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

    private static string NormalizeWordValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim();
    }

    private static bool TryParsePlaceOption(string value, out PlaceOption option)
    {
        option = PlaceOption.None;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim().ToLowerInvariant();
        normalized = normalized
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u");

        if (normalized.Contains("parque"))
        {
            option = PlaceOption.Parque;
            return true;
        }

        if (normalized.Contains("colegio") || normalized.Contains("escuela"))
        {
            option = PlaceOption.Colegio;
            return true;
        }

        if (normalized.Contains("teatro"))
        {
            option = PlaceOption.Teatro;
            return true;
        }

        if (normalized.Contains("calle"))
        {
            option = PlaceOption.Calle;
            return true;
        }

        if (normalized.Contains("casa"))
        {
            option = PlaceOption.Casa;
            return true;
        }

        if (normalized.Contains("hospital"))
        {
            option = PlaceOption.Hospital;
            return true;
        }

        if (normalized.Contains("estacion"))
        {
            option = PlaceOption.Estacion;
            return true;
        }

        if (normalized.Contains("banco"))
        {
            option = PlaceOption.Banco;
            return true;
        }

        if (normalized.Contains("oficina"))
        {
            option = PlaceOption.Oficina;
            return true;
        }

        if (normalized.Contains("tienda"))
        {
            option = PlaceOption.Tienda;
            return true;
        }

        return false;
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
