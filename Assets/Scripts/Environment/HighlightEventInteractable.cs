using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class HighlightEventInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private string interactionPrompt;
    [SerializeField] private UnityEvent onInteract;

    [Header("Hover Highlight")]
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Color highlightColor = new(0.15f, 0.8f, 1f, 1f);
    [Tooltip("Optional overlay material. Leave empty to tint the existing materials.")]
    [SerializeField] private Material hoverMaterial;

    public string InteractionPrompt => interactionPrompt;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private readonly Dictionary<Renderer, Material[]> originalMaterials = new();
    private readonly List<(Renderer renderer, int slot, MaterialPropertyBlock original)> originalBlocks = new();

    private MaterialPropertyBlock highlightBlock;
    private Material hoverMaterialInstance;
    private bool isHighlighted;

    private void Awake()
    {
        if (highlightRenderers == null || highlightRenderers.Length == 0)
            highlightRenderers = GetComponentsInChildren<Renderer>();

        highlightBlock = new MaterialPropertyBlock();
    }

    public void Interact()
    {
        if (isActiveAndEnabled)
            onInteract?.Invoke();
    }

    public void Hover()
    {
        if (isHighlighted || !isActiveAndEnabled)
            return;

        isHighlighted = true;

        if (hoverMaterial != null)
            ApplyHoverMaterial();
        else
            ApplyHoverTint();
    }

    public void Unhover()
    {
        if (!isHighlighted)
            return;

        isHighlighted = false;

        for (int i = originalBlocks.Count - 1; i >= 0; i--)
        {
            var (renderer, slot, original) = originalBlocks[i];
            if (renderer != null)
                renderer.SetPropertyBlock(original, slot);
        }

        originalBlocks.Clear();

        foreach (var entry in originalMaterials)
        {
            if (entry.Key != null)
                entry.Key.sharedMaterials = entry.Value;
        }

        originalMaterials.Clear();
    }

    private void ApplyHoverTint()
    {
        foreach (Renderer renderer in highlightRenderers)
        {
            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;
            for (int slot = 0; slot < materials.Length; slot++)
            {
                Material material = materials[slot];
                if (material == null)
                    continue;

                int colorId = material.HasProperty(BaseColorId) ? BaseColorId : ColorId;
                if (!material.HasProperty(colorId))
                    continue;

                var original = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(original, slot);
                originalBlocks.Add((renderer, slot, original.isEmpty ? null : original));

                if (original.isEmpty)
                    renderer.GetPropertyBlock(highlightBlock);
                else
                    renderer.GetPropertyBlock(highlightBlock, slot);

                highlightBlock.SetColor(colorId, highlightColor);
                if (material.HasProperty(EmissionColorId))
                    highlightBlock.SetColor(EmissionColorId, highlightColor * 3f);

                renderer.SetPropertyBlock(highlightBlock, slot);
            }
        }
    }

    private void ApplyHoverMaterial()
    {
        if (hoverMaterialInstance == null)
            hoverMaterialInstance = new Material(hoverMaterial);

        int colorId = hoverMaterialInstance.HasProperty(BaseColorId) ? BaseColorId : ColorId;
        if (hoverMaterialInstance.HasProperty(colorId))
            hoverMaterialInstance.SetColor(colorId, highlightColor);

        if (hoverMaterialInstance.HasProperty(EmissionColorId))
            hoverMaterialInstance.SetColor(EmissionColorId, highlightColor * 3f);

        foreach (Renderer renderer in highlightRenderers)
        {
            if (renderer == null || originalMaterials.ContainsKey(renderer))
                continue;

            Material[] materials = renderer.sharedMaterials;
            originalMaterials.Add(renderer, materials);

            var highlightedMaterials = new Material[materials.Length + 1];
            Array.Copy(materials, highlightedMaterials, materials.Length);
            highlightedMaterials[materials.Length] = hoverMaterialInstance;
            renderer.sharedMaterials = highlightedMaterials;
        }
    }

    private void OnDisable()
    {
        Unhover();
    }

    private void OnDestroy()
    {
        if (hoverMaterialInstance != null)
            Destroy(hoverMaterialInstance);
    }
}
