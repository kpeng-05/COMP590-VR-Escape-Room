#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Reflection;
using UnityEngine;

/// <summary>
/// TEMPORARY diagnostics for "lightsaber does not spawn". Creates itself at play start (Editor and
/// development builds only) and logs, with the prefix [SpawnDiag]:
/// - every robot song (robot name, order sent) and VerifySong's lastToSing after it;
/// - every change of the lightsaber's active state, with its world position;
/// - the lightsaber's position each second for 5 s after it becomes active, and a warning if it drops below y = -2.
/// Delete this file once the issue is found.
/// </summary>
public class SpawnDiagnostics : MonoBehaviour
{
    private static readonly FieldInfo LastToSing = typeof(VerifySong).GetField("lastToSing", BindingFlags.Instance | BindingFlags.NonPublic);

    private VerifySong _verify;
    private GameObject _saber;
    private bool _wasActive;
    private float _trackUntil;
    private float _nextTrack;
    private bool _warnedFall;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        new GameObject(nameof(SpawnDiagnostics)).AddComponent<SpawnDiagnostics>();
    }

    private void Start()
    {
        _verify = FindAnyObjectByType<VerifySong>(FindObjectsInactive.Include);
        Debug.Log("[SpawnDiag] VerifySong " + (_verify != null ? "found on '" + _verify.name + "', enabled=" + _verify.enabled + ", activeInHierarchy=" + _verify.gameObject.activeInHierarchy : "NOT FOUND"));

        foreach (RobotSing robot in FindObjectsByType<RobotSing>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            RobotSing r = robot;
            int listeners = r.robotPlayed != null ? r.robotPlayed.GetPersistentEventCount() : -1;
            Debug.Log($"[SpawnDiag] Robot '{r.name}' order={r.Order} persistentListeners={listeners}");
            r.robotPlayed?.AddListener(order =>
            {
                // Runs after the persistent listener (VerifySong), so lastToSing is already updated.
                object last = _verify != null && LastToSing != null ? LastToSing.GetValue(_verify) : "?";
                Debug.Log($"[SpawnDiag] '{r.name}' sang, sent order {order}; VerifySong.lastToSing is now {last}");
            });
        }

        BladeCollapse blade = FindAnyObjectByType<BladeCollapse>(FindObjectsInactive.Include);
        if (blade != null)
        {
            Rigidbody rb = blade.GetComponentInParent<Rigidbody>(true);
            _saber = rb != null ? rb.gameObject : blade.transform.root.gameObject;
        }
        if (_saber == null)
        {
            Debug.LogWarning("[SpawnDiag] Lightsaber NOT FOUND");
            return;
        }
        _wasActive = _saber.activeInHierarchy;
        Debug.Log($"[SpawnDiag] Lightsaber '{_saber.name}' at start: activeSelf={_saber.activeSelf} activeInHierarchy={_wasActive} pos={_saber.transform.position}");
        if (_wasActive) _trackUntil = Time.time + 5f;
    }

    private void Update()
    {
        if (_saber == null) return;

        bool active = _saber.activeInHierarchy;
        if (active != _wasActive)
        {
            _wasActive = active;
            Debug.Log($"[SpawnDiag] Lightsaber active changed to {active} at frame {Time.frameCount}, pos={_saber.transform.position}");
            if (active) _trackUntil = Time.time + 5f;
        }

        if (active && Time.time < _trackUntil && Time.time >= _nextTrack)
        {
            _nextTrack = Time.time + 1f;
            Debug.Log($"[SpawnDiag] Lightsaber pos={_saber.transform.position}");
        }

        if (active && !_warnedFall && _saber.transform.position.y < -2f)
        {
            _warnedFall = true;
            Debug.LogWarning($"[SpawnDiag] Lightsaber fell through the floor: pos={_saber.transform.position}");
        }
    }
}
#endif
