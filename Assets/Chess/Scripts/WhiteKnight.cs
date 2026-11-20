using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Mate in one: the white knight only counts when it is released on c7.
/// On success it snaps to the square, locks (no more grabbing) and fires onCorrectPlacement once.
/// Anywhere else it simply stays where it falls (no feedback).
/// </summary>
public class WhiteKnight : MonoBehaviour
{
    // World position of the target square (c7 = 3.5625, 1.0, 3.3375).
    [SerializeField] private float targetX = 3.5625f;
    [SerializeField] private float targetY = 1f;
    [SerializeField] private float targetZ = 3.3375f;

    [SerializeField] private UnityEvent onCorrectPlacement;

    [Tooltip("Max horizontal distance from the square centre (half a square is 0.1125 m).")]
    [SerializeField] private float horizontalTolerance = 0.06f;
    [SerializeField] private float verticalTolerance = 0.08f;
    [Tooltip("Dot product between the piece's up axis now and when it spawned.")]
    [SerializeField] private float uprightDot = 0.9f;
    [Tooltip("After release, keep checking this long so a piece dropped onto c7 still counts.")]
    [SerializeField] private float settleWindow = 1.5f;

    private readonly List<IInteractableView> _views = new List<IInteractableView>();
    private Rigidbody _rigidbody;
    private Quaternion _uprightRotation;
    private Vector3 _targetPoint;
    private bool _wasHeld;
    private float _checkUntil = -1f;
    private bool _solved;

    public bool Solved => _solved;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        foreach (MonoBehaviour mb in GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb is IInteractableView view) _views.Add(view);
        }
        gameObject.SetActive(false);
    }

    private void Start()
    {
        _targetPoint = new Vector3(targetX, targetY, targetZ);
        _uprightRotation = transform.rotation;
    }

    private void Update()
    {
        if (_solved) return;

        bool held = IsHeld();
        if (_wasHeld && !held) _checkUntil = Time.time + settleWindow;
        _wasHeld = held;

        if (!held && Time.time <= _checkUntil && IsOnTarget())
        {
            Solve();
        }
    }

    private bool IsHeld()
    {
        foreach (IInteractableView view in _views)
        {
            if (view != null && view.State == InteractableState.Select) return true;
        }
        return false;
    }

    private bool IsOnTarget()
    {
        Vector3 p = transform.position;
        Vector2 flat = new Vector2(p.x - _targetPoint.x, p.z - _targetPoint.z);
        if (flat.magnitude > horizontalTolerance) return false;
        if (Mathf.Abs(p.y - _targetPoint.y) > verticalTolerance) return false;

        Vector3 upNow = transform.rotation * Quaternion.Inverse(_uprightRotation) * Vector3.up;
        return Vector3.Dot(upNow, Vector3.up) >= uprightDot;
    }

    private void Solve()
    {
        _solved = true;

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
        }
        transform.SetPositionAndRotation(_targetPoint, _uprightRotation);

        // Lock: nothing can grab the knight any more.
        foreach (IInteractableView view in _views)
        {
            if (view is MonoBehaviour mb) mb.enabled = false;
        }
        foreach (Grabbable grabbable in GetComponentsInChildren<Grabbable>(true))
        {
            grabbable.enabled = false;
        }

        onCorrectPlacement?.Invoke();
    }
}
