using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decides which achievements the player earned in this run. Evaluated once when the game ends.
/// Achievements are per run only (nothing is saved).
/// </summary>
public class AchievementTracker : MonoBehaviour
{
    public const string Speedrunner = "Speedrunner";
    public const string GreatSun = "The Great Sun in the Sky";
    public const string YiLongMa = "Yi Long Ma";

    [SerializeField] private VerifySong robotPuzzle;
    [SerializeField] private float speedrunSeconds = 180f;

    private readonly List<string> _earned = new List<string>();

    public IReadOnlyList<string> Earned => _earned;

    private void Awake()
    {
        if (robotPuzzle == null) robotPuzzle = FindAnyObjectByType<VerifySong>(FindObjectsInactive.Include);
    }

    public IList<string> Evaluate(GameFlow.EndReason reason, float elapsedSeconds)
    {
        _earned.Clear();

        // Complete the game in less than 3 minutes.
        if (reason == GameFlow.EndReason.Escaped && elapsedSeconds < speedrunSeconds) _earned.Add(Speedrunner);

        // Fail to break out of the room in 10 minutes.
        if (reason == GameFlow.EndReason.TimeUp) _earned.Add(GreatSun);

        // Complete the robot puzzle in 1 attempt (no wrong robot hit at any point).
        if (robotPuzzle != null && robotPuzzle.Solved && robotPuzzle.SolvedFirstTry) _earned.Add(YiLongMa);

        Debug.Log($"[Achievements] {reason} after {elapsedSeconds:0.0} s: " + (_earned.Count > 0 ? string.Join(", ", _earned) : "none"));
        return new List<string>(_earned);
    }
}
