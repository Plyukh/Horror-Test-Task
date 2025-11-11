using UnityEngine;

/// <summary>
/// Component that controls a flashlight with lights and audio feedback
/// </summary>
public class Flashlight : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Lights to toggle (e.g., Spot light for flashlight)")]
    [SerializeField] private Light[] lights;

    [Header("Audio")]
    [Tooltip("Audio Source for toggle sound")]
    [SerializeField] private AudioSource audioSource;

    [Header("Settings")]
    [Tooltip("Start switched on")]
    [SerializeField] private bool onAtStart = false;

    private bool isOn = false;

    private void Awake()
    {
        isOn = onAtStart;
        ApplyState();
    }

    public void Toggle()
    {
        isOn = !isOn;
        ApplyState();
        PlayToggleSound();
    }

    public void SetState(bool on)
    {
        if (isOn == on) return;
        isOn = on;
        ApplyState();
    }

    public bool IsOn() => isOn;

    private void ApplyState()
    {
        if (lights == null) return;

        foreach (var light in lights)
        {
            if (light != null)
            {
                light.enabled = isOn;
            }
        }
    }

    private void PlayToggleSound()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
}
