using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Implemented by anything a PointerTool ray can affect (lamps, robots).
/// </summary>
public interface IPointerTarget
{
    void OnPointerHit(PointerTool tool, RaycastHit hit);
}

/// <summary>
/// Shared "held tool" behaviour for the wand, the baton and the lightsaber.
/// While the object is grabbed, pressing the index trigger of the holding controller
/// (or pinching with the holding hand when hand tracking is used) fires once.
/// Raycast mode: casts one invisible ray from <see cref="tip"/> and notifies the first IPointerTarget hit.
/// Activate mode: just raises <see cref="onFired"/> (used to toggle the lightsaber).
/// No visuals and no haptics.
/// </summary>
public class PointerTool : MonoBehaviour
{
    public enum Mode { Raycast, Activate }

    [SerializeField] private Mode mode = Mode.Raycast;
    [Tooltip("Ray origin. The ray travels along this transform's forward axis.")]
    [SerializeField] private Transform tip;
    [SerializeField] private float maxDistance = 25f;
    [Tooltip("Layers that hold valid targets (Lamp for the wand, Robot for the baton).")]
    [SerializeField] private LayerMask targetMask;
    [Tooltip("Layers that block the ray without being targets (walls, furniture).")]
    [SerializeField] private LayerMask occluderMask = 1 | (1 << 3);
    [SerializeField] private UnityEvent onFired = new UnityEvent();

    public UnityEvent OnFired => onFired;

    private readonly List<IInteractableView> _views = new List<IInteractableView>();
    private readonly RaycastHit[] _hits = new RaycastHit[16];
    private Rigidbody _rigidbody;
    private OVRCameraRig _rig;
    private IHand[] _hands = new IHand[0];
    private readonly Dictionary<IHand, bool> _wasPinching = new Dictionary<IHand, bool>();
    private float _nextHandRefresh;
    private int _lastFireFrame = -1;

    public bool IsHeld
    {
        get
        {
            foreach (IInteractableView view in _views)
            {
                if (view != null && view.State == InteractableState.Select) return true;
            }
            return false;
        }
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        foreach (MonoBehaviour mb in GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb is IInteractableView view) _views.Add(view);
        }
        if (tip == null) tip = transform;
    }

    private void Start()
    {
        _rig = FindFirstObjectByType<OVRCameraRig>();
    }

    private void Update()
    {
        if (!IsHeld) return;

        Handedness hand = HoldingHand();
        OVRInput.Controller controller = hand == Handedness.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        bool pressed = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller);
        pressed |= HandPinchStarted(hand);
#if UNITY_EDITOR
        pressed |= Input.GetKeyDown(KeyCode.F);
#endif
        if (pressed) Fire();
    }

    /// <summary>Fires the tool once (also callable from UnityEvents or tests).</summary>
    public void Fire()
    {
        if (_lastFireFrame == Time.frameCount) return; // controller + synthetic hand in the same frame
        _lastFireFrame = Time.frameCount;

        if (mode == Mode.Raycast) CastRay();
        onFired?.Invoke();
    }

    private void CastRay()
    {
        Ray ray = new Ray(tip.position, tip.forward);
        int count = Physics.RaycastNonAlloc(ray, _hits, maxDistance, targetMask | occluderMask, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        int bestIndex = -1;
        for (int i = 0; i < count; i++)
        {
            Collider c = _hits[i].collider;
            if (_rigidbody != null && c.attachedRigidbody == _rigidbody) continue; // ignore the tool itself
            if (_hits[i].distance < best)
            {
                best = _hits[i].distance;
                bestIndex = i;
            }
        }
        if (bestIndex < 0) return;

        RaycastHit hit = _hits[bestIndex];
        if ((targetMask.value & (1 << hit.collider.gameObject.layer)) == 0) return; // blocked by an occluder

        IPointerTarget target = hit.collider.GetComponentInParent<IPointerTarget>();
        target?.OnPointerHit(this, hit);
    }

    private Handedness HoldingHand()
    {
        if (_rig == null) _rig = FindFirstObjectByType<OVRCameraRig>();
        if (_rig == null) return Handedness.Right;
        float left = Vector3.Distance(transform.position, _rig.leftHandAnchor.position);
        float right = Vector3.Distance(transform.position, _rig.rightHandAnchor.position);
        return left < right ? Handedness.Left : Handedness.Right;
    }

    private bool HandPinchStarted(Handedness handedness)
    {
        if (Time.unscaledTime >= _nextHandRefresh)
        {
            _nextHandRefresh = Time.unscaledTime + 2f;
            _hands = FindObjectsByType<Hand>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        bool started = false;
        foreach (IHand hand in _hands)
        {
            if (hand == null || !hand.IsConnected || hand.Handedness != handedness) continue;
            // Only real tracked hands; controller-driven hands are covered by the trigger above.
            if (OVRInput.IsControllerConnected(handedness == Handedness.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch)) continue;
            bool pinching = hand.GetIndexFingerIsPinching();
            _wasPinching.TryGetValue(hand, out bool was);
            if (pinching && !was) started = true;
            _wasPinching[hand] = pinching;
        }
        return started;
    }

    private void OnDrawGizmosSelected()
    {
        Transform t = tip != null ? tip : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(t.position, t.position + t.forward * 0.5f);
    }
}
