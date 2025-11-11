using UnityEngine;

/// <summary>
/// Controller for NPC animations and footstep sounds
/// </summary>
public class NPCAnimatorController : MonoBehaviour
{
    private const string WALK_ANIMATOR_PARAMETER = "Walk";

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void SetWalk(bool walk)
    {
        if (animator != null)
        {
            animator.SetBool(WALK_ANIMATOR_PARAMETER, walk);
        }
    }

    public void PlayFootStep()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
}