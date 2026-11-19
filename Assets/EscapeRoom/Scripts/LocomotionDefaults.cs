using System.Collections;
using Oculus.Interaction.Locomotion;
using UnityEngine;

/// <summary>
/// Forces the Interaction SDK locomotion settings this game needs at runtime:
/// controller movement = Teleport (the Locomotor prefab defaults to Slide, which deactivates the
/// controller teleport interactors, so the arc never appears), and comfort vignettes Off.
/// ReactiveValue only notifies on change, so each value is flipped once to make the SDK apply it.
/// </summary>
public class LocomotionDefaults : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return null; // after the SDK components have started

        foreach (MovingSetting moving in FindObjectsByType<MovingSetting>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            moving.ControllerMovement.Value = MovingSetting.MovementStyle.Slide;
            moving.ControllerMovement.Value = MovingSetting.MovementStyle.Teleport;
        }

        foreach (ComfortVignetteSetting comfort in FindObjectsByType<ComfortVignetteSetting>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            comfort.ComfortLevel.Value = ComfortVignetteSetting.ComfortAssistance.Low;
            comfort.ComfortLevel.Value = ComfortVignetteSetting.ComfortAssistance.Off;
        }
    }
}
