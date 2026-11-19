using UnityEngine;

/// <summary>
/// A wall lamp the wand can switch on and off. Lives on the lamp root next to its collider.
/// Tells the light puzzle (RevealNumbersWhenDark on "Lights") every time it changes.
/// </summary>
public class ToggleableLight : MonoBehaviour, IPointerTarget
{
    [SerializeField] private Light targetLight;
    [SerializeField] private RevealNumbersWhenDark puzzle;

    public bool IsOn => targetLight != null && targetLight.enabled;

    private void Awake()
    {
        if (targetLight == null) targetLight = GetComponentInChildren<Light>(true);
        if (puzzle == null) puzzle = GetComponentInParent<RevealNumbersWhenDark>();
    }

    public void OnPointerHit(PointerTool tool, RaycastHit hit)
    {
        Toggle();
    }

    public void Toggle()
    {
        if (targetLight == null) return;
        targetLight.enabled = !targetLight.enabled;
        if (puzzle != null) puzzle.UpdateNumbersVisibility();
    }
}
