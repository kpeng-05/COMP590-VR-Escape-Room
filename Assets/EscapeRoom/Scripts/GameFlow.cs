using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Locomotion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu -> Playing -> Ended. The player starts in the lobby with the start menu.
/// Play moves the player into the room; the game ends when the player escapes (leave prompt or all walls down)
/// or the time limit runs out (GameTimer). The end menu offers Play Again (reload, skip menu) and Quit,
/// and lists the achievements earned in the run.
/// </summary>
public class GameFlow : MonoBehaviour
{
    public enum State { Menu, Playing, Ended }
    public enum EndReason { Escaped, TimeUp }

    /// <summary>Set by Play Again so the reloaded scene goes straight into play.</summary>
    public static bool SkipStartMenu;

    [SerializeField] private MenuPanel startMenu;
    [SerializeField] private MenuPanel endMenu;
    [SerializeField] private Transform roomStart;
    [SerializeField] private WallBreachTracker walls;
    [SerializeField] private float endMenuDistance = 2f;
    [SerializeField] private float allWallsDownDelay = 2f;
    [SerializeField] private ExitMonitor exitMonitor;
    [SerializeField] private AchievementTracker achievements;
    [SerializeField] private string escapedTitle = "You Win";
    [SerializeField] private string timeUpTitle = "Time's Up";
    [SerializeField] private UnityEvent<EndReason> onGameEnded = new UnityEvent<EndReason>();

    private FirstPersonLocomotor _locomotor;
    private OVRCameraRig _rig;
    private State _state = State.Menu;

    public State Current => _state;
    public EndReason Reason { get; private set; }
    /// <summary>Time.time when the player was moved into the room.</summary>
    public float RunStartTime { get; private set; }
    /// <summary>Seconds from entering the room to the end of the game (set when the game ends).</summary>
    public float ElapsedSeconds { get; private set; }
    public UnityEvent<EndReason> OnGameEnded => onGameEnded;

    private void Start()
    {
        _locomotor = FindFirstObjectByType<FirstPersonLocomotor>();
        _rig = FindFirstObjectByType<OVRCameraRig>();

        if (endMenu != null) endMenu.Hide();
        if (walls != null) walls.OnAllWallsDown.AddListener(OnAllWallsDown);

        if (SkipStartMenu)
        {
            SkipStartMenu = false;
            if (startMenu != null) startMenu.Hide();
            StartCoroutine(EnterRoomNextFrame());
        }
        else if (startMenu != null)
        {
            startMenu.Show();
        }
    }

    // Start menu "Play".
    public void Play()
    {
        if (_state != State.Menu) return;
        if (startMenu != null) startMenu.Hide();
        StartCoroutine(EnterRoomNextFrame());
    }

    private IEnumerator EnterRoomNextFrame()
    {
        // Let the locomotor finish its own Start before we move the player.
        yield return null;
        MovePlayerTo(roomStart);
        RunStartTime = Time.time;
        _state = State.Playing;
    }

    private void MovePlayerTo(Transform target)
    {
        if (target == null) return;
        if (_locomotor != null)
        {
            var pose = new Pose(target.position, target.rotation);
            _locomotor.HandleLocomotionEvent(new LocomotionEvent(0, pose,
                LocomotionEvent.TranslationType.Absolute, LocomotionEvent.RotationType.Absolute));
        }
        else if (_rig != null)
        {
            _rig.transform.SetPositionAndRotation(target.position, target.rotation);
        }
    }

    private void OnAllWallsDown()
    {
        if (_state == State.Playing) StartCoroutine(WinAfter(allWallsDownDelay));
    }

    private IEnumerator WinAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        Win();
    }

    // Leave prompt "Leave", or all walls down.
    public void Win() => EndGame(EndReason.Escaped);

    // GameTimer at 0:00.
    public void TimeUp() => EndGame(EndReason.TimeUp);

    public void EndGame(EndReason reason)
    {
        if (_state != State.Playing) return;
        _state = State.Ended;
        Reason = reason;
        ElapsedSeconds = Time.time - RunStartTime;

        if (exitMonitor != null) exitMonitor.HidePrompt();

        foreach (TeleportInteractable t in FindObjectsByType<TeleportInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            t.enabled = false;
        }

        IList<string> earned = achievements != null ? achievements.Evaluate(reason, ElapsedSeconds) : new List<string>();
        onGameEnded?.Invoke(reason);

        if (endMenu == null) return;
        endMenu.SetTitle(reason == EndReason.TimeUp ? timeUpTitle : escapedTitle);
        endMenu.SetAchievements(earned);
        if (walls != null) endMenu.SetSubtitle($"Walls cut: {Mathf.RoundToInt(walls.CutFraction * 100f)}%");
        Transform head = _rig != null ? _rig.centerEyeAnchor : Camera.main != null ? Camera.main.transform : transform;
        endMenu.ShowInFrontOf(head, endMenuDistance);
    }

    // End menu "Play Again".
    public void PlayAgain()
    {
        SkipStartMenu = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Both menus "Quit".
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
