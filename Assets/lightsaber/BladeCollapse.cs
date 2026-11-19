using System;
using UnityEngine;

public class BladeCollapse : MonoBehaviour
{
    public float speed = 0.1f;
    bool isOn;
    bool wantOn;
    float minScale = 0;
    float maxScale;
    float lerp;
    float scale;
    float xScale;
    float yScale;
    public GameObject blade;

    public AudioClip lightsaberOn;
    public AudioClip lightsaberOff;
    public AudioSource activationAudio;
    public AudioSource idleAudio;

    /// <summary>True while any part of the blade is extended. Wall tiles only cut when this is true.</summary>
    public bool IsOn => isOn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        xScale = transform.localScale.x;
        yScale = transform.localScale.z;
        isOn = false;
        wantOn = false;
        maxScale = transform.localScale.y;
        scale = minScale;
        lerp = -Math.Abs(maxScale / speed);
        idleAudio.volume = 0.3f;
    }

    /// <summary>Ignite or retract the blade. Called by the lightsaber's PointerTool (trigger) or hilt button.</summary>
    public void Toggle()
    {
        SetOn(!wantOn);
    }

    public void SetOn(bool on)
    {
        if (on == wantOn) return;
        wantOn = on;
        lerp = on ? Math.Abs(lerp) : -Math.Abs(lerp);
        activationAudio.PlayOneShot(on ? lightsaberOn : lightsaberOff);
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        // Keyboard shortcut for testing in the editor only.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Toggle();
        }
#endif

        if (isOn && !activationAudio.isPlaying && !idleAudio.isPlaying)
        {
            idleAudio.Play();
        }
        else if (!isOn)
        {
            idleAudio.Stop();
        }

        scale += lerp * Time.deltaTime;
        scale = Math.Clamp(scale, minScale, maxScale);
        transform.localScale = new Vector3(xScale, scale, yScale);
        isOn = scale > 0;
        if (isOn && !blade.activeSelf)
        {
            blade.SetActive(true);
        }
        if (!isOn && blade.activeSelf)
        {
            blade.SetActive(false);
        }
    }
}
