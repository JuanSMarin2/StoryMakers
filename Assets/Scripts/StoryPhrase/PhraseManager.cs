using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhraseManager : MonoBehaviour
{
    [Header("Phrase Source")]
    [TextArea]
    [SerializeField] private string phraseTemplate = "Habia una vez un _ que vivia en _ y buscaba _.";
    [SerializeField] private List<WordType> slotTypes = new List<WordType>();

    [Header("UI Prefabs")]
    [SerializeField] private Transform phraseContainer;
    [SerializeField] private TMP_Text textBlockPrefab;
    [SerializeField] private WordSlot slotPrefab;

    [Header("Dependencies")]
    [SerializeField] private KeyWordManager keyWordManager;
    [SerializeField] private Button continueButton;

    private readonly List<WordSlot> slots = new List<WordSlot>();
    private string[] parsedSegments;
    private bool isCompleted;

    private void Awake()
    {
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
        Debug.Log($"Frase final: {finalResult}");
    }
}
