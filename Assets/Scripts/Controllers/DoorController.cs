using System.Collections;
using UnityEngine;

/// <summary>
/// Controller for doors that can be opened, closed, and locked
/// </summary>
public class DoorController : MonoBehaviour, IInteractable
{
    private const float MIN_OPEN_SPEED = 0.0001f;
    private const float ROTATION_DURATION_MULTIPLIER = 1f;

    [Header("Main")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private bool completeCurrentQuest = false;

    [Header("Door Settings")]
    [SerializeField] private bool startsLocked = false;
    [SerializeField] private bool initiallyOpen = false;

    [Header("Pairing (optional)")]
    [Tooltip("If assigned, this door will act together with the paired door (open/close/highlight together)")]
    [SerializeField] private DoorController pairedDoor;

    [Header("Smooth Rotation Fallback")]
    [Tooltip("Pivot to rotate if no animator")]
    [SerializeField] private Transform doorPivot;
    [SerializeField] private Vector3 closedEuler = Vector3.zero;
    [SerializeField] private Vector3 openEuler = new Vector3(0f, 90f, 0f);
    [SerializeField] private float openSpeed = 6f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField] private AudioClip lockedClip;

    private bool isLocked;
    private bool isOpen;
    private Coroutine rotateCoroutine;

    public bool CanInteract()
    {
        return canInteract;
    }

    public bool CanInteractWith(GameObject go)
    {
        return false;
    }

    private void Awake()
    {
        isLocked = startsLocked;
        isOpen = initiallyOpen;

        ValidateConfiguration();
        ApplyStateInstant();
    }

    private void ValidateConfiguration()
    {
        if (doorPivot == null)
        {
            Debug.LogError("DoorController: doorPivot is not assigned!");
        }
    }

    private void ApplyStateInstant()
    {
        if (doorPivot != null)
        {
            doorPivot.localEulerAngles = isOpen ? openEuler : closedEuler;
        }
    }

    public void Interact(GameObject interactor)
    {
        if (isLocked)
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.ShowQuestInfo();
            }

            PlayClip(lockedClip);
            return;
        }

        Toggle();
    }

    public void Lock() => isLocked = true;
    public void Unlock() => isLocked = false;

    public void Open()
    {
        if (!isOpen)
        {
            SetOpenState(true);
        }
    }

    public void Close()
    {
        if (isOpen)
        {
            SetOpenState(false);
        }
    }

    private void Toggle() => SetOpenState(!isOpen);

    private void SetOpenState(bool open, bool pairSync = false)
    {
        isOpen = open;

        if (completeCurrentQuest)
        {
            QuestManager.Instance.CompleteCurrentQuest();
        }

        if (doorPivot == null) return;

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
        }

        rotateCoroutine = StartCoroutine(RotateDoorCoroutine(open));

        if (!pairSync && pairedDoor != null)
        {
            pairedDoor.SetOpenState(open, true);
        }
    }

    public void SetOutlineWidth(float width)
    {
        OutlineHelper.SetOutlineWidthRecursive(gameObject, width);
    }

    // Public property for pairing (used by PickupController)
    public DoorController PairedDoor => pairedDoor;

    private IEnumerator RotateDoorCoroutine(bool open)
    {
        if (doorPivot == null) yield break;

        Vector3 from = doorPivot.localEulerAngles;
        Vector3 to = open ? openEuler : closedEuler;
        float duration = ROTATION_DURATION_MULTIPLIER / Mathf.Max(MIN_OPEN_SPEED, openSpeed);

        Quaternion qFrom = Quaternion.Euler(from);
        Quaternion qTo = Quaternion.Euler(to);

        PlayClip(open ? openClip : closeClip);

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            doorPivot.localRotation = Quaternion.Slerp(qFrom, qTo, t);
            yield return null;
        }

        doorPivot.localRotation = qTo;
        rotateCoroutine = null;
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void SetCanInteract(bool value)
    {
        canInteract = value;
    }
}
