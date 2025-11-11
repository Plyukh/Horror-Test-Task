using UnityEngine;

/// <summary>
/// Controller for radio that can play normal or interference audio
/// </summary>
public class RadioController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip normalClip;
    [SerializeField] private AudioClip interferenceClip;

    private bool isInterference = false;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        PlayNormal();
    }

    public void ToggleInterference()
    {
        if (isInterference)
        {
            PlayNormal();
        }
        else
        {
            PlayInterference();
        }
    }

    public void PlayNormal()
    {
        if (audioSource == null || normalClip == null) return;

        isInterference = false;
        
        if (audioSource.clip == normalClip && audioSource.isPlaying)
        {
            return;
        }

        audioSource.clip = normalClip;
        audioSource.Play();
    }

    public void PlayInterference()
    {
        if (audioSource == null || interferenceClip == null) return;

        isInterference = true;
        
        if (audioSource.clip == interferenceClip && audioSource.isPlaying)
        {
            return;
        }

        audioSource.clip = interferenceClip;
        audioSource.Play();
    }

    public void StopRadio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public bool IsInterference() => isInterference;
}
