using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Widens the player's grab reach by scaling the trigger volumes of every grab interactor in the rig
/// (controller GrabInteractor and hand / controller-driven HandGrabInteractor, including grip and pinch volumes).
/// The SDK defaults are about 5 cm; rangeMultiplier 1.6 makes that about 8 cm.
/// Creates itself in every loaded scene if none is placed, so no scene setup is needed.
/// </summary>
public class GrabRangeBoost : MonoBehaviour
{
    [Tooltip("Scale applied once to each grab interactor trigger volume. 1 = SDK default.")]
    [SerializeField] private float rangeMultiplier = 1.6f;

    private static readonly FieldInfo GripField = typeof(HandGrabInteractor).GetField("_gripCollider", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo PinchField = typeof(HandGrabInteractor).GetField("_pinchCollider", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly HashSet<Collider> _scaled = new HashSet<Collider>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInScene();
        SceneManager.sceneLoaded += (scene, mode) => EnsureInScene();
    }

    private static void EnsureInScene()
    {
        if (FindAnyObjectByType<GrabRangeBoost>(FindObjectsInactive.Include) != null) return;
        new GameObject(nameof(GrabRangeBoost)).AddComponent<GrabRangeBoost>();
    }

    private IEnumerator Start()
    {
        yield return null; // after the rig's interactors have started
        Apply();
    }

    private void Apply()
    {
        if (Mathf.Approximately(rangeMultiplier, 1f)) return;

        foreach (GrabInteractor grab in FindObjectsByType<GrabInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            ScaleTriggers(grab.Rigidbody);
        }

        foreach (HandGrabInteractor handGrab in FindObjectsByType<HandGrabInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            ScaleTriggers(handGrab.Rigidbody);
            Scale(GripField?.GetValue(handGrab) as Collider);
            Scale(PinchField?.GetValue(handGrab) as Collider);
        }
    }

    private void ScaleTriggers(Rigidbody body)
    {
        if (body == null) return;
        foreach (Collider c in body.GetComponentsInChildren<Collider>(true))
        {
            if (c.isTrigger) Scale(c);
        }
    }

    private void Scale(Collider c)
    {
        if (c == null || !_scaled.Add(c)) return;
        switch (c)
        {
            case SphereCollider sphere:
                sphere.radius *= rangeMultiplier;
                break;
            case CapsuleCollider capsule:
                capsule.radius *= rangeMultiplier;
                capsule.height *= rangeMultiplier;
                break;
            case BoxCollider box:
                box.size *= rangeMultiplier;
                break;
        }
    }
}
