using UnityEngine;

public class HighlightObject : MonoBehaviour
{
    [SerializeField] private Color hoverColor = new Color(1f, 0.82f, 0.25f, 1f);

    private Renderer[] renderers;
    private Material[][] materials;
    private Color[][] originalColors;
    private bool isHovered;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        materials = new Material[renderers.Length][];
        originalColors = new Color[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            materials[i] = renderers[i].materials;
            originalColors[i] = new Color[materials[i].Length];

            for (int j = 0; j < materials[i].Length; j++)
                originalColors[i][j] = GetMaterialColor(materials[i][j]);
        }
    }

    public void SetHover(bool hover)
    {
        if (isHovered == hover)
            return;

        isHovered = hover;

        for (int i = 0; i < materials.Length; i++)
        {
            for (int j = 0; j < materials[i].Length; j++)
            {
                SetMaterialColor(materials[i][j], hover ? hoverColor : originalColors[i][j]);
            }
        }
    }

    private Color GetMaterialColor(Material material)
    {
        if (material.HasProperty("_BaseColor"))
            return material.GetColor("_BaseColor");

        if (material.HasProperty("_Color"))
            return material.GetColor("_Color");

        return Color.white;
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
            return;
        }

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }
}
