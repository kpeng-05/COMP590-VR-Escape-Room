using UnityEngine;

/// <summary>
/// Briefly shows a pointer ray as a thin white line, fixed in the world where it was fired.
/// PointerTool calls Show() each time it casts.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RayFlash : MonoBehaviour
{
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private float width = 0.003f;
    [SerializeField] private Color color = Color.white;

    private LineRenderer _line;
    private float _hideAt;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.positionCount = 2;
        _line.startWidth = _line.endWidth = width;
        _line.startColor = _line.endColor = color;
        _line.numCapVertices = 0;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;
        _line.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        _line.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        if (_line.sharedMaterial == null)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit != null)
            {
                var mat = new Material(unlit);
                mat.SetColor("_BaseColor", color);
                _line.sharedMaterial = mat;
            }
        }
        _line.enabled = false;
    }

    public void Show(Vector3 from, Vector3 to)
    {
        if (_line == null) return;
        _line.SetPosition(0, from);
        _line.SetPosition(1, to);
        _line.enabled = true;
        _hideAt = Time.time + duration;
    }

    private void Update()
    {
        if (_line != null && _line.enabled && Time.time >= _hideAt) _line.enabled = false;
    }
}
