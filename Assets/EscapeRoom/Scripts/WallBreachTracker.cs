using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Counts cut wall tiles. Raises onFirstBreach (unlocks outdoor teleport) and
/// onAllWallsDown (ends the game) once the cut fraction reaches allDownFraction.
/// </summary>
public class WallBreachTracker : MonoBehaviour
{
    [SerializeField, Range(0.5f, 1f)] private float allDownFraction = 0.95f;
    [SerializeField] private int maxFallingDebris = 40;
    [SerializeField] private UnityEvent onFirstBreach = new UnityEvent();
    [SerializeField] private UnityEvent onAllWallsDown = new UnityEvent();

    private int _total;
    private int _cut;
    private int _falling;
    private bool _breached;
    private bool _allDown;

    public bool Breached => _breached;
    public bool AllDown => _allDown;
    public float CutFraction => _total == 0 ? 0f : (float)_cut / _total;
    public UnityEvent OnFirstBreach => onFirstBreach;
    public UnityEvent OnAllWallsDown => onAllWallsDown;

    private void Start()
    {
        _total = 0;
        foreach (WallTile tile in GetComponentsInChildren<WallTile>(true))
        {
            if (tile.Cuttable) _total++;
        }
    }

    public void ReportCut(WallTile tile)
    {
        _cut++;
        if (!_breached)
        {
            _breached = true;
            onFirstBreach?.Invoke();
        }
        if (!_allDown && CutFraction >= allDownFraction)
        {
            _allDown = true;
            onAllWallsDown?.Invoke();
        }
    }

    public bool TryBeginDebris()
    {
        if (_falling >= maxFallingDebris) return false;
        _falling++;
        return true;
    }

    public void EndDebris()
    {
        _falling = Mathf.Max(0, _falling - 1);
    }
}
