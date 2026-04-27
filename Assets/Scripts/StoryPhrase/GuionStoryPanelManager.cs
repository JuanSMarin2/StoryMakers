using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GuionStoryPanelManager : MonoBehaviour
{
    private const int TotalScenes = 6;

    [Header("References")]
    [SerializeField] private PhraseManager phraseManager;
    [SerializeField] private GameObject guionStoryPanel;
    [SerializeField] private List<TMP_Text> sceneTexts = new List<TMP_Text>();

    [Header("Messages")]
    [SerializeField] private string missingSceneMessage = "Aun no has escrito esta parte.";

    private void OnEnable()
    {
        RefreshPanel();
    }

    public void OpenPanel()
    {
        if (guionStoryPanel != null)
        {
            guionStoryPanel.SetActive(true);
        }

        RefreshPanel();
    }

    public void ClosePanel()
    {
        if (guionStoryPanel != null)
        {
            guionStoryPanel.SetActive(false);
        }
    }

    [ContextMenu("Refresh Story Panel")]
    public void RefreshPanel()
    {
        for (int sceneNumber = 1; sceneNumber <= TotalScenes; sceneNumber++)
        {
            TMP_Text targetText = GetSceneText(sceneNumber);
            if (targetText == null)
            {
                continue;
            }

            string line;
            string phrase;
            if (phraseManager != null && phraseManager.TryGetSceneFinalPhrase(sceneNumber, out phrase) && !string.IsNullOrWhiteSpace(phrase))
            {
                line = string.Format("Escena {0}: {1}", sceneNumber, phrase);
            }
            else
            {
                line = string.Format("Escena {0}: {1}", sceneNumber, missingSceneMessage);
            }

            targetText.text = line;
        }
    }

    private TMP_Text GetSceneText(int sceneNumber)
    {
        int index = sceneNumber - 1;
        if (index < 0 || index >= sceneTexts.Count)
        {
            return null;
        }

        return sceneTexts[index];
    }
}