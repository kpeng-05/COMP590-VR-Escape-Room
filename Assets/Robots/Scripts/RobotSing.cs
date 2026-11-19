using System;
using UnityEngine;
using UnityEngine.Events;

public class RobotSing : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private int order;

    public UnityEvent<int> robotPlayed;
    private bool shouldSing;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        shouldSing = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (shouldSing && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(audioClip);
            shouldSing = false;
            robotPlayed?.Invoke(order);
        }
    }
}
