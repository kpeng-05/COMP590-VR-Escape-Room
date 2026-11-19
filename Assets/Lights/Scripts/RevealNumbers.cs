using UnityEngine;

/// <summary>
/// Light puzzle: when every lamp under this object is off, the digits appear and glow.
/// Relighting any lamp hides them again. Lamps call UpdateNumbersVisibility() via ToggleableLight.
/// </summary>
public class RevealNumbersWhenDark : MonoBehaviour
{
    [SerializeField] private GameObject numbers;
    [SerializeField] private NumberGlow numberGlow;
    [SerializeField] private int lightsOnThreshold = 0;

    private Light[] childLights;

    private void Awake()
    {
        childLights = GetComponentsInChildren<Light>(true);
        if (numberGlow == null && numbers != null) numberGlow = numbers.GetComponent<NumberGlow>();
    }

    private void Start()
    {
        EnableLights();
    }

    // Called by ToggleableLight whenever a lamp changes.
    public void UpdateNumbersVisibility()
    {
        int lightsOn = 0;

        foreach (Light light in childLights)
        {
            if (light.enabled && light.gameObject.activeInHierarchy)
            {
                lightsOn++;
            }
        }

        bool dark = lightsOn <= lightsOnThreshold;
        if (numberGlow != null) numberGlow.SetGlow(dark);
        if (numbers != null) numbers.SetActive(dark);
    }

    public void EnableLights()
    {
        foreach (Light light in childLights)
        {
            light.gameObject.SetActive(true);
            light.enabled = true;
        }

        UpdateNumbersVisibility();
    }
}
