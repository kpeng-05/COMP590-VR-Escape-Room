using TMPro;
using UnityEngine;

/// <summary>
/// Game time limit. Counts down from timeLimitSeconds once the player is in the room and ends the game
/// (GameFlow.TimeUp) at 0:00. The remaining time is shown as red m:ss text on a head-locked HUD;
/// the HUD is visible only while playing. No other visual effects.
/// </summary>
public class GameTimer : MonoBehaviour
{
    [SerializeField] private GameFlow gameFlow;
    [SerializeField] private float timeLimitSeconds = 600f;
    [Tooltip("Root of the HUD canvas (hidden outside play).")]
    [SerializeField] private GameObject hud;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color32 color = new Color32(255, 0, 0, 255);

    private int _shownSeconds = -1;
    private bool _expired;

    public float TimeLimitSeconds => timeLimitSeconds;

    public float Remaining
    {
        get
        {
            if (gameFlow == null || gameFlow.Current == GameFlow.State.Menu) return timeLimitSeconds;
            float elapsed = gameFlow.Current == GameFlow.State.Ended ? gameFlow.ElapsedSeconds : Time.time - gameFlow.RunStartTime;
            return Mathf.Max(0f, timeLimitSeconds - elapsed);
        }
    }

    private void Awake()
    {
        if (gameFlow == null) gameFlow = GetComponent<GameFlow>();
        if (label != null) label.color = color;
        SetHudVisible(false);
    }

    private void Update()
    {
        bool playing = gameFlow != null && gameFlow.Current == GameFlow.State.Playing;
        SetHudVisible(playing);
        if (!playing) return;

        float remaining = Remaining;
        int seconds = Mathf.CeilToInt(remaining);
        if (seconds != _shownSeconds && label != null)
        {
            _shownSeconds = seconds;
            label.text = $"{seconds / 60}:{seconds % 60:00}";
        }

        if (remaining <= 0f && !_expired)
        {
            _expired = true;
            if (label != null) label.text = "0:00";
            gameFlow.TimeUp();
        }
    }

    private void SetHudVisible(bool visible)
    {
        if (hud != null && hud.activeSelf != visible) hud.SetActive(visible);
    }
}
