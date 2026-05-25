using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StoryBoardManager : MonoBehaviour
{
    [Header("Stage Roots")]
    [SerializeField] private GameObject sceneryStageRoot;
    [SerializeField] private List<GameObject> sceneryUiObjectsToDisable = new List<GameObject>();
    [SerializeField] private GameObject storyBoardStageRoot;
    [SerializeField] private UiPanelTransition storyBoardStageTransition;
    [SerializeField] private GameObject guionPanel;

    [Header("Dependencies")]
    [SerializeField] private SceneryManager sceneryManager;
    [SerializeField] private PhraseManager phraseManager;
    [SerializeField] private CharacterSetup characterSetup;
    [SerializeField] private AnimationManager animationManager;

    [Header("Storyboard Scenery Parents")]
    [SerializeField] private List<GameObject> storyBoardSceneryRoots = new List<GameObject>();

    [Header("Cameras")]
    [SerializeField] private List<Camera> sceneCameras = new List<Camera>();

    [Header("UI")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button rotateCharacter1Button;
    [SerializeField] private Button rotateCharacter2Button;
    [SerializeField] private TMP_Text characterText1;
    [SerializeField] private TMP_Text characterText2;
    [SerializeField] private GameObject namePositionText1;
    [SerializeField] private GameObject namePositionText2;
    [SerializeField] private TMP_Text sceneDescriptionTMP;
    [SerializeField] private Text sceneDescriptionText;
    [SerializeField] private List<GameObject> character1UiObjects = new List<GameObject>();
    [SerializeField] private List<GameObject> character2UiObjects = new List<GameObject>();

    [Header("UI Follow")]
    [SerializeField] private bool useScreenSpaceUi = true;
    [SerializeField] private Camera uiFollowCamera;

    [Header("Z-Axis Controls")]
    [SerializeField] private Scrollbar character1ZAxisScrollbar;
    [SerializeField] private Scrollbar character2ZAxisScrollbar;
    [SerializeField] private Button character1ZAxisPlusButton;
    [SerializeField] private Button character1ZAxisMinusButton;
    [SerializeField] private Button character2ZAxisPlusButton;
    [SerializeField] private Button character2ZAxisMinusButton;
    [SerializeField] private float zAxisMaxOffset = 8f;
    [SerializeField] private float zAxisStep = 0.5f;

    private PointerHoldTracker character1ZHoldTracker;
    private PointerHoldTracker character2ZHoldTracker;

    [Header("Results Sequence")]
    [SerializeField] private GameObject resultsTextSpace;
    [SerializeField] private GameObject ResultsPanel;
    [SerializeField] private UiPanelTransition resultsPanelTransition;
    [SerializeField] private Image PhotoImage;
    [SerializeField] private TextMeshProUGUI resultsPhraseText;
    [SerializeField] private float resultsIntroDuration = 1.5f;
    [SerializeField] private float resultsSpawnOffset = 2f;
    [SerializeField] private float resultsMoveDuration = 1.5f;
    [SerializeField] private float resultsCharacterDelay = 0.5f;
    [SerializeField] private float resultsHoldDuration = 3f;
    [SerializeField] private float resultsTriggerDelay = 0.1f;
    [SerializeField] private AnimationCurve resultsMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Full Display")]
    [SerializeField] private GameObject FullDisplayPanel;
    [SerializeField] private UiPanelTransition fullDisplayPanelTransition;
    [SerializeField] private List<Image> fullDisplayImages = new List<Image>();
    [SerializeField] private List<TextMeshProUGUI> fullDisplayPhrases = new List<TextMeshProUGUI>();
    [SerializeField] private Button RestartButton;

    [Header("Reset Positions")]
    [SerializeField] private Button resetPositionsButton;

    [Header("Photo Capture")]
    [SerializeField] private int captureWidth = 1280;
    [SerializeField] private int captureHeight = 720;

    private const int TotalScenes = 6;
    private int currentSceneNumber;

    public int CurrentSceneNumber => currentSceneNumber;
    private int scenery1Index;
    private int scenery2Index;
    private bool storyBoardStarted;
    private readonly List<Sprite> capturedPhotos = new List<Sprite>();
    private readonly List<string> capturedPhrases = new List<string>();
    private Coroutine resultsCoroutine;

    private struct UiFollowEntry
    {
        public Transform target;
        public Transform uiTransform;
        public Vector3 offset;
        public bool useScreenSpace;
        public Camera camera;
    }

    private readonly List<UiFollowEntry> character1Follow = new List<UiFollowEntry>();
    private readonly List<UiFollowEntry> character2Follow = new List<UiFollowEntry>();
    private readonly Dictionary<Transform, Vector3> uiInitialPositions = new Dictionary<Transform, Vector3>();
    private readonly float[] character1BaseZ = new float[TotalScenes];
    private readonly float[] character2BaseZ = new float[TotalScenes];

    private void Awake()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinuePressed);
        }

        if (rotateCharacter1Button != null)
        {
            rotateCharacter1Button.onClick.AddListener(OnRotateCharacter1Pressed);
        }

        if (rotateCharacter2Button != null)
        {
            rotateCharacter2Button.onClick.AddListener(OnRotateCharacter2Pressed);
        }

        if (RestartButton != null)
        {
            RestartButton.onClick.AddListener(OnRestartPressed);
        }

        if (resetPositionsButton != null)
        {
            resetPositionsButton.onClick.AddListener(OnResetPositionsPressed);
        }

        if (character1ZAxisScrollbar != null)
        {
            character1ZAxisScrollbar.onValueChanged.AddListener(OnCharacter1ZAxisChanged);
        }

        if (character2ZAxisScrollbar != null)
        {
            character2ZAxisScrollbar.onValueChanged.AddListener(OnCharacter2ZAxisChanged);
        }

        if (character1ZAxisPlusButton != null)
        {
            character1ZAxisPlusButton.onClick.AddListener(OnCharacter1ZAxisPlusPressed);
        }

        if (character1ZAxisMinusButton != null)
        {
            character1ZAxisMinusButton.onClick.AddListener(OnCharacter1ZAxisMinusPressed);
        }

        if (character2ZAxisPlusButton != null)
        {
            character2ZAxisPlusButton.onClick.AddListener(OnCharacter2ZAxisPlusPressed);
        }

        if (character2ZAxisMinusButton != null)
        {
            character2ZAxisMinusButton.onClick.AddListener(OnCharacter2ZAxisMinusPressed);
        }

        HookScrollbarHoldTrackers();
        EnsureReferencesByName();
        SetAllStoryBoardSceneryInactive();
        SetAllCamerasActive(false);
        EnsureCapturedStorageSize();
        SetResultsPanelsActive(false);
        CacheInitialUiPositions();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinuePressed);
        }

        if (rotateCharacter1Button != null)
        {
            rotateCharacter1Button.onClick.RemoveListener(OnRotateCharacter1Pressed);
        }

        if (rotateCharacter2Button != null)
        {
            rotateCharacter2Button.onClick.RemoveListener(OnRotateCharacter2Pressed);
        }

        if (RestartButton != null)
        {
            RestartButton.onClick.RemoveListener(OnRestartPressed);
        }

        if (resetPositionsButton != null)
        {
            resetPositionsButton.onClick.RemoveListener(OnResetPositionsPressed);
        }

        if (character1ZAxisScrollbar != null)
        {
            character1ZAxisScrollbar.onValueChanged.RemoveListener(OnCharacter1ZAxisChanged);
        }

        if (character2ZAxisScrollbar != null)
        {
            character2ZAxisScrollbar.onValueChanged.RemoveListener(OnCharacter2ZAxisChanged);
        }

        if (character1ZAxisPlusButton != null)
        {
            character1ZAxisPlusButton.onClick.RemoveListener(OnCharacter1ZAxisPlusPressed);
        }

        if (character1ZAxisMinusButton != null)
        {
            character1ZAxisMinusButton.onClick.RemoveListener(OnCharacter1ZAxisMinusPressed);
        }

        if (character2ZAxisPlusButton != null)
        {
            character2ZAxisPlusButton.onClick.RemoveListener(OnCharacter2ZAxisPlusPressed);
        }

        if (character2ZAxisMinusButton != null)
        {
            character2ZAxisMinusButton.onClick.RemoveListener(OnCharacter2ZAxisMinusPressed);
        }

        UnhookScrollbarHoldTrackers();

        CleanupCapturedPhotos();
    }

    public void StartStoryBoard()
    {
        if (resultsCoroutine != null)
        {
            StopCoroutine(resultsCoroutine);
            resultsCoroutine = null;
        }

        CaptureSceneryIndexes();
        if (sceneryManager != null)
        {
            sceneryManager.SetSceneryOptionsActive(false);
        }

        DisableSceneryStageUI();
        ClearCapturedData();
        SetResultsPanelsActive(false);

        SetPanelActive(storyBoardStageRoot, storyBoardStageTransition, true);

        if (continueButton != null)
        {
            continueButton.interactable = true;
        }

        storyBoardStarted = true;
        currentSceneNumber = 1;
        ShowScene(currentSceneNumber);
    }

    private void LateUpdate()
    {
        if (!storyBoardStarted)
        {
            return;
        }

        UpdateFollowList(character1Follow, character1ZHoldTracker);
        UpdateFollowList(character2Follow, character2ZHoldTracker);
    }

    private void HookScrollbarHoldTrackers()
    {
        HookScrollbarHoldTracker(character1ZAxisScrollbar, ref character1ZHoldTracker, OnCharacter1ScrollbarHoldChanged);
        HookScrollbarHoldTracker(character2ZAxisScrollbar, ref character2ZHoldTracker, OnCharacter2ScrollbarHoldChanged);
    }

    private void UnhookScrollbarHoldTrackers()
    {
        if (character1ZHoldTracker != null)
        {
            character1ZHoldTracker.HoldChanged -= OnCharacter1ScrollbarHoldChanged;
        }

        if (character2ZHoldTracker != null)
        {
            character2ZHoldTracker.HoldChanged -= OnCharacter2ScrollbarHoldChanged;
        }
    }

    private static void HookScrollbarHoldTracker(Scrollbar scrollbar, ref PointerHoldTracker tracker, System.Action<bool> onChanged)
    {
        if (scrollbar == null)
        {
            return;
        }

        tracker = scrollbar.GetComponent<PointerHoldTracker>();
        if (tracker == null)
        {
            tracker = scrollbar.gameObject.AddComponent<PointerHoldTracker>();
        }

        tracker.HoldChanged -= onChanged;
        tracker.HoldChanged += onChanged;
    }

    private void OnCharacter1ScrollbarHoldChanged(bool held)
    {
        if (!held)
        {
            UpdateFollowList(character1Follow, character1ZHoldTracker);
        }
    }

    private void OnCharacter2ScrollbarHoldChanged(bool held)
    {
        if (!held)
        {
            UpdateFollowList(character2Follow, character2ZHoldTracker);
        }
    }

    private void OnContinuePressed()
    {
        if (!storyBoardStarted)
        {
            return;
        }

        CapturePhoto();

        if (currentSceneNumber >= TotalScenes)
        {
            FinishStoryBoard();
            return;
        }

        currentSceneNumber++;
        ShowScene(currentSceneNumber);
    }

    private void OnRotateCharacter1Pressed()
    {
        ToggleCurrentSceneCharacterRotation(1);
    }

    private void OnRotateCharacter2Pressed()
    {
        ToggleCurrentSceneCharacterRotation(2);
    }

    private void ShowScene(int sceneNumber)
    {
        if (sceneNumber < 1 || sceneNumber > TotalScenes)
        {
            return;
        }

        SetAllStoryBoardSceneryInactive();
        SetAllCamerasActive(false);

        int rootIndex = sceneNumber - 1;
        if (rootIndex >= 0 && rootIndex < storyBoardSceneryRoots.Count)
        {
            GameObject sceneRoot = storyBoardSceneryRoots[rootIndex];
            if (sceneRoot != null)
            {
                sceneRoot.SetActive(true);
                int sceneryIndex = GetSceneryIndexForScene(sceneNumber);
                ActivateSceneryVariant(sceneRoot.transform, sceneryIndex);
            }
        }

        if (rootIndex >= 0 && rootIndex < sceneCameras.Count && sceneCameras[rootIndex] != null)
        {
            sceneCameras[rootIndex].gameObject.SetActive(true);
        }

        SetCharacterUiForScene(sceneNumber);
        SetSceneDescription(sceneNumber);
        ResetZAxisScrollbars();
        BindCharacterUiFollow(sceneNumber);
        UpdateCharacterNameTexts();
    }

    private void FinishStoryBoard()
    {
        storyBoardStarted = false;

        if (continueButton != null)
        {
            continueButton.interactable = false;
        }

        if (guionPanel != null)
        {
            guionPanel.SetActive(false);
        }

        SetPanelActive(storyBoardStageRoot, storyBoardStageTransition, false);

        resultsCoroutine = StartCoroutine(ShowResultsSequence());
    }

    private void OnResetPositionsPressed()
    {
        if (characterSetup != null)
        {
            characterSetup.ResetAllCopiesToSpawn();
        }

        ResetZAxisScrollbars();
        BindCharacterUiFollow(currentSceneNumber);
    }

    private void CapturePhoto()
    {
        int sceneNumber = currentSceneNumber;
        int sceneIndex = sceneNumber - 1;
        if (sceneIndex < 0 || sceneIndex >= TotalScenes)
        {
            return;
        }

        EnsureCapturedStorageSize();
        capturedPhrases[sceneIndex] = GetPhraseForScene(sceneNumber);

        if (sceneIndex < 0 || sceneIndex >= sceneCameras.Count || sceneCameras[sceneIndex] == null)
        {
            return;
        }

        Camera sourceCamera = sceneCameras[sceneIndex];
        int width = Mathf.Max(1, captureWidth);
        int height = Mathf.Max(1, captureHeight);

        RenderTexture tempRT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = sourceCamera.targetTexture;

        sourceCamera.targetTexture = tempRT;
        sourceCamera.Render();
        RenderTexture.active = tempRT;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        texture.Apply();

        sourceCamera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(tempRT);

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
        ReplaceCapturedPhoto(sceneIndex, sprite);
    }

    private IEnumerator ShowResultsSequence()
    {
        if (resultsTextSpace != null)
        {
            resultsTextSpace.SetActive(true);
        }

        SetPanelActive(ResultsPanel, resultsPanelTransition, true);

        if (resultsIntroDuration > 0f)
        {
            yield return new WaitForSeconds(resultsIntroDuration);
        }

        SetPanelActive(ResultsPanel, resultsPanelTransition, false);

        for (int i = 0; i < TotalScenes; i++)
        {
            yield return StartCoroutine(PlayCinematicScene(i + 1));
        }

        ShowFullDisplay();
        resultsCoroutine = null;
    }

    private void ShowFullDisplay()
    {
        SetPanelActive(ResultsPanel, resultsPanelTransition, false);
        SetPanelActive(FullDisplayPanel, fullDisplayPanelTransition, true);

        for (int i = 0; i < fullDisplayImages.Count; i++)
        {
            if (fullDisplayImages[i] != null)
            {
                fullDisplayImages[i].sprite = i < capturedPhotos.Count ? capturedPhotos[i] : null;
            }
        }

        for (int i = 0; i < fullDisplayPhrases.Count; i++)
        {
            if (fullDisplayPhrases[i] != null)
            {
                string phrase = i < capturedPhrases.Count ? capturedPhrases[i] : "Frase no disponible";
                fullDisplayPhrases[i].text = phrase;
            }
        }
    }


    private IEnumerator PlayCinematicScene(int sceneNumber)
    {
        if (sceneNumber < 1 || sceneNumber > TotalScenes)
        {
            yield break;
        }

        SetupResultsScene(sceneNumber, false);
        UpdateResultsPhrase(sceneNumber);

        int sceneIndex = sceneNumber - 1;
        CharacterDraggable[] sceneDraggables = GetSceneDraggables(sceneIndex);
        CharacterDraggable character1 = FindSceneCharacter(sceneDraggables, sceneIndex, 0);
        CharacterDraggable character2 = FindSceneCharacter(sceneDraggables, sceneIndex, 1);
        SetSceneDraggablesActive(sceneDraggables, false);

        Vector3 finalPosition1 = Vector3.zero;
        Vector3 finalPosition2 = Vector3.zero;
        bool hasCharacter1 = false;
        bool hasCharacter2 = false;

        if (character1 != null)
        {
            finalPosition1 = character1.transform.position;
            hasCharacter1 = true;
        }

        float appliedDelay = 0f;
        if (hasCharacter1 && resultsCharacterDelay > 0f)
        {
            appliedDelay = resultsCharacterDelay;
        }

        if (character2 != null)
        {
            finalPosition2 = character2.transform.position;
            hasCharacter2 = true;
        }

        Vector3 offsetDir1 = Vector3.left;
        Vector3 offsetDir2 = Vector3.right;

        Vector3 facingDir1 = Vector3.zero;
        Vector3 facingDir2 = Vector3.zero;
        bool hasFacingDir1 = hasCharacter1 && TryGetFacingHorizontalOffsetDirection(character1.transform, out facingDir1);
        bool hasFacingDir2 = hasCharacter2 && TryGetFacingHorizontalOffsetDirection(character2.transform, out facingDir2);

        if (hasFacingDir1)
        {
            offsetDir1 = facingDir1;
        }

        if (hasFacingDir2)
        {
            offsetDir2 = facingDir2;
        }

        // Fallback: if a character is facing front/back (no clear left/right), keep the previous logic
        // that offsets based on which character is on the left/right.
        if (hasCharacter1 && hasCharacter2 && (!hasFacingDir1 || !hasFacingDir2))
        {
            bool character1IsLeft = finalPosition1.x <= finalPosition2.x;
            Vector3 compareDir1 = character1IsLeft ? Vector3.left : Vector3.right;
            Vector3 compareDir2 = character1IsLeft ? Vector3.right : Vector3.left;

            if (!hasFacingDir1)
            {
                offsetDir1 = compareDir1;
            }

            if (!hasFacingDir2)
            {
                offsetDir2 = compareDir2;
            }
        }

        if (hasCharacter1)
        {
            PrepareCharacterForCinematic(sceneIndex, true, character1.transform, offsetDir1);
        }

        if (hasCharacter2)
        {
            PrepareCharacterForCinematic(sceneIndex, false, character2.transform, offsetDir2);
        }

        ResetOtherSceneAnimatorsToDefault(sceneIndex);

        ActivateResultsCamera(sceneIndex);

        if (hasCharacter1)
        {
            StartCoroutine(PlayCharacterCinematic(sceneIndex, true, character1.transform, finalPosition1));
        }

        if (hasCharacter2 && appliedDelay > 0f)
        {
            yield return new WaitForSeconds(appliedDelay);
        }

        if (hasCharacter2)
        {
            StartCoroutine(PlayCharacterCinematic(sceneIndex, false, character2.transform, finalPosition2));
        }

        float totalMoveTime = Mathf.Max(0.1f, resultsMoveDuration + appliedDelay);
        yield return new WaitForSeconds(totalMoveTime);

        if (resultsHoldDuration > 0f)
        {
            yield return new WaitForSeconds(resultsHoldDuration);
        }
    }

    private IEnumerator PlayCharacterCinematic(int sceneIndex, bool character1, Transform target, Vector3 finalPosition)
    {
        if (target == null)
        {
            yield break;
        }

        Animator animator = animationManager != null ? animationManager.GetAnimatorAt(sceneIndex, character1) : null;

        if (resultsTriggerDelay > 0f)
        {
            yield return new WaitForSeconds(resultsTriggerDelay);
        }

        if (animator != null && animationManager != null)
        {
            string finalTrigger = animationManager.GetLastTriggerForScene(sceneIndex, character1);
            animator.SetTrigger(finalTrigger);
        }

        Vector3 startPosition = target.position;
        float duration = Mathf.Max(0.01f, resultsMoveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = resultsMoveCurve != null ? resultsMoveCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);
            target.position = Vector3.LerpUnclamped(startPosition, finalPosition, eased);
            yield return null;
        }

        target.position = finalPosition;
    }

    private static bool TryGetFacingHorizontalOffsetDirection(Transform target, out Vector3 direction)
    {
        direction = Vector3.zero;

        if (target == null)
        {
            return false;
        }

        // Determine if the character is clearly facing left/right in world space.
        // If they face mostly forward/backward, we return false so caller can use the legacy comparison logic.
        Vector3 forward = target.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        forward.Normalize();
        float dotX = Vector3.Dot(forward, Vector3.right);

        const float horizontalFacingThreshold = 0.7f; // ~45 degrees
        if (Mathf.Abs(dotX) < horizontalFacingThreshold)
        {
            return false;
        }

        direction = dotX >= 0f ? Vector3.right : Vector3.left;
        return true;
    }

    private void SetupResultsScene(int sceneNumber, bool activateCamera)
    {
        SetAllStoryBoardSceneryInactive();
        SetAllCamerasActive(false);

        int rootIndex = sceneNumber - 1;
        if (rootIndex >= 0 && rootIndex < storyBoardSceneryRoots.Count)
        {
            GameObject sceneRoot = storyBoardSceneryRoots[rootIndex];
            if (sceneRoot != null)
            {
                sceneRoot.SetActive(true);
                int sceneryIndex = GetSceneryIndexForScene(sceneNumber);
                ActivateSceneryVariant(sceneRoot.transform, sceneryIndex);
            }
        }

        if (activateCamera)
        {
            ActivateResultsCamera(rootIndex);
        }

        SetCharacterUiForScene(sceneNumber);
    }

    private void SetCharacterUiForScene(int sceneNumber)
    {
        bool character1Active = sceneNumber != 4;
        bool character2Active = sceneNumber != 1;

        SetUiObjectsActive(character1UiObjects, character1Active);
        SetUiObjectsActive(character2UiObjects, character2Active);

        if (characterText1 != null)
        {
            characterText1.gameObject.SetActive(character1Active);
        }

        if (characterText2 != null)
        {
            characterText2.gameObject.SetActive(character2Active);
        }

        if (namePositionText1 != null)
        {
            namePositionText1.gameObject.SetActive(character1Active);
        }

        if (namePositionText2 != null)
        {
            namePositionText2.gameObject.SetActive(character2Active);
        }

        if (character1ZAxisPlusButton != null)
        {
            character1ZAxisPlusButton.gameObject.SetActive(character1Active);
        }

        if (character1ZAxisMinusButton != null)
        {
            character1ZAxisMinusButton.gameObject.SetActive(character1Active);
        }

        if (character2ZAxisPlusButton != null)
        {
            character2ZAxisPlusButton.gameObject.SetActive(character2Active);
        }

        if (character2ZAxisMinusButton != null)
        {
            character2ZAxisMinusButton.gameObject.SetActive(character2Active);
        }
    }

    private void UpdateCharacterNameTexts()
    {
        if (phraseManager == null)
        {
            return;
        }

        string tipo1;
        string personaje;

        if (phraseManager.TryGetCharacterDescriptor(1, out tipo1, out personaje))
        {
            SetCharacterNameText(characterText1, personaje);
        }

        if (phraseManager.TryGetCharacterDescriptor(2, out tipo1, out personaje))
        {
            SetCharacterNameText(characterText2, personaje);
        }
    }

    private static void SetCharacterNameText(TMP_Text target, string value)
    {
        if (target == null)
        {
            return;
        }

        target.text = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
    }

    private static void SetUiObjectsActive(List<GameObject> targets, bool active)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            GameObject target = targets[i];
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }

    private void ActivateResultsCamera(int sceneIndex)
    {
        if (sceneIndex >= 0 && sceneIndex < sceneCameras.Count && sceneCameras[sceneIndex] != null)
        {
            sceneCameras[sceneIndex].gameObject.SetActive(true);
        }
    }

    private Vector3 PrepareCharacterForCinematic(int sceneIndex, bool character1, Transform target, Vector3 offsetDirection)
    {
        Vector3 finalPosition = target.position;
        Animator animator = animationManager != null ? animationManager.GetAnimatorAt(sceneIndex, character1) : null;
        if (animator != null)
        {
            animator.SetTrigger("Neutral");
        }

        target.position = finalPosition + offsetDirection * resultsSpawnOffset;
        return finalPosition;
    }

    private void ResetOtherSceneAnimatorsToDefault(int activeSceneIndex)
    {
        if (animationManager == null)
        {
            return;
        }

        for (int i = 0; i < TotalScenes; i++)
        {
            if (i == activeSceneIndex)
            {
                continue;
            }

            Animator anim1 = animationManager.GetAnimatorAt(i, true);
            if (anim1 != null)
            {
                anim1.SetTrigger("Neutral");
            }

            Animator anim2 = animationManager.GetAnimatorAt(i, false);
            if (anim2 != null)
            {
                anim2.SetTrigger("Neutral");
            }
        }
    }

    private void UpdateResultsPhrase(int sceneNumber)
    {
        if (resultsPhraseText == null)
        {
            return;
        }

        string phrase = sceneNumber - 1 < capturedPhrases.Count ? capturedPhrases[sceneNumber - 1] : "Frase no disponible";
        resultsPhraseText.text = string.Format("Escena {0}: {1}", sceneNumber, phrase);
    }

    private static CharacterDraggable[] GetSceneDraggables(int sceneIndex)
    {
        CharacterDraggable[] draggables = FindObjectsOfType<CharacterDraggable>(true);
        List<CharacterDraggable> matches = new List<CharacterDraggable>();

        for (int i = 0; i < draggables.Length; i++)
        {
            CharacterDraggable draggable = draggables[i];
            if (draggable != null && draggable.CopyIndex == sceneIndex)
            {
                matches.Add(draggable);
            }
        }

        return matches.ToArray();
    }

    private static CharacterDraggable FindSceneCharacter(CharacterDraggable[] draggables, int sceneIndex, int characterIndex)
    {
        if (draggables == null)
        {
            return null;
        }

        for (int i = 0; i < draggables.Length; i++)
        {
            CharacterDraggable draggable = draggables[i];
            if (draggable != null && draggable.CopyIndex == sceneIndex && draggable.CharacterIndex == characterIndex)
            {
                return draggable;
            }
        }

        return null;
    }

    private static void SetSceneDraggablesActive(CharacterDraggable[] draggables, bool active)
    {
        if (draggables == null)
        {
            return;
        }

        for (int i = 0; i < draggables.Length; i++)
        {
            CharacterDraggable draggable = draggables[i];
            if (draggable != null)
            {
                draggable.IsDraggable = active;
            }
        }
    }

    private void EnsureCapturedStorageSize()
    {
        while (capturedPhotos.Count < TotalScenes)
        {
            capturedPhotos.Add(null);
        }

        while (capturedPhrases.Count < TotalScenes)
        {
            capturedPhrases.Add("Frase no disponible");
        }
    }

    private void ClearCapturedData()
    {
        EnsureCapturedStorageSize();

        for (int i = 0; i < capturedPhrases.Count; i++)
        {
            capturedPhrases[i] = "Frase no disponible";
        }

        for (int i = 0; i < capturedPhotos.Count; i++)
        {
            ReplaceCapturedPhoto(i, null);
        }
    }

    private void ReplaceCapturedPhoto(int index, Sprite newSprite)
    {
        if (index < 0 || index >= capturedPhotos.Count)
        {
            return;
        }

        Sprite previous = capturedPhotos[index];
        if (previous != null)
        {
            Texture oldTexture = previous.texture;
            Object.Destroy(previous);

            if (oldTexture != null)
            {
                Object.Destroy(oldTexture);
            }
        }

        capturedPhotos[index] = newSprite;
    }

    private void CleanupCapturedPhotos()
    {
        for (int i = 0; i < capturedPhotos.Count; i++)
        {
            ReplaceCapturedPhoto(i, null);
        }
    }

    private string GetPhraseForScene(int sceneNumber)
    {
        if (phraseManager != null)
        {
            string phrase;
            if (phraseManager.TryGetSceneFinalPhrase(sceneNumber, out phrase) && !string.IsNullOrWhiteSpace(phrase))
            {
                return phrase;
            }
        }

        return "Frase no disponible";
    }

    private void SetResultsPanelsActive(bool active)
    {
        SetPanelActive(ResultsPanel, resultsPanelTransition, active);
        SetPanelActive(FullDisplayPanel, fullDisplayPanelTransition, active);
    }

    private static void SetPanelActive(GameObject panel, UiPanelTransition transition, bool active)
    {
        if (panel == null)
        {
            return;
        }

        UiPanelTransition resolvedTransition = transition != null ? transition : panel.GetComponent<UiPanelTransition>();
        if (resolvedTransition != null)
        {
            if (active)
            {
                resolvedTransition.TransitionIn();
            }
            else
            {
                resolvedTransition.TransitionOut();
            }

            return;
        }

        panel.SetActive(active);
    }

    private void OnRestartPressed()
    {
        if (TransitionPanelManager.Instance != null)
        {
            TransitionPanelManager.Instance.ChangeScene("MainMenu");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene("MainMenu");
    }

    private void CaptureSceneryIndexes()
    {
        scenery1Index = 0;
        scenery2Index = 0;

        if (sceneryManager == null)
        {
            return;
        }

        scenery1Index = Mathf.Max(0, sceneryManager.GetSelectedSceneryIndex(1));
        scenery2Index = Mathf.Max(0, sceneryManager.GetSelectedSceneryIndex(2));
    }

    private int GetSceneryIndexForScene(int sceneNumber)
    {
        if (sceneNumber <= 3)
        {
            return scenery1Index;
        }

        return scenery2Index;
    }

    private static void ActivateSceneryVariant(Transform root, int targetIndex)
    {
        if (root == null)
        {
            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        string targetName = targetIndex.ToString();
        Transform namedChild = root.Find(targetName);
        if (namedChild != null)
        {
            namedChild.gameObject.SetActive(true);
            return;
        }

        if (targetIndex >= 0 && targetIndex < root.childCount)
        {
            root.GetChild(targetIndex).gameObject.SetActive(true);
        }
    }

    private void SetSceneDescription(int sceneNumber)
    {
        string description = "Frase no disponible";

        if (phraseManager != null)
        {
            string phrase;
            if (phraseManager.TryGetSceneFinalPhrase(sceneNumber, out phrase) && !string.IsNullOrWhiteSpace(phrase))
            {
                description = phrase;
            }
        }

        string finalText = string.Format("Escena {0}: {1}", sceneNumber, description);

        if (sceneDescriptionTMP != null)
        {
            sceneDescriptionTMP.text = finalText;
        }

        if (sceneDescriptionText != null)
        {
            sceneDescriptionText.text = finalText;
        }
    }

    private void CacheInitialUiPositions()
    {
        CacheInitialPositions(character1UiObjects);
        CacheInitialPositions(character2UiObjects);
        CacheInitialPosition(rotateCharacter1Button);
        CacheInitialPosition(rotateCharacter2Button);
        CacheInitialPosition(characterText1);
        CacheInitialPosition(characterText2);
        CacheInitialPosition(namePositionText1 != null ? namePositionText1.transform : null);
        CacheInitialPosition(namePositionText2 != null ? namePositionText2.transform : null);
        CacheInitialPosition(character1ZAxisScrollbar);
        CacheInitialPosition(character2ZAxisScrollbar);
        CacheInitialPosition(character1ZAxisPlusButton);
        CacheInitialPosition(character1ZAxisMinusButton);
        CacheInitialPosition(character2ZAxisPlusButton);
        CacheInitialPosition(character2ZAxisMinusButton);
    }

    private void CacheInitialPositions(List<GameObject> targets)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            GameObject target = targets[i];
            if (target == null)
            {
                continue;
            }

            CacheInitialPosition(target.transform);
        }
    }

    private void CacheInitialPosition(Component target)
    {
        if (target == null)
        {
            return;
        }

        CacheInitialPosition(target.transform);
    }

    private void CacheInitialPosition(Transform target)
    {
        if (target == null || uiInitialPositions.ContainsKey(target))
        {
            return;
        }

        uiInitialPositions.Add(target, target.position);
    }

    private void RestoreInitialPositions(List<GameObject> targets)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            GameObject target = targets[i];
            if (target == null)
            {
                continue;
            }

            RestoreInitialPosition(target.transform);
        }
    }

    private void RestoreInitialPosition(Component target)
    {
        if (target == null)
        {
            return;
        }

        RestoreInitialPosition(target.transform);
    }

    private void RestoreInitialPosition(Transform target)
    {
        if (target == null)
        {
            return;
        }

        Vector3 cachedPosition;
        if (uiInitialPositions.TryGetValue(target, out cachedPosition))
        {
            target.position = cachedPosition;
        }
    }

    private void BindCharacterUiFollow(int sceneNumber)
    {
        character1Follow.Clear();
        character2Follow.Clear();

        RestoreInitialPositions(character1UiObjects);
        RestoreInitialPositions(character2UiObjects);
        RestoreInitialPosition(rotateCharacter1Button);
        RestoreInitialPosition(rotateCharacter2Button);
        RestoreInitialPosition(characterText1);
        RestoreInitialPosition(characterText2);
        RestoreInitialPosition(namePositionText1 != null ? namePositionText1.transform : null);
        RestoreInitialPosition(namePositionText2 != null ? namePositionText2.transform : null);
        RestoreInitialPosition(character1ZAxisScrollbar);
        RestoreInitialPosition(character2ZAxisScrollbar);
        RestoreInitialPosition(character1ZAxisPlusButton);
        RestoreInitialPosition(character1ZAxisMinusButton);
        RestoreInitialPosition(character2ZAxisPlusButton);
        RestoreInitialPosition(character2ZAxisMinusButton);

        int copyIndex = sceneNumber - 1;
        Transform character1Target = characterSetup != null ? characterSetup.GetCopyTransform(copyIndex, 1) : null;
        Transform character2Target = characterSetup != null ? characterSetup.GetCopyTransform(copyIndex, 2) : null;

        Transform namePosition1Transform = namePositionText1 != null ? namePositionText1.transform : null;
        Transform namePosition2Transform = namePositionText2 != null ? namePositionText2.transform : null;

        CacheBaseZForScene(sceneNumber, character1Target, character2Target);

        BuildFollowEntries(character1Target, null, character1Follow, characterText1, namePosition1Transform);
        BuildFollowEntries(character2Target, null, character2Follow, characterText2, namePosition2Transform);
    }

    private void BuildFollowEntries(
        Transform target,
        List<GameObject> uiObjects,
        List<UiFollowEntry> entries,
        params Component[] extra)
    {
        if (target == null)
        {
            return;
        }

        HashSet<Transform> visited = new HashSet<Transform>();

        if (uiObjects != null)
        {
            for (int i = 0; i < uiObjects.Count; i++)
            {
                GameObject obj = uiObjects[i];
                if (obj == null)
                {
                    continue;
                }

                TryAddFollowEntry(target, obj.transform, entries, visited);
            }
        }

        if (extra != null)
        {
            for (int i = 0; i < extra.Length; i++)
            {
                Component component = extra[i];
                if (component == null)
                {
                    continue;
                }

                TryAddFollowEntry(target, component.transform, entries, visited);
            }
        }
    }

    private void TryAddFollowEntry(
        Transform target,
        Transform uiTransform,
        List<UiFollowEntry> entries,
        HashSet<Transform> visited)
    {
        if (uiTransform == null || !visited.Add(uiTransform))
        {
            return;
        }

        Camera camera = ResolveUiCamera();
        bool screenSpace = useScreenSpaceUi && camera != null;
        Vector3 targetUiPosition = GetTargetUiPosition(target, uiTransform, camera, screenSpace);

        UiFollowEntry entry = new UiFollowEntry
        {
            target = target,
            uiTransform = uiTransform,
            offset = uiTransform.position - targetUiPosition,
            useScreenSpace = screenSpace,
            camera = camera
        };

        entries.Add(entry);
    }

    private static void UpdateFollowList(List<UiFollowEntry> entries, PointerHoldTracker holdTracker)
    {
        if (entries == null || entries.Count == 0)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            UiFollowEntry entry = entries[i];
            if (entry.target == null || entry.uiTransform == null)
            {
                continue;
            }

            if (holdTracker != null && holdTracker.IsHeld && entry.uiTransform == holdTracker.transform)
            {
                continue;
            }

            Vector3 targetPosition = entry.useScreenSpace && entry.camera != null
                ? entry.camera.WorldToScreenPoint(entry.target.position)
                : entry.target.position;

            targetPosition.z = entry.uiTransform.position.z;
            entry.uiTransform.position = targetPosition + entry.offset;
        }
    }

    private Camera ResolveUiCamera()
    {
        if (uiFollowCamera != null)
        {
            return uiFollowCamera;
        }

        return Camera.main;
    }

    private static Vector3 GetTargetUiPosition(Transform target, Transform uiTransform, Camera camera, bool screenSpace)
    {
        if (!screenSpace || camera == null)
        {
            return target.position;
        }

        Vector3 screenPoint = camera.WorldToScreenPoint(target.position);
        screenPoint.z = uiTransform.position.z;
        return screenPoint;
    }

    private void ToggleCurrentSceneCharacterRotation(int characterNumber)
    {
        if (!storyBoardStarted || characterSetup == null)
        {
            return;
        }

        int copyIndex = currentSceneNumber - 1;
        characterSetup.ToggleCopyRotation(copyIndex, characterNumber);
    }

    private void OnCharacter1ZAxisChanged(float value)
    {
        if (!storyBoardStarted || characterSetup == null)
        {
            return;
        }

        int copyIndex = currentSceneNumber - 1;
        float zPosition = GetCharacterBaseZ(copyIndex, 1) + Mathf.Clamp01(value) * Mathf.Max(0f, zAxisMaxOffset);
        characterSetup.SetCharacterZPosition(copyIndex, 1, zPosition);
    }

    private void OnCharacter2ZAxisChanged(float value)
    {
        if (!storyBoardStarted || characterSetup == null)
        {
            return;
        }

        int copyIndex = currentSceneNumber - 1;
        float zPosition = GetCharacterBaseZ(copyIndex, 2) + Mathf.Clamp01(value) * Mathf.Max(0f, zAxisMaxOffset);
        characterSetup.SetCharacterZPosition(copyIndex, 2, zPosition);
    }

    private void OnCharacter1ZAxisPlusPressed()
    {
        AdjustCharacterZAxis(1, zAxisStep);
    }

    private void OnCharacter1ZAxisMinusPressed()
    {
        AdjustCharacterZAxis(1, -zAxisStep);
    }

    private void OnCharacter2ZAxisPlusPressed()
    {
        AdjustCharacterZAxis(2, zAxisStep);
    }

    private void OnCharacter2ZAxisMinusPressed()
    {
        AdjustCharacterZAxis(2, -zAxisStep);
    }

    private void AdjustCharacterZAxis(int characterNumber, float delta)
    {
        if (!storyBoardStarted || characterSetup == null)
        {
            return;
        }

        int copyIndex = currentSceneNumber - 1;
        Transform target = characterSetup.GetCopyTransform(copyIndex, characterNumber);
        if (target == null)
        {
            return;
        }

        float baseZ = GetCharacterBaseZ(copyIndex, characterNumber);
        float maxOffset = Mathf.Max(0f, zAxisMaxOffset);
        float currentOffset = Mathf.Clamp(target.position.z - baseZ, 0f, maxOffset);
        float nextOffset = Mathf.Clamp(currentOffset + delta, 0f, maxOffset);
        float normalized = maxOffset > 0f ? nextOffset / maxOffset : 0f;

        Scrollbar scrollbar = characterNumber == 1 ? character1ZAxisScrollbar : character2ZAxisScrollbar;
        if (scrollbar != null)
        {
            scrollbar.SetValueWithoutNotify(normalized);
        }

        if (characterNumber == 1)
        {
            OnCharacter1ZAxisChanged(normalized);
        }
        else
        {
            OnCharacter2ZAxisChanged(normalized);
        }
    }

    private void ResetZAxisScrollbars()
    {
        if (character1ZAxisScrollbar != null)
        {
            character1ZAxisScrollbar.SetValueWithoutNotify(0f);
            OnCharacter1ZAxisChanged(0f);
        }

        if (character2ZAxisScrollbar != null)
        {
            character2ZAxisScrollbar.SetValueWithoutNotify(0f);
            OnCharacter2ZAxisChanged(0f);
        }
    }

    private void CacheBaseZForScene(int sceneNumber, Transform character1Target, Transform character2Target)
    {
        int sceneIndex = sceneNumber - 1;
        if (sceneIndex < 0 || sceneIndex >= TotalScenes)
        {
            return;
        }

        if (character1Target != null)
        {
            character1BaseZ[sceneIndex] = character1Target.position.z;
        }

        if (character2Target != null)
        {
            character2BaseZ[sceneIndex] = character2Target.position.z;
        }
    }

    private float GetCharacterBaseZ(int copyIndex, int characterNumber)
    {
        if (copyIndex < 0 || copyIndex >= TotalScenes)
        {
            return 0f;
        }

        return characterNumber == 1 ? character1BaseZ[copyIndex] : character2BaseZ[copyIndex];
    }

    private void DisableSceneryStageUI()
    {
        if (sceneryStageRoot != null)
        {
            sceneryStageRoot.SetActive(false);
        }

        for (int i = 0; i < sceneryUiObjectsToDisable.Count; i++)
        {
            GameObject target = sceneryUiObjectsToDisable[i];
            if (target != null)
            {
                target.SetActive(false);
            }
        }
    }

    private void SetAllStoryBoardSceneryInactive()
    {
        for (int i = 0; i < storyBoardSceneryRoots.Count; i++)
        {
            GameObject root = storyBoardSceneryRoots[i];
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }

    private void SetAllCamerasActive(bool active)
    {
        for (int i = 0; i < sceneCameras.Count; i++)
        {
            Camera cam = sceneCameras[i];
            if (cam != null)
            {
                cam.gameObject.SetActive(active);
            }
        }
    }

    private void EnsureReferencesByName()
    {
        AutoPopulateSceneryRoots();
        AutoPopulateCameras();

        if (sceneDescriptionTMP == null)
        {
            GameObject tmpTextObject = GameObject.Find("SceneDescriptionText");
            if (tmpTextObject != null)
            {
                sceneDescriptionTMP = tmpTextObject.GetComponent<TMP_Text>();
                sceneDescriptionText = tmpTextObject.GetComponent<Text>();
            }
        }
    }

    private void AutoPopulateSceneryRoots()
    {
        if (storyBoardSceneryRoots.Count >= TotalScenes)
        {
            return;
        }

        storyBoardSceneryRoots.Clear();

        for (int i = 1; i <= TotalScenes; i++)
        {
            GameObject root = GameObject.Find(string.Format("SB_Scenery_{0}", i));
            storyBoardSceneryRoots.Add(root);
        }
    }

    private void AutoPopulateCameras()
    {
        if (sceneCameras.Count >= TotalScenes)
        {
            return;
        }

        sceneCameras.Clear();

        for (int i = 1; i <= TotalScenes; i++)
        {
            GameObject camObject = GameObject.Find(string.Format("Scene{0}Cam", i));
            Camera cam = camObject != null ? camObject.GetComponent<Camera>() : null;
            sceneCameras.Add(cam);
        }
    }
}
