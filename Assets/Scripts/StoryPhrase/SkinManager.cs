using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinManager : MonoBehaviour
{
    [Serializable]
    private class SkinCategory
    {
        public string name;
        public GameObject targetObject;
        public List<Texture> textures = new List<Texture>();
        public Button previousButton;
        public Button nextButton;

        [HideInInspector] public int currentIndex;
    }

    [Serializable]
    private class CharacterSetup
    {
        public string label = "Personaje";
        public GameObject rootObject;

        [Header("Gender")]
        public GameObject maleImage;
        public GameObject femaleImage;
        public GameObject maleModel;
        public GameObject femaleModel;

        [Header("Clothing Targets")]
        public GameObject peloTarget;
        public GameObject camisaTarget;
        public GameObject pantalonTarget;
    }

    private enum FlowStage
    {
        Character1Gender,
        Character1Dress,
        Character2Gender,
        Character2Dress,
        Completed
    }

    [Header("Categories")]
    [SerializeField] private SkinCategory pelo = new SkinCategory { name = "Pelo" };
    [SerializeField] private SkinCategory camisa = new SkinCategory { name = "Camisa" };
    [SerializeField] private SkinCategory pantalon = new SkinCategory { name = "Pantalon" };

    [Header("Flow Characters")]
    [SerializeField] private CharacterSetup character1 = new CharacterSetup { label = "Personaje 1" };
    [SerializeField] private CharacterSetup character2 = new CharacterSetup { label = "Personaje 2" };

    [Header("Gender Buttons")]
    [SerializeField] private Button previousGenderButton;
    [SerializeField] private Button nextGenderButton;

    [Header("Flow")]
    [SerializeField] private Button continueButton;
    [SerializeField] private PhraseManager phraseManager;
    [SerializeField] private global::CharacterSetup characterSetup;
    [SerializeField] private SceneryManager sceneryManager;

    [Header("Scenery Phase Objects")]
    [SerializeField] private List<GameObject> sceneryPhaseObjects = new List<GameObject>();

    [Header("Texts")]
    [SerializeField] private TMP_Text skinStatusText;
    [SerializeField] private TMP_Text characterText;

    private FlowStage currentStage = FlowStage.Character1Gender;
    private int selectedCharacterIndex;
    private readonly int[] selectedGender = new int[2];

    private void Awake()
    {
        if (sceneryManager == null)
        {
            sceneryManager = FindObjectOfType<SceneryManager>();
        }

        if (character1.peloTarget == null)
        {
            character1.peloTarget = pelo.targetObject;
        }

        if (character1.camisaTarget == null)
        {
            character1.camisaTarget = camisa.targetObject;
        }

        if (character1.pantalonTarget == null)
        {
            character1.pantalonTarget = pantalon.targetObject;
        }

        BindButtons(pelo, PreviousPelo, NextPelo);
        BindButtons(camisa, PreviousCamisa, NextCamisa);
        BindButtons(pantalon, PreviousPantalon, NextPantalon);

        if (previousGenderButton != null)
        {
            previousGenderButton.onClick.AddListener(PreviousGender);
        }

        if (nextGenderButton != null)
        {
            nextGenderButton.onClick.AddListener(NextGender);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinuePressed);
        }
    }

    private void Start()
    {
        SetSceneryPhaseObjectsActive(false);
        SetSkinPhaseObjectsActive(true);

        ApplyCurrentTexture(pelo);
        ApplyCurrentTexture(camisa);
        ApplyCurrentTexture(pantalon);

        selectedGender[0] = 0;
        selectedGender[1] = 0;

        EnterStage(FlowStage.Character1Gender);
    }

    private void OnDestroy()
    {
        UnbindButtons(pelo, PreviousPelo, NextPelo);
        UnbindButtons(camisa, PreviousCamisa, NextCamisa);
        UnbindButtons(pantalon, PreviousPantalon, NextPantalon);

        if (previousGenderButton != null)
        {
            previousGenderButton.onClick.RemoveListener(PreviousGender);
        }

        if (nextGenderButton != null)
        {
            nextGenderButton.onClick.RemoveListener(NextGender);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinuePressed);
        }
    }

    private void Update()
    {
        if (currentStage == FlowStage.Completed)
        {
            return;
        }

        RefreshCharacterText();
    }

    public void NextPelo()
    {
        if (!IsDressStage())
        {
            return;
        }

        MoveNext(pelo);
    }

    public void PreviousPelo()
    {
        if (!IsDressStage())
        {
            return;
        }

        MovePrevious(pelo);
    }

    public void NextCamisa()
    {
        if (!IsDressStage())
        {
            return;
        }

        MoveNext(camisa);
    }

    public void PreviousCamisa()
    {
        if (!IsDressStage())
        {
            return;
        }

        MovePrevious(camisa);
    }

    public void NextPantalon()
    {
        if (!IsDressStage())
        {
            return;
        }

        MoveNext(pantalon);
    }

    public void PreviousPantalon()
    {
        if (!IsDressStage())
        {
            return;
        }

        MovePrevious(pantalon);
    }

    public void NextGender()
    {
        if (!IsGenderStage())
        {
            return;
        }

        selectedGender[selectedCharacterIndex] = (selectedGender[selectedCharacterIndex] + 1) % 2;
        RefreshCurrentCharacterVisuals();
    }

    public void PreviousGender()
    {
        if (!IsGenderStage())
        {
            return;
        }

        selectedGender[selectedCharacterIndex] = (selectedGender[selectedCharacterIndex] + 1) % 2;
        RefreshCurrentCharacterVisuals();
    }

    private void OnContinuePressed()
    {
        switch (currentStage)
        {
            case FlowStage.Character1Gender:
                EnterStage(FlowStage.Character1Dress);
                break;
            case FlowStage.Character1Dress:
                CreateCharacterCopies(1, character1);
                EnterStage(FlowStage.Character2Gender);
                break;
            case FlowStage.Character2Gender:
                EnterStage(FlowStage.Character2Dress);
                break;
            case FlowStage.Character2Dress:
                CreateCharacterCopies(2, character2);
                EnterStage(FlowStage.Completed);
                break;
            case FlowStage.Completed:
                break;
        }
    }

    private static void BindButtons(SkinCategory category, UnityEngine.Events.UnityAction previousAction, UnityEngine.Events.UnityAction nextAction)
    {
        if (category.previousButton != null)
        {
            category.previousButton.onClick.AddListener(previousAction);
        }

        if (category.nextButton != null)
        {
            category.nextButton.onClick.AddListener(nextAction);
        }
    }

    private static void UnbindButtons(SkinCategory category, UnityEngine.Events.UnityAction previousAction, UnityEngine.Events.UnityAction nextAction)
    {
        if (category.previousButton != null)
        {
            category.previousButton.onClick.RemoveListener(previousAction);
        }

        if (category.nextButton != null)
        {
            category.nextButton.onClick.RemoveListener(nextAction);
        }
    }

    private static void MoveNext(SkinCategory category)
    {
        if (!HasTextures(category))
        {
            return;
        }

        category.currentIndex = (category.currentIndex + 1) % category.textures.Count;
        ApplyTextureToTarget(category);
    }

    private static void MovePrevious(SkinCategory category)
    {
        if (!HasTextures(category))
        {
            return;
        }

        category.currentIndex = (category.currentIndex - 1 + category.textures.Count) % category.textures.Count;
        ApplyTextureToTarget(category);
    }

    private bool IsGenderStage()
    {
        return currentStage == FlowStage.Character1Gender || currentStage == FlowStage.Character2Gender;
    }

    private bool IsDressStage()
    {
        return currentStage == FlowStage.Character1Dress || currentStage == FlowStage.Character2Dress;
    }

    private void EnterStage(FlowStage stage)
    {
        currentStage = stage;

        if (stage == FlowStage.Completed)
        {
            SetSkinPhaseObjectsActive(false);
            SetSceneryPhaseObjectsActive(true);

            if (sceneryManager == null)
            {
                sceneryManager = FindObjectOfType<SceneryManager>();
            }

            if (sceneryManager != null)
            {
                sceneryManager.StartSceneryStage();
            }
            else
            {
                Debug.LogWarning("SkinManager: no se encontro SceneryManager para iniciar la etapa de escenarios.");
            }

            SetCharacterRootActive(character1, false);
            SetCharacterRootActive(character2, false);
            SetGenderButtonsActive(false);
            SetSkinButtonsActive(false);
            SetClothingObjectsActive(character1, false);
            SetClothingObjectsActive(character2, false);
            return;
        }

        selectedCharacterIndex = (stage == FlowStage.Character1Gender || stage == FlowStage.Character1Dress) ? 0 : 1;
        CharacterSetup activeCharacter = GetCurrentCharacter();
        CharacterSetup inactiveCharacter = selectedCharacterIndex == 0 ? character2 : character1;
        SetSkinPhaseObjectsActive(true);
        SetSceneryPhaseObjectsActive(false);

        SetCharacterRootActive(inactiveCharacter, false);
        SetCharacterRootActive(activeCharacter, true);

        bool genderStage = IsGenderStage();
        SetGenderButtonsActive(genderStage);
        SetSkinButtonsActive(!genderStage);

        SetClothingObjectsActive(activeCharacter, !genderStage);
        SetClothingObjectsActive(inactiveCharacter, false);

        if (genderStage)
        {
            SetSkinStatus("Elige el genero de tu personaje");
        }
        else
        {
            SetSkinStatus("Viste a tu personaje");
            ApplyCurrentTexture(pelo);
            ApplyCurrentTexture(camisa);
            ApplyCurrentTexture(pantalon);
        }

        RefreshCurrentCharacterVisuals();
        RefreshCharacterText();
    }

    private void RefreshCurrentCharacterVisuals()
    {
        CharacterSetup activeCharacter = GetCurrentCharacter();
        if (activeCharacter == null)
        {
            return;
        }

        bool femaleSelected = selectedGender[selectedCharacterIndex] == 1;

        SetActiveIfNotNull(activeCharacter.maleImage, !femaleSelected);
        SetActiveIfNotNull(activeCharacter.femaleImage, femaleSelected);
        SetActiveIfNotNull(activeCharacter.maleModel, !femaleSelected);
        SetActiveIfNotNull(activeCharacter.femaleModel, femaleSelected);

        pelo.targetObject = activeCharacter.peloTarget;
        camisa.targetObject = activeCharacter.camisaTarget;
        pantalon.targetObject = activeCharacter.pantalonTarget;
    }

    private CharacterSetup GetCurrentCharacter()
    {
        return selectedCharacterIndex == 0 ? character1 : character2;
    }

    private static void SetCharacterRootActive(CharacterSetup setup, bool active)
    {
        if (setup != null && setup.rootObject != null)
        {
            setup.rootObject.SetActive(active);
        }
    }

    private static void SetClothingObjectsActive(CharacterSetup setup, bool active)
    {
        if (setup == null)
        {
            return;
        }

        SetActiveIfNotNull(setup.peloTarget, active);
        SetActiveIfNotNull(setup.camisaTarget, active);
        SetActiveIfNotNull(setup.pantalonTarget, active);
    }

    private static void SetActiveIfNotNull(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void SetSkinPhaseObjectsActive(bool active)
    {
        SetCharacterRootActive(character1, active);
        SetCharacterRootActive(character2, active);

        SetGameObjectActive(previousGenderButton, active);
        SetGameObjectActive(nextGenderButton, active);
        SetSkinCategoryButtonsVisible(pelo, active);
        SetSkinCategoryButtonsVisible(camisa, active);
        SetSkinCategoryButtonsVisible(pantalon, active);
        SetGameObjectActive(continueButton, active);
        SetGameObjectActive(skinStatusText, active);
        SetGameObjectActive(characterText, active);

        if (!active)
        {
            SetClothingObjectsActive(character1, false);
            SetClothingObjectsActive(character2, false);
        }
    }

    private void SetSceneryPhaseObjectsActive(bool active)
    {
        if (sceneryPhaseObjects == null)
        {
            return;
        }

        for (int i = 0; i < sceneryPhaseObjects.Count; i++)
        {
            SetActiveIfNotNull(sceneryPhaseObjects[i], active);
        }
    }

    private static void SetSkinCategoryButtonsVisible(SkinCategory category, bool active)
    {
        if (category == null)
        {
            return;
        }

        SetGameObjectActive(category.previousButton, active);
        SetGameObjectActive(category.nextButton, active);
    }

    private static void SetGameObjectActive(Component component, bool active)
    {
        if (component != null && component.gameObject != null)
        {
            component.gameObject.SetActive(active);
        }
    }

    private void SetGenderButtonsActive(bool active)
    {
        if (previousGenderButton != null)
        {
            previousGenderButton.gameObject.SetActive(active);
            previousGenderButton.interactable = active;
        }

        if (nextGenderButton != null)
        {
            nextGenderButton.gameObject.SetActive(active);
            nextGenderButton.interactable = active;
        }
    }

    private void SetSkinButtonsActive(bool active)
    {
        SetSkinCategoryButtonsActive(pelo, active);
        SetSkinCategoryButtonsActive(camisa, active);
        SetSkinCategoryButtonsActive(pantalon, active);
    }

    private static void SetSkinCategoryButtonsActive(SkinCategory category, bool active)
    {
        if (category.previousButton != null)
        {
            category.previousButton.gameObject.SetActive(active);
            category.previousButton.interactable = active;
        }

        if (category.nextButton != null)
        {
            category.nextButton.gameObject.SetActive(active);
            category.nextButton.interactable = active;
        }
    }

    private void SetSkinStatus(string value)
    {
        if (skinStatusText != null)
        {
            skinStatusText.text = value;
        }
    }

    private void SetCharacterText(string value)
    {
        if (characterText != null)
        {
            characterText.text = value;
        }
    }

    private void RefreshCharacterText()
    {
        int displayIndex = selectedCharacterIndex + 1;
        string fallback = string.Format("Personaje {0}:", displayIndex);

        if (phraseManager == null)
        {
            SetCharacterText(fallback);
            return;
        }

        string tipo1;
        string personaje;
        if (!phraseManager.TryGetCharacterDescriptor(displayIndex, out tipo1, out personaje))
        {
            SetCharacterText(fallback);
            return;
        }

        string normalizedTipo = CapitalizeFirst(tipo1);
        string normalizedPersonaje = CapitalizeFirst(personaje);
        SetCharacterText(string.Format("Personaje {0}: {1} {2}", displayIndex, normalizedTipo, normalizedPersonaje));
    }

    private static string CapitalizeFirst(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        text = text.Trim();
        if (text.Length == 1)
        {
            return text.ToUpperInvariant();
        }

        return char.ToUpperInvariant(text[0]) + text.Substring(1);
    }

    private static void ApplyCurrentTexture(SkinCategory category)
    {
        if (!HasTextures(category))
        {
            return;
        }

        category.currentIndex = Mathf.Clamp(category.currentIndex, 0, category.textures.Count - 1);
        ApplyTextureToTarget(category);
    }

    private static bool HasTextures(SkinCategory category)
    {
        return category != null
            && category.targetObject != null
            && category.textures != null
            && category.textures.Count > 0;
    }

    private static void ApplyTextureToTarget(SkinCategory category)
    {
        Texture texture = category.textures[category.currentIndex];
        if (texture == null)
        {
            return;
        }

        Renderer targetRenderer = category.targetObject.GetComponent<Renderer>();
        if (targetRenderer != null && targetRenderer.material != null)
        {
            targetRenderer.material.mainTexture = texture;
            return;
        }

        RawImage rawImage = category.targetObject.GetComponent<RawImage>();
        if (rawImage != null)
        {
            rawImage.texture = texture;
            return;
        }

        Debug.LogWarning($"SkinManager: Target object for {category.name} has no Renderer or RawImage.");
    }

    private void CreateCharacterCopies(int characterNumber, CharacterSetup sourceCharacter)
    {
        if (characterSetup == null || sourceCharacter == null || sourceCharacter.rootObject == null)
        {
            return;
        }

        characterSetup.GenerateCopies(characterNumber, sourceCharacter.rootObject);
    }
}
