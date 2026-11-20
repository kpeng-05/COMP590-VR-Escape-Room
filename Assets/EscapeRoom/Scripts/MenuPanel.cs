using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// World-space menu: orbit-camera feed behind a translucent gray filter, a title and two buttons.
/// The same component is used for the start menu, the end menu and the leave prompt.
/// Buttons are pressed with the rig's ray interactors (RayInteractable on each button).
/// </summary>
public class MenuPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text subtitle;
    [SerializeField] private TMP_Text primaryLabel;
    [SerializeField] private TMP_Text secondaryLabel;
    [Tooltip("Optional list of earned achievements (end menu only).")]
    [SerializeField] private TMP_Text achievementsText;
    [SerializeField] private MenuOrbitCamera orbitCamera;
    [SerializeField] private UnityEvent onPrimary = new UnityEvent();
    [SerializeField] private UnityEvent onSecondary = new UnityEvent();

    public UnityEvent OnPrimary => onPrimary;
    public UnityEvent OnSecondary => onSecondary;

    private float _shownAt;

    public void Bind(TMP_Text titleText, TMP_Text subtitleText, TMP_Text primary, TMP_Text secondary, MenuOrbitCamera orbit)
    {
        title = titleText;
        subtitle = subtitleText;
        primaryLabel = primary;
        secondaryLabel = secondary;
        orbitCamera = orbit;
    }

    public void BindAchievements(TMP_Text text)
    {
        achievementsText = text;
    }

    public void SetTitle(string text)
    {
        if (title != null && !string.IsNullOrEmpty(text)) title.text = text;
    }

    /// <summary>Shows "Achievements" and one earned title per line; hides the block when the list is empty.</summary>
    public void SetAchievements(IList<string> earned)
    {
        if (achievementsText == null) return;
        bool any = earned != null && earned.Count > 0;
        achievementsText.gameObject.SetActive(any);
        if (any) achievementsText.text = "Achievements\n" + string.Join("\n", earned);
    }

    public void SetSubtitle(string text)
    {
        if (subtitle == null) return;
        subtitle.text = text;
        subtitle.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }

    public void Show()
    {
        gameObject.SetActive(true);
        _shownAt = Time.unscaledTime;
        if (orbitCamera != null) orbitCamera.SetRunning(true);
    }

    /// <summary>Places the panel in front of the head (fixed afterwards), then shows it.</summary>
    public void ShowInFrontOf(Transform head, float distance)
    {
        Vector3 forward = head.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
        forward.Normalize();
        transform.position = head.position + forward * distance;
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        Show();
    }

    public void Hide()
    {
        if (orbitCamera != null) orbitCamera.SetRunning(false);
        gameObject.SetActive(false);
    }

    // Called by the buttons' InteractableUnityEventWrapper.WhenSelect.
    public void PressPrimary()
    {
        if (!Ready()) return;
        onPrimary?.Invoke();
    }

    public void PressSecondary()
    {
        if (!Ready()) return;
        onSecondary?.Invoke();
    }

    // Ignore a press carried over from the action that opened the panel.
    private bool Ready() => gameObject.activeInHierarchy && Time.unscaledTime - _shownAt > 0.3f;
}
