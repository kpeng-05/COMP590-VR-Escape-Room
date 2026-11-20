using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A robot the conductor baton can point at. Every hit (even mid-song) restarts its clip
/// and reports this robot's Unity "order" number to the code controller (VerifySong).
/// </summary>
public class RobotSing : MonoBehaviour, IPointerTarget
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private int order;

    public UnityEvent<int> robotPlayed;
    public int Order => order;

    private void Awake()
    {
        audioSource.playOnAwake = false;
    }

    public void OnPointerHit(PointerTool tool, RaycastHit hit)
    {
        Sing();
    }

    public void Sing()
    {
        if (audioSource != null && audioClip != null)
        {
            audioSource.PlayOneShot(audioClip, 2.0f);
        }
        robotPlayed?.Invoke(order);
    }
}
