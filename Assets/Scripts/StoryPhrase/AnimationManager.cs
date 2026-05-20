using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class AnimationManager : MonoBehaviour
{
    [SerializeField] private List<Animator> animCharacter1 = new List<Animator>();

    [SerializeField] private List<Animator> animCharacter2 = new List<Animator>();

    [SerializeField] private string[] animationTriggers;

    [Header("Results Storage")]
    [SerializeField] private int totalScenes = 6;
    [SerializeField] private string defaultTriggerName = "Default";
    private readonly List<string> lastTriggerCharacter1 = new List<string>();
    private readonly List<string> lastTriggerCharacter2 = new List<string>();

    [SerializeField] private StoryBoardManager storyBoardManager;
    [Header("UI")]
    [SerializeField] private Transform chooseAnimationsCharacter1;
    [SerializeField] private Transform chooseAnimationsCharacter2;
    [SerializeField] private Button animationButtonPrefab;
    [SerializeField] private Vector2 buttonSpacing = new Vector2(10f, 10f);
    private int _currentAnimationIndex = 0;
    private int _currentSceneIndex = 0; //Determina el animator a usar entre 0 y 5
   
    private void Awake()
    {
        EnsureTriggerStorageSize();
    }

    void Start()
    {
        _currentAnimationIndex = 0;
        BuildAnimationButtons();
   
    }

    void Update(){
     _currentSceneIndex = storyBoardManager.CurrentSceneNumber -1; 
    }

 
    public void TriggerAnimation(string triggerName, bool character1)
    {
        Debug.Log($"Triggering animation: {triggerName} for {(character1 ? "Character 1" : "Character 2")} at scene index {_currentSceneIndex}");
        if (character1)
        {
            animCharacter1[_currentSceneIndex].SetTrigger(triggerName);
        }
        else
        {
            animCharacter2[_currentSceneIndex].SetTrigger(triggerName);
        }

        SetLastTriggerForScene(_currentSceneIndex, character1, triggerName);
    }

    // Register an Animator for a copy at a specific index for character1 or character2.
    public void SetAnimatorAt(int index, Animator animator, bool character1)
    {
        List<Animator> target = character1 ? animCharacter1 : animCharacter2;
        if (target == null)
        {
            return;
        }

        // Ensure list has enough capacity
        while (target.Count <= index)
        {
            target.Add(null);
        }

        target[index] = animator;
    }

    public Animator GetAnimatorAt(int index, bool character1)
    {
        List<Animator> target = character1 ? animCharacter1 : animCharacter2;
        if (target == null || index < 0 || index >= target.Count)
        {
            return null;
        }

        return target[index];
    }

    public string GetLastTriggerForScene(int sceneIndex, bool character1)
    {
        EnsureTriggerStorageSize();
        List<string> target = character1 ? lastTriggerCharacter1 : lastTriggerCharacter2;
        if (target == null || sceneIndex < 0 || sceneIndex >= target.Count)
        {
            return defaultTriggerName;
        }

        string stored = target[sceneIndex];
        return string.IsNullOrWhiteSpace(stored) ? defaultTriggerName : stored;
    }

    public void NextAnimation( bool character1)
    {
        if(_currentAnimationIndex < animationTriggers.Length)
        {
            TriggerAnimation(animationTriggers[_currentAnimationIndex], character1);
            _currentAnimationIndex++;
        }
        else
        {
            _currentAnimationIndex = 0; 
            if (character1)
                animCharacter1[_currentSceneIndex].SetTrigger("Default");
            else
            animCharacter2[_currentSceneIndex].SetTrigger("Default");

        }

}

    private void BuildAnimationButtons()
    {
        if (animationButtonPrefab == null)
        {
            Debug.LogWarning("AnimationManager: falta asignar animationButtonPrefab.");
            return;
        }

        BuildButtonsForPanel(chooseAnimationsCharacter1, true);
        BuildButtonsForPanel(chooseAnimationsCharacter2, false);
    }

    private void BuildButtonsForPanel(Transform panel, bool character1)
    {
        if (panel == null)
        {
            Debug.LogWarning("AnimationManager: panel de animaciones no asignado.");
            return;
        }

        EnsureGridLayout(panel.gameObject, animationButtonPrefab);
        ClearPanelChildren(panel);

        if (animationTriggers == null)
        {
            return;
        }

        for (int i = 0; i < animationTriggers.Length; i++)
        {
            string triggerName = animationTriggers[i];
            Button button = Instantiate(animationButtonPrefab, panel);
            if (button == null)
            {
                continue;
            }

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = triggerName;
            }

            button.onClick.AddListener(() => TriggerAnimationForCurrentScene(triggerName, character1, panel));
        }
    }

    private void TriggerAnimationForCurrentScene(string triggerName, bool character1, Transform panel)
    {
        Animator animator = GetAnimatorForCurrentScene(character1);
        if (animator == null)
        {
            return;
        }

        animator.SetTrigger(triggerName);
        SetLastTriggerForScene(_currentSceneIndex, character1, triggerName);
        if (panel != null)
        {
            panel.gameObject.SetActive(false);
        }
    }

    private Animator GetAnimatorForCurrentScene(bool character1)
    {
        List<Animator> animators = character1 ? animCharacter1 : animCharacter2;
        if (animators == null)
        {
            return null;
        }

        int index = _currentSceneIndex;
        if (index < 0 || index >= animators.Count)
        {
            return null;
        }

        return animators[index];
    }

    private static void ClearPanelChildren(Transform panel)
    {
        for (int i = panel.childCount - 1; i >= 0; i--)
        {
            Transform child = panel.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void EnsureGridLayout(GameObject panel, Button buttonPrefab)
    {
        GridLayoutGroup grid = panel.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = panel.AddComponent<GridLayoutGroup>();
        }

        grid.constraint = GridLayoutGroup.Constraint.Flexible;
        grid.spacing = buttonSpacing;
        grid.startAxis = GridLayoutGroup.Axis.Vertical;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;

        if (buttonPrefab != null)
        {
            RectTransform rect = buttonPrefab.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector2 size = rect.rect.size;
                if (size.x <= 0f || size.y <= 0f)
                {
                    size = rect.sizeDelta;
                }

                if (size.x > 0f && size.y > 0f)
                {
                    grid.cellSize = size;
                }
            }
        }
    }

    public void ShowCharacter1Panel()
    {
        SetPanelActive(chooseAnimationsCharacter1, true);
    }

    public void ShowCharacter2Panel()
    {
        SetPanelActive(chooseAnimationsCharacter2, true);
    }

    private static void SetPanelActive(Transform panel, bool active)
    {
        if (panel != null)
        {
            panel.gameObject.SetActive(active);
        }
    }

    private void EnsureTriggerStorageSize()
    {
        int scenes = Mathf.Max(0, totalScenes);

        while (lastTriggerCharacter1.Count < scenes)
        {
            lastTriggerCharacter1.Add(defaultTriggerName);
        }

        while (lastTriggerCharacter2.Count < scenes)
        {
            lastTriggerCharacter2.Add(defaultTriggerName);
        }
    }

    private void SetLastTriggerForScene(int sceneIndex, bool character1, string triggerName)
    {
        EnsureTriggerStorageSize();

        List<string> target = character1 ? lastTriggerCharacter1 : lastTriggerCharacter2;
        if (target == null || sceneIndex < 0 || sceneIndex >= target.Count)
        {
            return;
        }

        target[sceneIndex] = string.IsNullOrWhiteSpace(triggerName) ? defaultTriggerName : triggerName;
    }
}
