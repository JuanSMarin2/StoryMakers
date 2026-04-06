using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WordSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image slotImage;
    [SerializeField] private float minSlotWidth = 120f;
    [SerializeField] private float extraWidthWhenFilled = 16f;

    public WordType RequiredType { get; private set; }
    public KeyWord CurrentWord { get; private set; }

    private PhraseManager phraseManager;
    private LayoutElement layoutElement;
    private RectTransform rectTransform;
    private float defaultPreferredWidth;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        layoutElement = GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        float currentWidth = rectTransform != null ? rectTransform.rect.width : minSlotWidth;
        float layoutWidth = layoutElement.preferredWidth > 0f ? layoutElement.preferredWidth : currentWidth;
        defaultPreferredWidth = Mathf.Max(minSlotWidth, layoutWidth);
        layoutElement.preferredWidth = defaultPreferredWidth;
    }

    public void Initialize(WordType requiredType, PhraseManager manager, Color slotColor)
    {
        RequiredType = requiredType;
        phraseManager = manager;

        if (slotImage != null)
        {
            slotImage.color = slotColor;
        }

        SetWidth(defaultPreferredWidth);
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null)
        {
            return;
        }

        KeyWord incomingWord = droppedObject.GetComponent<KeyWord>();
        if (incomingWord == null)
        {
            return;
        }

        if (incomingWord.Type != RequiredType)
        {
            incomingWord.ReturnToOriginSmooth();
            return;
        }

        if (CurrentWord != null && CurrentWord != incomingWord)
        {
            KeyWord displaced = CurrentWord;
            CurrentWord = null;
            displaced.ReturnToOriginSmooth();
        }

        CurrentWord = incomingWord;
        UpdateWidthForWord(incomingWord);
        incomingWord.SnapIntoSlot(this);
        phraseManager.NotifySlotStateChanged();
    }

    public void RemoveWord(KeyWord word, bool notify)
    {
        if (CurrentWord != word)
        {
            return;
        }

        CurrentWord = null;
        SetWidth(defaultPreferredWidth);

        if (notify)
        {
            phraseManager.NotifySlotStateChanged();
        }
    }

    private void UpdateWidthForWord(KeyWord word)
    {
        if (word == null)
        {
            SetWidth(defaultPreferredWidth);
            return;
        }

        float target = Mathf.Max(defaultPreferredWidth, word.GetPreferredWidth() + extraWidthWhenFilled);
        SetWidth(target);
    }

    private void SetWidth(float width)
    {
        if (layoutElement != null)
        {
            layoutElement.preferredWidth = width;
        }

        if (rectTransform != null)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }
    }
}
