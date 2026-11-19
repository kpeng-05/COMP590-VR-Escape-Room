using UnityEngine;

public class NumberGlow : MonoBehaviour
{
    [SerializeField] private Renderer[] numberRenderers;
    [SerializeField] private Color glowColor = Color.cyan;
    [SerializeField, Min(0f)] private float glowIntensity = 3f;

    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    public void SetGlow(bool active)
    {
        foreach (Renderer numberRenderer in numberRenderers)
        {
            if (numberRenderer == null) continue;

            numberRenderer.material.EnableKeyword("_EMISSION");
            numberRenderer.material.SetColor(
                EmissionColor,
                active ? glowColor * glowIntensity : Color.black
            );
        }
    }
}