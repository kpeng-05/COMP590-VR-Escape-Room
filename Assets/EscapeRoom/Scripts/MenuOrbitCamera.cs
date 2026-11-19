using UnityEngine;

/// <summary>
/// Non-XR camera that circles a point while always looking at it. Renders into a RenderTexture
/// shown on a menu panel (never drives the headset view, for comfort).
/// </summary>
[RequireComponent(typeof(Camera))]
public class MenuOrbitCamera : MonoBehaviour
{
    [SerializeField] private Vector3 center = new Vector3(0f, 1f, 0f);
    [SerializeField] private float radius = 5f;
    [SerializeField] private float height = 3f;
    [SerializeField] private float degreesPerSecond = 6f;

    private Camera _camera;
    private float _angle;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _camera.stereoTargetEye = StereoTargetEyeMask.None;
    }

    public void Configure(Vector3 newCenter, float newRadius, float newHeight)
    {
        center = newCenter;
        radius = newRadius;
        height = newHeight;
    }

    public void SetRunning(bool running)
    {
        if (_camera == null) _camera = GetComponent<Camera>();
        _camera.enabled = running;
        enabled = running;
    }

    private void LateUpdate()
    {
        _angle = (_angle + degreesPerSecond * Time.deltaTime) % 360f;
        float rad = _angle * Mathf.Deg2Rad;
        transform.position = center + new Vector3(Mathf.Cos(rad) * radius, height, Mathf.Sin(rad) * radius);
        transform.LookAt(center);
    }
}
