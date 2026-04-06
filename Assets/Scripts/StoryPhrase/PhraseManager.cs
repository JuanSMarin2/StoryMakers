using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhraseManager : MonoBehaviour
{
    [System.Serializable]
    private class PremiseDefinition
    {
        public string name;
        [TextArea]
        public string template;
        public List<WordType> slots = new List<WordType>();
        public List<string> sujetos = new List<string>();
        public List<string> lugares = new List<string>();
        public List<string> acciones = new List<string>();
    }

    [Header("Phrase Source")]
    [TextArea]
    [SerializeField] private string phraseTemplate = "Habia una vez un _ que vivia en _ y buscaba _.";
    [SerializeField] private List<WordType> slotTypes = new List<WordType>();
    [SerializeField] private bool useRandomPremise = true;

    [Header("Automatic Premises")]
    [SerializeField] private List<PremiseDefinition> premises = new List<PremiseDefinition>();

    [Header("UI Prefabs")]
    [SerializeField] private Transform phraseContainer;
    [SerializeField] private TMP_Text textBlockPrefab;
    [SerializeField] private WordSlot slotPrefab;

    [Header("Wrapped Layout")]
    [SerializeField] private bool wrapInsideScreen = true;
    [SerializeField] private float startOffsetX = 16f;
    [SerializeField] private float startOffsetY = 16f;
    [SerializeField] private float rightPadding = 16f;
    [SerializeField] private float horizontalSpacing = 8f;
    [SerializeField] private float minLineBreakStep = 70f;
    [SerializeField] private float lineSpacing = 8f;

    [Header("Dependencies")]
    [SerializeField] private KeyWordManager keyWordManager;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text finalPhraseOutputText;

    private readonly List<WordSlot> slots = new List<WordSlot>();
    private string[] parsedSegments;
    private bool isCompleted;

    private void Awake()
    {
        EnsureDefaultPremises();

        if (useRandomPremise)
        {
            ApplyRandomPremise();
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
        bool allFilled = slots.Count > 0;

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
        for (int i = phraseContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(phraseContainer.GetChild(i).gameObject);
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

        Debug.Log($"Frase final: {finalResult}");
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
        float cursorX = startOffsetX;
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
            if (wouldOverflow && hasItemsInLine)
            {
                float lineStep = Mathf.Max(minLineBreakStep, currentLineHeight + lineSpacing);
                cursorX = startOffsetX;
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

    private void EnsureDefaultPremises()
    {
        if (premises != null && premises.Count > 0)
        {
            return;
        }

        premises = new List<PremiseDefinition>
        {
            new PremiseDefinition
            {
                name = "Premisa #1",
                template = "Un dia, _ andaba por _, cuando _ llego para _ a _, logrando que _ decida _",
                slots = new List<WordType>
                {
                    WordType.Sujeto,
                    WordType.Lugar,
                    WordType.Sujeto,
                    WordType.Accion,
                    WordType.Sujeto,
                    WordType.Sujeto,
                    WordType.Accion
                },
                sujetos = new List<string> { "Policia", "Deportista", "Estudiante", "Policia", "Deportista", "Estudiante" },
                lugares = new List<string> { "Ciudad", "Parque" },
                acciones = new List<string> { "Pegar", "Saludar", "Besar" }
            },
            new PremiseDefinition
            {
                name = "Idea Premisa #2",
                template = "A medianoche, dos _ estaban por _ en _, cuando unos _ aparecieron, obligandolos a _",
                slots = new List<WordType>
                {
                    WordType.Sujeto,
                    WordType.Accion,
                    WordType.Lugar,
                    WordType.Sujeto,
                    WordType.Accion,
                    WordType.Accion
                },
                sujetos = new List<string> { "Ladrones", "Policias", "Amigos" },
                lugares = new List<string> { "Banco", "Bar" },
                acciones = new List<string> { "Robar", "Bailar", "Cantar" }
            },
            new PremiseDefinition
            {
                name = "Idea Premisa #3",
                template = "Una _ estaba por _ en _, pero _ comenzo a _ forzandolos a _",
                slots = new List<WordType>
                {
                    WordType.Sujeto,
                    WordType.Accion,
                    WordType.Lugar,
                    WordType.Sujeto,
                    WordType.Accion,
                    WordType.Accion
                },
                sujetos = new List<string> { "Actriz", "Deportista", "Maestra" },
                lugares = new List<string> { "Parque", "Teatro", "Salon" },
                acciones = new List<string> { "Bailar", "Huir", "Cantar", "Correr" }
            }
        };
    }

    private void ApplyRandomPremise()
    {
        if (premises == null || premises.Count == 0)
        {
            return;
        }

        PremiseDefinition selectedPremise = premises[Random.Range(0, premises.Count)];

        phraseTemplate = selectedPremise.template;
        slotTypes = new List<WordType>(selectedPremise.slots);

        if (keyWordManager != null)
        {
            keyWordManager.SetWords(CreateEntries(selectedPremise));
        }

        Debug.Log($"Premisa seleccionada: {selectedPremise.name}");
    }

    private static List<KeyWordManager.KeyWordEntry> CreateEntries(PremiseDefinition premise)
    {
        List<KeyWordManager.KeyWordEntry> entries = new List<KeyWordManager.KeyWordEntry>();

        AddEntries(entries, premise.sujetos, WordType.Sujeto);
        AddEntries(entries, premise.lugares, WordType.Lugar);
        AddEntries(entries, premise.acciones, WordType.Accion);

        return entries;
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
