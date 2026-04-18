using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private const int TotalScenes = 6;
    private int currentSceneNumber;
    private int scenery1Index;
    private int scenery2Index;
    private bool storyBoardStarted;

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

        EnsureReferencesByName();
        SetAllStoryBoardSceneryInactive();
        SetAllCamerasActive(false);
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
    }

    public void StartStoryBoard()
    {
        CaptureSceneryIndexes();
        DisableSceneryStageUI();

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
