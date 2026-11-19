using Oculus.Interaction.Locomotion;
using UnityEngine;

/// <summary>
/// After the first wall breach, walking more than exitDistance from the room centre (feet, horizontal)
/// shows "Leave the room?". "Keep exploring" hides it until the player comes back within rearmDistance.
/// </summary>
public class ExitMonitor : MonoBehaviour
{
    [SerializeField] private WallBreachTracker walls;
    [SerializeField] private MenuPanel leavePrompt;
    [SerializeField] private GameFlow gameFlow;
    [SerializeField] private Vector3 roomCenter = Vector3.zero;
    [SerializeField] private float exitDistance = 12f;
    [SerializeField] private float rearmDistance = 9f;
    [SerializeField] private float promptDistance = 1.5f;

    private Transform _feet;
    private Transform _head;
    private bool _armed = true;
    private bool _open;

    private void Start()
    {
        FirstPersonLocomotor locomotor = FindFirstObjectByType<FirstPersonLocomotor>();
        _feet = locomotor != null ? locomotor.transform : null;
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        _head = rig != null ? rig.centerEyeAnchor : null;
        if (_feet == null && rig != null) _feet = rig.transform;
        if (leavePrompt != null) leavePrompt.Hide();
    }

    private void Update()
    {
        if (_feet == null || gameFlow == null || gameFlow.Current != GameFlow.State.Playing) return;
        if (walls == null || !walls.Breached) return;

        Vector3 offset = _feet.position - roomCenter;
        offset.y = 0f;
        float distance = offset.magnitude;

        if (!_armed && distance < rearmDistance) _armed = true;

        if (_armed && !_open && distance > exitDistance)
        {
            _open = true;
            if (leavePrompt != null) leavePrompt.ShowInFrontOf(_head != null ? _head : _feet, promptDistance);
        }
    }

    // Prompt "Leave".
    public void Leave()
    {
        _open = false;
        if (leavePrompt != null) leavePrompt.Hide();
        gameFlow.Win();
    }

    // Prompt "Keep exploring".
    public void KeepExploring()
    {
        _open = false;
        _armed = false;
        if (leavePrompt != null) leavePrompt.Hide();
    }
}
