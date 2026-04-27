using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class StoryBoardManager : MonoBehaviour
{
    [Header("Stage Roots")]
    [SerializeField] private GameObject sceneryStageRoot;
    [SerializeField] private List<GameObject> sceneryUiObjectsToDisable = new List<GameObject>();
    [SerializeField] private GameObject storyBoardStageRoot;
    [SerializeField] private GameObject guionPanel;

    [Header("Dependencies")]
    [SerializeField] private SceneryManager sceneryManager;
    [SerializeField] private PhraseManager phraseManager;
    [SerializeField] private CharacterSetup characterSetup;

    [Header("Storyboard Scenery Parents")]
    [SerializeField] private List<GameObject> storyBoardSceneryRoots = new List<GameObject>();

    [Header("Cameras")]
    [SerializeField] private List<Camera> sceneCameras = new List<Camera>();

    [Header("UI")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button rotateCharacter1Button;
    [SerializeField] private Button rotateCharacter2Button;
    [SerializeField] private TMP_Text sceneDescriptionTMP;
    [SerializeField] private Text sceneDescriptionText;

    [Header("Results Sequence")]
    [SerializeField] private GameObject ResultsPanel;
    [SerializeField] private Image PhotoImage;
    [SerializeField] private TextMeshProUGUI resultsPhraseText;
    [SerializeField] private float displayDuration = 5f;

    [Header("Full Display")]
    [SerializeField] private GameObject FullDisplayPanel;
    [SerializeField] private List<Image> fullDisplayImages = new List<Image>();
    [SerializeField] private List<TextMeshProUGUI> fullDisplayPhrases = new List<TextMeshProUGUI>();
    [SerializeField] private Button RestartButton;

    [Header("Photo Capture")]
    [SerializeField] private int captureWidth = 1280;
    [SerializeField] private int captureHeight = 720;

    private const int TotalScenes = 6;
    private int currentSceneNumber;
    private int scenery1Index;
    private int scenery2Index;
    private bool storyBoardStarted;
    private readonly List<Sprite> capturedPhotos = new List<Sprite>();
    private readonly List<string> capturedPhrases = new List<string>();
    private Coroutine resultsCoroutine;

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

        EnsureReferencesByName();
        SetAllStoryBoardSceneryInactive();
        SetAllCamerasActive(false);
        EnsureCapturedStorageSize();
        SetResultsPanelsActive(false);
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
        DisableSceneryStageUI();
        ClearCapturedData();
        SetResultsPanelsActive(false);

        if (storyBoardStageRoot != null)
        {
            storyBoardStageRoot.SetActive(true);
        }

        if (continueButton != null)
        {
            continueButton.interactable = true;
        }

        storyBoardStarted = true;
        currentSceneNumber = 1;
        ShowScene(currentSceneNumber);
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

        SetSceneDescription(sceneNumber);
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

        resultsCoroutine = StartCoroutine(ShowResultsSequence());
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
        if (ResultsPanel != null)
        {
            ResultsPanel.SetActive(true);
        }

        for (int i = 0; i < TotalScenes; i++)
        {
            if (PhotoImage != null)
            {
                PhotoImage.sprite = i < capturedPhotos.Count ? capturedPhotos[i] : null;
            }

            if (resultsPhraseText != null)
            {
                string phrase = i < capturedPhrases.Count ? capturedPhrases[i] : "Frase no disponible";
                resultsPhraseText.text = string.Format("Escena {0}: {1}", i + 1, phrase);
            }

            yield return StartCoroutine(WaitForResultsAdvance());
        }

        ShowFullDisplay();
        resultsCoroutine = null;
    }

    private void ShowFullDisplay()
    {
        if (ResultsPanel != null)
        {
            ResultsPanel.SetActive(false);
        }

        if (FullDisplayPanel != null)
        {
            FullDisplayPanel.SetActive(true);
        }

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

    private IEnumerator WaitForResultsAdvance()
    {
        float elapsed = 0f;

        while (elapsed < displayDuration)
        {
            if (IsAdvanceInputPressed())
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        while (IsAnyInputHeld())
        {
            yield return null;
        }
    }

    private static bool IsAdvanceInputPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        Touchscreen touch = Touchscreen.current;
        if (touch != null)
        {
            var touches = touch.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                if (touches[i] != null && touches[i].press.wasPressedThisFrame)
                {
                    return true;
                }
            }
        }

        return false;
#else
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                {
                    return true;
                }
            }
        }

        return false;
#endif
    }

    private static bool IsAnyInputHeld()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
        {
            return true;
        }

        Touchscreen touch = Touchscreen.current;
        if (touch != null)
        {
            var touches = touch.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                if (touches[i] != null && touches[i].press.isPressed)
                {
                    return true;
                }
            }
        }

        return false;
#else
        return Input.GetMouseButton(0) || Input.touchCount > 0;
#endif
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
        if (ResultsPanel != null)
        {
            ResultsPanel.SetActive(active);
        }

        if (FullDisplayPanel != null)
        {
            FullDisplayPanel.SetActive(active);
        }
    }

    private void OnRestartPressed()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
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

    private void ToggleCurrentSceneCharacterRotation(int characterNumber)
    {
        if (!storyBoardStarted || characterSetup == null)
        {
            return;
        }

        int copyIndex = currentSceneNumber - 1;
        characterSetup.ToggleCopyRotation(copyIndex, characterNumber);
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
