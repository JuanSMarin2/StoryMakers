using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WordSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image slotImage;

    public WordType RequiredType { get; private set; }
    public KeyWord CurrentWord { get; private set; }

    private PhraseManager phraseManager;

    public void Initialize(WordType requiredType, PhraseManager manager, Color slotColor)
    {
        RequiredType = requiredType;
        phraseManager = manager;

        if (slotImage != null)
        {
            slotImage.color = slotColor;
        }
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

        if (notify)
        {
            phraseManager.NotifySlotStateChanged();
        }
    }
}
