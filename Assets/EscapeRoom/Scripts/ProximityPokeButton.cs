using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A small physical button pressed by any Interaction SDK poke tip (hand index finger or controller tip).
/// Fires once when a poke tip enters <see cref="pressRadius"/> and re-arms after it leaves <see cref="releaseRadius"/>.
/// Used for the safe keypad and for the lightsaber hilt button (hand tracking).
/// </summary>
public class ProximityPokeButton : MonoBehaviour
{
    [SerializeField] private Vector3 localCenter = Vector3.zero;
    [SerializeField] private float pressRadius = 0.015f;
    [SerializeField] private float releaseRadius = 0.03f;
    [Tooltip("Optional: only active while this tool is held (lightsaber hilt button).")]
    [SerializeField] private PointerTool requireHeld;
    [SerializeField] private UnityEvent onPressed = new UnityEvent();

    public UnityEvent OnPressed => onPressed;

    private static readonly List<PokeInteractor> Pokers = new List<PokeInteractor>();
    private static float _nextRefresh;
    private readonly Dictionary<PokeInteractor, bool> _inside = new Dictionary<PokeInteractor, bool>();
    private bool _wasActive;

    public void Configure(Vector3 center, float press, float release)
    {
        localCenter = center;
        pressRadius = press;
        releaseRadius = Mathf.Max(release, press * 1.5f);
    }

    private static void RefreshPokers()
    {
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + 1f;
        Pokers.Clear();
        Pokers.AddRange(FindObjectsByType<PokeInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
    }

    private void Update()
    {
        bool active = requireHeld == null || requireHeld.IsHeld;
        if (!active)
        {
            _wasActive = false;
            return;
        }

        RefreshPokers();
        Vector3 center = transform.TransformPoint(localCenter);
        bool justActivated = !_wasActive;
        _wasActive = true;

        foreach (PokeInteractor poker in Pokers)
        {
            if (poker == null || !poker.isActiveAndEnabled) continue;
            float d = Vector3.Distance(poker.Origin, center);
            bool known = _inside.TryGetValue(poker, out bool inside);

            if (!known || justActivated)
            {
                // A tip that is already close when we start watching (e.g. the hand holding the
                // lightsaber) must leave first before it can press.
                _inside[poker] = d < releaseRadius;
                continue;
            }

            if (!inside && d < pressRadius)
            {
                _inside[poker] = true;
                onPressed?.Invoke();
            }
            else if (inside && d > releaseRadius)
            {
                _inside[poker] = false;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 c = transform.TransformPoint(localCenter);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(c, pressRadius);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(c, releaseRadius);
    }
}
