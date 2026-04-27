using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SceneryManager : MonoBehaviour
{
    [Serializable]
    private class SceneryOption
    {
        public string label;
        public GameObject targetObject;
        public List<PlaceOption> representedPlaces = new List<PlaceOption>();
    }

    private enum FlowStage
    {
        Scene1Selection,
        Scene2Selection,
        Completed
    }

    [Header("Scenery")]
    [SerializeField] private List<SceneryOption> sceneryOptions = new List<SceneryOption>();
    [SerializeField] private bool debugSceneryIndex = true;

    [Header("Controls")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject SB_Panel;

    [Header("Dependencies")]
    [SerializeField] private PhraseManager phraseManager;
    [Header("Validation Audio")]
    [SerializeField] private AudioSource validationAudioSource;
    [SerializeField] private AudioClip wrongSceneryClip;

    [Header("Texts")]
    [SerializeField] private TMP_Text sceneryStatusText;
    [SerializeField] private TMP_Text sceneryText;

    [Header("Optional Phrases")]
    [SerializeField] private List<string> scenePrompts = new List<string>
    {
        "Escenario 1",
        "Escenario 2"
    };

    private FlowStage currentStage = FlowStage.Scene1Selection;
    private int currentOptionIndex;
    private readonly int[] selectedSceneryIndex = new int[2] { -1, -1 };
    private bool sceneryStageStarted;

    private void Awake()
    {
        // Never allow scenery variants to stay active from scene defaults.
        SetAllSceneryObjectsActive(false);

        if (previousButton != null)
        {
            previousButton.onClick.AddListener(PreviousScenery);
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextScenery);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinuePressed);
        }
    }

    private void Start()
    {
        sceneryStageStarted = false;
        SetButtonsActive(false);
        SetAllSceneryObjectsActive(false);
        SetStatusText("Esperando etapa de escenarios");
        SetSceneryText("Sin escenarios activos");

        if (SB_Panel != null)
        {
            SB_Panel.SetActive(false);
        }
    }

    public void StartSceneryStage()
    {
        sceneryStageStarted = true;
        selectedSceneryIndex[0] = -1;
        selectedSceneryIndex[1] = -1;
        currentOptionIndex = 0;

        // Stage starts with only index 0 active.
        EnterStage(FlowStage.Scene1Selection);
    }

    private void OnDestroy()
    {
        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(PreviousScenery);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextScenery);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinuePressed);
        }
    }

    private void Update()
    {
        if (!sceneryStageStarted)
        {
            return;
        }

        RefreshSceneryText();
    }

    public void NextScenery()
    {
        if (!sceneryStageStarted || !IsSelectionStage() || !HasOptions())
        {
            return;
        }

        currentOptionIndex = (currentOptionIndex + 1) % sceneryOptions.Count;
        ApplyCurrentOption();
    }

    public void PreviousScenery()
    {
        if (!sceneryStageStarted || !IsSelectionStage() || !HasOptions())
        {
            return;
        }

        currentOptionIndex = (currentOptionIndex - 1 + sceneryOptions.Count) % sceneryOptions.Count;
        ApplyCurrentOption();
    }

    public int GetSelectedSceneryIndex(int sceneNumber)
    {
        if (sceneNumber < 1 || sceneNumber > 2)
        {
            return -1;
        }

        return selectedSceneryIndex[sceneNumber - 1];
    }

    public bool TryGetSelectedSceneryLabel(int sceneNumber, out string label)
    {
        label = string.Empty;

        int selectedIndex = GetSelectedSceneryIndex(sceneNumber);
        if (selectedIndex < 0 || selectedIndex >= sceneryOptions.Count)
        {
            return false;
        }

        label = GetOptionLabel(selectedIndex);
        return !string.IsNullOrWhiteSpace(label);
    }

    private void OnContinuePressed()
    {
        if (!sceneryStageStarted || !IsSelectionStage())
        {
            return;
        }

        if (!HasOptions())
        {
            EnterStage(FlowStage.Completed);
            return;
        }
        string validationMessage;
        if (!CanContinueCurrentScene(out validationMessage))
        {
            SetStatusText(validationMessage);
            PlayWrongScenerySound();
            return;
        }
        SetStatusText("Escoje el escenario para tu escena");

        if (currentStage == FlowStage.Scene1Selection)
        {
            selectedSceneryIndex[0] = currentOptionIndex;
            LogSavedIndex(1, selectedSceneryIndex[0]);
            EnterStage(FlowStage.Scene2Selection);
            return;
        }

        if (currentStage == FlowStage.Scene2Selection)
        {
            selectedSceneryIndex[1] = currentOptionIndex;
            LogSavedIndex(2, selectedSceneryIndex[1]);
            EnterStage(FlowStage.Completed);
        }
    }

    private void EnterStage(FlowStage stage)
    {
        if (!sceneryStageStarted)
        {
            return;
        }

        currentStage = stage;

        if (currentStage == FlowStage.Completed)
        {
            SetButtonsActive(false);
            SetStatusText("Seleccion de escenarios completa");
            SetSceneryText(BuildCompletedText());
            SetAllSceneryObjectsActive(false);

            if (SB_Panel != null)
            {
                SB_Panel.SetActive(true);
            }
            return;
        }

        if (currentStage == FlowStage.Scene1Selection || currentStage == FlowStage.Scene2Selection)
        {
            currentOptionIndex = 0;
        }

        SetButtonsActive(true);
        SetStatusText("Escoje el escenario para tu escena");
        ApplyCurrentOption();
    }

    private void ApplyCurrentOption()
    {
        if (!HasOptions())
        {
            SetAllSceneryObjectsActive(false);
            SetSceneryText(GetStageLabel() + ": Sin escenarios");
            return;
        }

        currentOptionIndex = Mathf.Clamp(currentOptionIndex, 0, sceneryOptions.Count - 1);
        SetAllSceneryObjectsActive(false);

        GameObject optionTarget = sceneryOptions[currentOptionIndex].targetObject;
        if (optionTarget != null)
        {
            optionTarget.SetActive(true);
        }

        RefreshSceneryText();
    }

    private string BuildCompletedText()
    {
        string scene1 = GetPlaceFromPhraseManagerOrFallback(1);
        string scene2 = GetPlaceFromPhraseManagerOrFallback(2);
        return string.Format("Escenario 1: {0} | Escenario 2: {1}", scene1, scene2);
    }

    private int GetCurrentSceneNumber()
    {
        return currentStage == FlowStage.Scene2Selection ? 2 : 1;
    }

    private string GetPlaceFromPhraseManagerOrFallback(int sceneNumber)
    {
        if (phraseManager != null)
        {
            string place;
            if (phraseManager.TryGetSceneryPlace(sceneNumber, out place) && !string.IsNullOrWhiteSpace(place))
            {
                return place;
            }
        }

        return "Lugar no definido";
    }
    private bool CanContinueCurrentScene(out string message)
    {
        message = string.Empty;

        if (!HasOptions() || currentOptionIndex < 0 || currentOptionIndex >= sceneryOptions.Count)
        {
            return true;
        }

        if (phraseManager == null)
        {
            return true;
        }

        int sceneNumber = GetCurrentSceneNumber();
        PlaceOption expectedPlace;
        if (!phraseManager.TryGetSceneryPlaceOption(sceneNumber, out expectedPlace))
        {
            message = "Debes confirmar Decide Assets en PhraseManager antes de continuar.";
            return false;
        }

        SceneryOption selectedOption = sceneryOptions[currentOptionIndex];
        if (selectedOption == null || selectedOption.representedPlaces == null || selectedOption.representedPlaces.Count == 0)
        {
            message = "Configura los lugares permitidos para este escenario en el inspector.";
            return false;
        }

        if (selectedOption.representedPlaces.Contains(expectedPlace))
        {
            return true;
        }

        message = string.Format("Escenario incorrecto. Debe representar: {0}.", FormatPlaceOption(expectedPlace));
        return false;
    }

    private static string FormatPlaceOption(PlaceOption placeOption)
    {
        switch (placeOption)
        {
            case PlaceOption.Parque:
                return "el parque";
            case PlaceOption.Colegio:
                return "el colegio";
            case PlaceOption.Teatro:
                return "el teatro";
            case PlaceOption.Calle:
                return "la calle";
            case PlaceOption.Casa:
                return "la casa";
            case PlaceOption.Hospital:
                return "el hospital";
            case PlaceOption.Estacion:
                return "la estacion";
            case PlaceOption.Banco:
                return "el banco";
            case PlaceOption.Oficina:
                return "la oficina";
            default:
                return "lugar configurado";
        }
    }

    private void PlayWrongScenerySound()
    {
        if (wrongSceneryClip == null)
        {
            return;
        }

        if (validationAudioSource != null)
        {
            validationAudioSource.PlayOneShot(wrongSceneryClip);
            return;
        }

        AudioSource.PlayClipAtPoint(wrongSceneryClip, transform.position);
    }

    private void RefreshSceneryText()
    {
        if (currentStage == FlowStage.Completed)
        {
            SetSceneryText(BuildCompletedText());
            return;
        }

        if (!IsSelectionStage())
        {
            return;
        }

        SetSceneryText(string.Format("{0}: {1}", GetStageLabel(), GetPlaceFromPhraseManagerOrFallback(GetCurrentSceneNumber())));
    }

    private string GetStoredLabel(int arrayIndex)
    {
        if (arrayIndex < 0 || arrayIndex >= selectedSceneryIndex.Length)
        {
            return "Sin seleccionar";
        }

        int storedIndex = selectedSceneryIndex[arrayIndex];
        if (storedIndex < 0 || storedIndex >= sceneryOptions.Count)
        {
            return "Sin seleccionar";
        }

        return GetOptionLabel(storedIndex);
    }

    private string GetStageLabel()
    {
        int stageNumber = currentStage == FlowStage.Scene2Selection ? 2 : 1;
        int promptIndex = stageNumber - 1;

        if (scenePrompts != null && promptIndex >= 0 && promptIndex < scenePrompts.Count && !string.IsNullOrWhiteSpace(scenePrompts[promptIndex]))
        {
            return scenePrompts[promptIndex].Trim();
        }

        return string.Format("Escenario {0}", stageNumber);
    }

    private string GetOptionLabel(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= sceneryOptions.Count)
        {
            return "Sin etiqueta";
        }

        string configuredLabel = sceneryOptions[optionIndex].label;
        if (!string.IsNullOrWhiteSpace(configuredLabel))
        {
            return configuredLabel.Trim();
        }

        return string.Format("Escenario {0}", optionIndex + 1);
    }

    private bool IsSelectionStage()
    {
        return currentStage == FlowStage.Scene1Selection || currentStage == FlowStage.Scene2Selection;
    }

    private bool HasOptions()
    {
        return sceneryOptions != null && sceneryOptions.Count > 0;
    }

    private void SetButtonsActive(bool active)
    {
        if (previousButton != null)
        {
            previousButton.gameObject.SetActive(active);
            previousButton.interactable = active;
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(active);
            nextButton.interactable = active;
        }

        if (continueButton != null)
        {
            continueButton.interactable = active;
        }
    }

    private void SetAllSceneryObjectsActive(bool active)
    {
        if (sceneryOptions == null)
        {
            return;
        }

        for (int i = 0; i < sceneryOptions.Count; i++)
        {
            GameObject targetObject = sceneryOptions[i].targetObject;
            if (targetObject != null)
            {
                targetObject.SetActive(active);
            }
        }
    }

    private void SetStatusText(string text)
    {
        if (sceneryStatusText != null)
        {
            sceneryStatusText.text = text;
        }
    }

    private void SetSceneryText(string text)
    {
        if (sceneryText != null)
        {
            sceneryText.text = text;
        }
    }

    private void LogSavedIndex(int sceneNumber, int savedIndex)
    {
        if (!debugSceneryIndex)
        {
            return;
        }

        string label = GetOptionLabel(savedIndex);
        Debug.Log(string.Format(
            "SceneryManager: guardado escenario {0} -> index {1} ({2})",
            sceneNumber,
            savedIndex,
            label));
    }
}