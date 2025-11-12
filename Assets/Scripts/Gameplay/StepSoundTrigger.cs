using UnityEngine;

public class StepSoundTrigger : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clip;

    private bool soundPlayed;

    public void PlaySound()
    {
        audioSource.PlayOneShot(clip);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (soundPlayed == false)
        {
            if (other.gameObject.GetComponent<PlayerMovementController>())
            {
                soundPlayed = true;
                PlaySound();
            }
        }
    }
}