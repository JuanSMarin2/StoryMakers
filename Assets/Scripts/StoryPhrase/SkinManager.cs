using System;
using System.Collections.Generic;
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

    [Header("Categories")]
    [SerializeField] private SkinCategory pelo = new SkinCategory { name = "Pelo" };
    [SerializeField] private SkinCategory camisa = new SkinCategory { name = "Camisa" };
    [SerializeField] private SkinCategory pantalon = new SkinCategory { name = "Pantalon" };

    private void Awake()
    {
        BindButtons(pelo, PreviousPelo, NextPelo);
        BindButtons(camisa, PreviousCamisa, NextCamisa);
        BindButtons(pantalon, PreviousPantalon, NextPantalon);
    }

    private void Start()
    {
        ApplyCurrentTexture(pelo);
        ApplyCurrentTexture(camisa);
        ApplyCurrentTexture(pantalon);
    }

    private void OnDestroy()
    {
        UnbindButtons(pelo, PreviousPelo, NextPelo);
        UnbindButtons(camisa, PreviousCamisa, NextCamisa);
        UnbindButtons(pantalon, PreviousPantalon, NextPantalon);
    }

    public void NextPelo()
    {
        MoveNext(pelo);
    }

    public void PreviousPelo()
    {
        MovePrevious(pelo);
    }

    public void NextCamisa()
    {
        MoveNext(camisa);
    }

    public void PreviousCamisa()
    {
        MovePrevious(camisa);
    }

    public void NextPantalon()
    {
        MoveNext(pantalon);
    }

    public void PreviousPantalon()
    {
        MovePrevious(pantalon);
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
}
