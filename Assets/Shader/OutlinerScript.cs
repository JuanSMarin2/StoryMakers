using UnityEngine;
using System.Collections.Generic;

public class OutlinerScriptShader : MonoBehaviour
{
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private float outlineScaleFactor;
    [SerializeField] private Color outlineColor;
    private Renderer outlineRenderer;

    void Start()
    {
        if (outlineMaterial == null)
        {
            return;
        }

        outlineRenderer = CreateOutline(outlineMaterial, outlineScaleFactor, outlineColor);
        if (outlineRenderer != null)
        {
            outlineRenderer.enabled = true;
        }
    }

    Renderer CreateOutline(Material outlineMat, float scaleFactor, Color color)
    {
        Transform outlineSource = ResolveOutlineSource();
        if (outlineSource == null)
        {
            return null;
        }

        Transform outlineParent = outlineSource.parent;
        GameObject outlineObject = Instantiate(outlineSource.gameObject, outlineParent, false);
        outlineObject.transform.localPosition = outlineSource.localPosition;
        outlineObject.transform.localRotation = outlineSource.localRotation;
        outlineObject.transform.localScale = outlineSource.localScale;

        Renderer[] renderers = outlineObject.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Destroy(outlineObject);
            return null;
        }

        Renderer rend = renderers[0];

        foreach (Renderer childRenderer in renderers)
        {
            childRenderer.sharedMaterial = outlineMat;
            childRenderer.material.SetColor("_OutlineColor", color);
            childRenderer.material.SetFloat("_Scale", scaleFactor);
            childRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        OutlinerScriptShader[] clonedScripts = outlineObject.GetComponentsInChildren<OutlinerScriptShader>(true);
        for (int i = 0; i < clonedScripts.Length; i++)
        {
            if (clonedScripts[i] != null)
            {
                clonedScripts[i].enabled = false;
            }
        }

        Collider[] clonedColliders = outlineObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < clonedColliders.Length; i++)
        {
            if (clonedColliders[i] != null)
            {
                clonedColliders[i].enabled = false;
            }
        }

        Behaviour[] clonedBehaviours = outlineObject.GetComponentsInChildren<Behaviour>(true);
        for (int i = 0; i < clonedBehaviours.Length; i++)
        {
            Behaviour behaviour = clonedBehaviours[i];
            if (behaviour != null && !(behaviour is Animator))
            {
                behaviour.enabled = false;
            }
        }

        rend.enabled = false;

        return rend;
    }

    private Transform ResolveOutlineSource()
    {
        Transform current = transform;

        while (current.parent != null)
        {
            Renderer[] parentRenderers = current.parent.GetComponentsInChildren<Renderer>(true);
            if (parentRenderers == null || parentRenderers.Length == 0)
            {
                break;
            }

            current = current.parent;
        }

        return current;
    }
}
