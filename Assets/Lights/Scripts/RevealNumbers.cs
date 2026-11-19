using UnityEngine;

public class RevealNumbersWhenDark : MonoBehaviour
{
    [SerializeField] private GameObject numbers;
    [SerializeField] private int lightsOnThreshold = 0;

    private Light[] childLights;

    private void Start()
    {
        foreach (Light light in childLights)
        {
            light.gameObject.SetActive(true);
            light.enabled = true;
        }

        UpdateNumbersVisibility();
    }

    private void Awake()
    {
        childLights = GetComponentsInChildren<Light>(true);
        UpdateNumbersVisibility();
    }

    // Call this whenever the room lights change.
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

        numbers.SetActive(lightsOn <= lightsOnThreshold);
    }
}