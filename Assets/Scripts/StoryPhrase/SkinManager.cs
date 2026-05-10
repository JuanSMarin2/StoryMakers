using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinManager : MonoBehaviour
{
    [Serializable]
    private class SkinVariant
    {
        public GameObject character1Model;
        public GameObject character2Model;
        public List<Texture> textures = new List<Texture>();
    }

    [Serializable]
    private class SkinCategory
    {
        public string name;
        public List<SkinVariant> variants = new List<SkinVariant>();
        public Button previousButton;
        public Button nextButton;

        [HideInInspector] public int currentVariantIndex;
        [HideInInspector] public int currentTextureIndex;
    }

    [Serializable]
    private class CharacterSetup
    {
        public string label = "Personaje";
        public GameObject rootObject;

        [Header("Clothing Targets")]
        public GameObject peloTarget;
        public GameObject camisaTarget;
        public GameObject pantalonTarget;
    }

    private enum FlowStage
    {
        Character1Dress,
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

    private FlowStage currentStage = FlowStage.Character1Dress;
    private int selectedCharacterIndex;

    private void Awake()
    {
        if (sceneryManager == null)
        {
            sceneryManager = FindObjectOfType<SceneryManager>();
        }

        BindButtons(pelo, PreviousPelo, NextPelo);
        BindButtons(camisa, PreviousCamisa, NextCamisa);
        BindButtons(pantalon, PreviousPantalon, NextPantalon);

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinuePressed);
        }

        // Inicializar índices de todas las categorías
        InitializeCategoryIndices(pelo);
        InitializeCategoryIndices(camisa);
        InitializeCategoryIndices(pantalon);
    }

    private static void InitializeCategoryIndices(SkinCategory category)
    {
        if (category == null)
        {
            return;
        }

        category.currentVariantIndex = 0;
        category.currentTextureIndex = 0;

        // Desactivar todas las variantes excepto la primera
        for (int i = 0; i < category.variants.Count; i++)
        {
            SetVariantActive(category.variants[i], i == 0);
        }
    }

    private void Start()
    {
        SetSceneryPhaseObjectsActive(false);
        SetSkinPhaseObjectsActive(true);

        ApplyCurrentTexture(pelo);
        ApplyCurrentTexture(camisa);
        ApplyCurrentTexture(pantalon);
        EnterStage(FlowStage.Character1Dress);
    }

    private void OnDestroy()
    {
        UnbindButtons(pelo, PreviousPelo, NextPelo);
        UnbindButtons(camisa, PreviousCamisa, NextCamisa);
        UnbindButtons(pantalon, PreviousPantalon, NextPantalon);

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

    private void OnContinuePressed()
    {
        switch (currentStage)
        {
            case FlowStage.Character1Dress:
                CreateCharacterCopies(1, character1);
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
        if (category == null || category.variants == null || category.variants.Count == 0)
        {
            return;
        }

        if (!HasAnyVariantTextures(category))
        {
            return;
        }

        SkinVariant currentVariant = category.variants[category.currentVariantIndex];
        if (currentVariant != null && currentVariant.textures != null && category.currentTextureIndex < currentVariant.textures.Count - 1)
        {
            category.currentTextureIndex++;
            ApplyCurrentTexture(category);
            return;
        }

        int nextVariantIndex = GetNextVariantIndexWithTextures(category, category.currentVariantIndex);
        if (nextVariantIndex == category.currentVariantIndex)
        {
            return;
        }

        SetVariantActive(currentVariant, false);
        category.currentVariantIndex = nextVariantIndex;
        category.currentTextureIndex = 0;
        SetVariantActive(category.variants[category.currentVariantIndex], true);
        ApplyCurrentTexture(category);
    }

    private static void MovePrevious(SkinCategory category)
    {
        if (category == null || category.variants == null || category.variants.Count == 0)
        {
            return;
        }

        if (!HasAnyVariantTextures(category))
        {
            return;
        }

        SkinVariant currentVariant = category.variants[category.currentVariantIndex];
        if (currentVariant != null && currentVariant.textures != null && category.currentTextureIndex > 0)
        {
            category.currentTextureIndex--;
            ApplyCurrentTexture(category);
            return;
        }

        int previousVariantIndex = GetPreviousVariantIndexWithTextures(category, category.currentVariantIndex);
        if (previousVariantIndex == category.currentVariantIndex)
        {
            return;
        }

        SetVariantActive(currentVariant, false);
        category.currentVariantIndex = previousVariantIndex;
        SkinVariant previousVariant = category.variants[category.currentVariantIndex];
        category.currentTextureIndex = previousVariant.textures.Count - 1;
        SetVariantActive(previousVariant, true);
        ApplyCurrentTexture(category);
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
            SetSkinButtonsActive(false);
            
            // Desactivar todas las variantes de todas las categorías
            DeactivateAllVariants(pelo);
            DeactivateAllVariants(camisa);
            DeactivateAllVariants(pantalon);
            return;
        }

        selectedCharacterIndex = stage == FlowStage.Character1Dress ? 0 : 1;
        CharacterSetup activeCharacter = GetCurrentCharacter();
        CharacterSetup inactiveCharacter = selectedCharacterIndex == 0 ? character2 : character1;
        SetSkinPhaseObjectsActive(true);
        SetSceneryPhaseObjectsActive(false);

        SetCharacterRootActive(inactiveCharacter, false);
        SetCharacterRootActive(activeCharacter, true);

        SetSkinButtonsActive(true);

        SetSkinStatus("Viste a tu personaje");
        
        // Re-inicializar los índices y variantes para esta etapa
        InitializeCategoryIndices(pelo);
        InitializeCategoryIndices(camisa);
        InitializeCategoryIndices(pantalon);
        
        ApplyCurrentTexture(pelo);
        ApplyCurrentTexture(camisa);
        ApplyCurrentTexture(pantalon);

        RefreshCurrentCharacterVisuals();
        RefreshCharacterText();
    }

    private void RefreshCurrentCharacterVisuals()
    {
        // Las variantes se manejan automáticamente en MoveNext/MovePrevious
        // Solo necesitamos inicializar los índices si es necesario
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
        // Con el nuevo sistema, deactivamos todas las variantes de cada categoría
        // cuando el personaje se desactiva (active = false)
        // Las variantes se re-inicializan en InitializeCategoryIndices cuando el personaje entra
    }

    private static void SetActiveIfNotNull(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void DeactivateAllVariants(SkinCategory category)
    {
        if (category == null || category.variants == null)
        {
            return;
        }

        foreach (SkinVariant variant in category.variants)
        {
            SetVariantActive(variant, false);
        }
    }

    private void SetSkinPhaseObjectsActive(bool active)
    {
        SetCharacterRootActive(character1, active);
        SetCharacterRootActive(character2, active);
        SetSkinCategoryButtonsVisible(pelo, active);
        SetSkinCategoryButtonsVisible(camisa, active);
        SetSkinCategoryButtonsVisible(pantalon, active);
        SetGameObjectActive(continueButton, active);
        SetGameObjectActive(skinStatusText, active);
        SetGameObjectActive(characterText, active);

        if (!active)
        {
            // Desactivar todas las variantes cuando se desactiva la fase de skin
            DeactivateAllVariants(pelo);
            DeactivateAllVariants(camisa);
            DeactivateAllVariants(pantalon);
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
        if (category == null || category.variants == null || category.variants.Count == 0)
        {
            return;
        }

        SkinVariant currentVariant = category.variants[category.currentVariantIndex];
        if (currentVariant == null || currentVariant.textures == null || currentVariant.textures.Count == 0)
        {
            return;
        }

        int textureIndex = Mathf.Clamp(category.currentTextureIndex, 0, currentVariant.textures.Count - 1);
        Texture texture = currentVariant.textures[textureIndex];

        ApplyTextureToGameObject(currentVariant.character1Model, texture);
        ApplyTextureToGameObject(currentVariant.character2Model, texture);
    }

    private static bool HasTextures(SkinCategory category)
    {
        return category != null
            && category.variants != null
            && category.variants.Count > 0;
    }

    private static bool HasAnyVariantTextures(SkinCategory category)
    {
        if (!HasTextures(category))
        {
            return false;
        }

        for (int i = 0; i < category.variants.Count; i++)
        {
            SkinVariant variant = category.variants[i];
            if (variant != null && variant.textures != null && variant.textures.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static int GetNextVariantIndexWithTextures(SkinCategory category, int startIndex)
    {
        if (!HasTextures(category))
        {
            return startIndex;
        }

        for (int offset = 1; offset <= category.variants.Count; offset++)
        {
            int candidateIndex = (startIndex + offset) % category.variants.Count;
            SkinVariant candidate = category.variants[candidateIndex];
            if (candidate != null && candidate.textures != null && candidate.textures.Count > 0)
            {
                return candidateIndex;
            }
        }

        return startIndex;
    }

    private static int GetPreviousVariantIndexWithTextures(SkinCategory category, int startIndex)
    {
        if (!HasTextures(category))
        {
            return startIndex;
        }

        for (int offset = 1; offset <= category.variants.Count; offset++)
        {
            int candidateIndex = (startIndex - offset + category.variants.Count) % category.variants.Count;
            SkinVariant candidate = category.variants[candidateIndex];
            if (candidate != null && candidate.textures != null && candidate.textures.Count > 0)
            {
                return candidateIndex;
            }
        }

        return startIndex;
    }

    private static void ApplyTextureToGameObject(GameObject target, Texture texture)
    {
        if (target == null || texture == null)
        {
            return;
        }

        Renderer targetRenderer = target.GetComponent<Renderer>();
        if (targetRenderer != null && targetRenderer.material != null)
        {
            targetRenderer.material.mainTexture = texture;
            return;
        }

        Renderer[] childRenderers = target.GetComponentsInChildren<Renderer>(true);
        bool applied = false;
        for (int i = 0; i < childRenderers.Length; i++)
        {
            Renderer renderer = childRenderers[i];
            if (renderer != null && renderer.material != null)
            {
                renderer.material.mainTexture = texture;
                applied = true;
            }
        }

        if (applied)
        {
            return;
        }

        RawImage rawImage = target.GetComponent<RawImage>();
        if (rawImage != null)
        {
            rawImage.texture = texture;
            return;
        }

        RawImage[] childRawImages = target.GetComponentsInChildren<RawImage>(true);
        for (int i = 0; i < childRawImages.Length; i++)
        {
            RawImage image = childRawImages[i];
            if (image != null)
            {
                image.texture = texture;
                return;
            }
        }

        Debug.LogWarning($"SkinManager: Target object {target.name} has no Renderer or RawImage.");
    }

    private static void SetVariantActive(SkinVariant variant, bool active)
    {
        if (variant == null)
        {
            return;
        }

        if (variant.character1Model != null)
        {
            variant.character1Model.SetActive(active);
        }
        if (variant.character2Model != null)
        {
            variant.character2Model.SetActive(active);
        }
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
