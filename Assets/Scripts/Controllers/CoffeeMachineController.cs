using System.Collections;
using UnityEngine;

/// <summary>
/// Controller for coffee machine that accepts cups and makes coffee
/// </summary>
public class CoffeeMachineController : BaseItemReceiverController
{
    private const float MIN_PLACEMENT_DELAY = 0f;

    [Header("Placement")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool enablePhysicsOnPlace = true;
    [SerializeField] private bool enableCollidersOnPlace = true;
    [SerializeField] private float placementDelay = 0f;

    [Header("Cooking")]
    [SerializeField] private float cookingTime = 3f;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip placeClip;
    [SerializeField] private AudioClip cookingClip;

    [Header("Visual")]
    [SerializeField] private Outline outline;

    private Animator currentCupAnimator;
    private bool isCooking = false;

    private void Awake()
    {
        if (outline == null)
        {
            outline = GetComponent<Outline>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public override void Interact(GameObject item)
    {
        if (currentCupAnimator == null && !isCooking)
        {
            HandleCupPlacement(item);
        }
        else if (currentCupAnimator != null && !isCooking)
        {
            StartCoroutine(MakeCoffeeCoroutine());
        }
    }

    private void HandleCupPlacement(GameObject item)
    {
        if (!CanInteractWith(item))
        {
            ShowQuestInfoIfNeeded();
            return;
        }

        CompleteQuestIfNeeded();
        StartCoroutine(PlaceHeldItemCoroutine(item));
    }

    private IEnumerator PlaceHeldItemCoroutine(GameObject held)
    {
        if (held == null) yield break;

        currentCupAnimator = held.GetComponent<Animator>();
        if (currentCupAnimator == null)
        {
            Debug.LogWarning("CoffeeMachineController: Held item does not have an Animator component.");
            yield break;
        }

        held.transform.SetParent(null, true);

        var rb = held.GetComponent<Rigidbody>();
        bool originalKinematic = false;
        if (rb != null)
        {
            originalKinematic = rb.isKinematic;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Collider[] colliders = held.GetComponentsInChildren<Collider>(true);
        bool[] collidersOriginallyEnabled = null;
        if (colliders != null && colliders.Length > 0)
        {
            collidersOriginallyEnabled = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
            {
                collidersOriginallyEnabled[i] = colliders[i].enabled;
                if (enableCollidersOnPlace)
                {
                    colliders[i].enabled = false;
                }
            }
        }

        PlayAudioClip(placeClip);

        Transform targetTransform = spawnPoint != null ? spawnPoint : transform;
        Vector3 startPos = held.transform.position;
        Quaternion startRot = held.transform.rotation;
        Vector3 targetPos = targetTransform.position;
        Quaternion targetRot = targetTransform.rotation;

        if (placementDelay <= MIN_PLACEMENT_DELAY)
        {
            held.transform.position = targetPos;
            held.transform.rotation = targetRot;
        }
        else
        {
            yield return StartCoroutine(AnimatePlacement(held.transform, startPos, startRot, targetPos, targetRot, placementDelay));
        }

        if (enableCollidersOnPlace && colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = true;
                }
            }
        }

        if (enablePhysicsOnPlace && rb != null)
        {
            rb.isKinematic = false;
        }
        else if (rb != null)
        {
            rb.isKinematic = originalKinematic;
        }
    }

    private IEnumerator AnimatePlacement(Transform target, Vector3 startPos, Quaternion startRot, Vector3 targetPos, Quaternion targetRot, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            target.position = Vector3.Lerp(startPos, targetPos, smoothT);
            target.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
            
            yield return null;
        }

        target.position = targetPos;
        target.rotation = targetRot;
    }

    private IEnumerator MakeCoffeeCoroutine()
    {
        if (currentCupAnimator == null || isCooking) yield break;

        isCooking = true;
        canInteract = false;

        if (outline != null)
        {
            outline.enabled = false;
        }

        PlayAudioClip(cookingClip);
        currentCupAnimator.SetTrigger("With Coffee");

        float elapsedTime = 0f;
        while (elapsedTime < cookingTime)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        EnableCollidersOnCup();
        CompleteQuestIfNeeded();
        
        isCooking = false;
    }

    private void EnableCollidersOnCup()
    {
        if (currentCupAnimator == null) return;

        Collider[] colliders = currentCupAnimator.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = true;
            }
        }
    }

    private void PlayAudioClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
