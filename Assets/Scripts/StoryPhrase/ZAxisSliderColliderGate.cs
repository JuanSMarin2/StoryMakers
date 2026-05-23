using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Scrollbar))]
public class ZAxisSliderColliderGate : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private StoryBoardManager storyBoardManager;
    [SerializeField] private CharacterSetup characterSetup;
    [SerializeField] private int characterNumber = 1;

    private void Awake()
    {
        if (storyBoardManager == null)
        {
            storyBoardManager = FindObjectOfType<StoryBoardManager>();
        }

        if (characterSetup == null && storyBoardManager != null)
        {
            characterSetup = storyBoardManager.GetComponent<CharacterSetup>();
        }

        if (characterSetup == null)
        {
            characterSetup = FindObjectOfType<CharacterSetup>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetCollidersEnabled(false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetCollidersEnabled(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        SetCollidersEnabled(false);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SetCollidersEnabled(true);
    }

    private void OnDisable()
    {
        SetCollidersEnabled(true);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (characterSetup == null || storyBoardManager == null)
        {
            return;
        }

        int copyIndex = storyBoardManager.CurrentSceneNumber - 1;
        if (copyIndex < 0)
        {
            return;
        }

        characterSetup.SetCharacterCollidersEnabled(copyIndex, characterNumber, enabled);
    }
}
