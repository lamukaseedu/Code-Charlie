/*
 * Author: Savio Xavier
 * Created: 9/24/2026
 */

using UnityEngine;

public class InteractableHoverHighlight : MonoBehaviour
{
    [SerializeField] private Renderer highlightRenderer;
    [SerializeField] private Color highlightColor = new(0.15f, 0.8f, 1f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private Color normalColor = Color.white;
    private int colorPropertyId;

    private void Awake()
    {
        if (highlightRenderer == null)
            highlightRenderer = GetComponentInChildren<Renderer>();

        CacheMaterialColor();
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlightRenderer == null || colorPropertyId == 0)
            return;

        propertyBlock ??= new MaterialPropertyBlock();
        highlightRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(colorPropertyId, highlighted ? highlightColor : normalColor);
        highlightRenderer.SetPropertyBlock(propertyBlock);
    }

    private void CacheMaterialColor()
    {
        if (highlightRenderer == null || highlightRenderer.sharedMaterial == null)
            return;

        Material material = highlightRenderer.sharedMaterial;

        if (material.HasProperty(BaseColorId))
            colorPropertyId = BaseColorId;
        else if (material.HasProperty(ColorId))
            colorPropertyId = ColorId;
        else
            return;

        normalColor = material.GetColor(colorPropertyId);
    }
}
