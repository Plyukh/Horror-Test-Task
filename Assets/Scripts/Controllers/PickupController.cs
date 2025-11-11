using System.Collections;
using UnityEngine;

/// <summary>
/// Controller for picking up, holding, and interacting with items
/// </summary>
public class PickupController : MonoBehaviour
{
    private const float VIEWPORT_CENTER_X = 0.5f;
    private const float VIEWPORT_CENTER_Y = 0.5f;
    private const float MIN_PICKUP_DURATION = 0f;

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform selectedItemParent;

    [Header("Pickup Settings")]
    [SerializeField] private float interactRange = 2.0f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;

    [Header("Hold / Throw")]
    [SerializeField] private KeyCode throwKey = KeyCode.Mouse0;
    [SerializeField] private float throwForce = 6f;
    [SerializeField] private bool setKinematicOnPickup = true;
    [SerializeField] private bool disableCollidersOnPickup = false;

    [Header("Pickup Animation")]
    [Tooltip("Duration in seconds to move item into the selectedItemParent")]
    [SerializeField] private float pickupDuration = 0.25f;
    [SerializeField] private AnimationCurve pickupCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Flashlight")]
    [Tooltip("Right mouse toggles flashlight while holding an object with Flashlight component")]
    [SerializeField] private KeyCode flashlightToggleKey = KeyCode.Mouse1;

    [Header("Audio (SFX)")]
    [Tooltip("AudioSource used to play pickup/drop/throw SFX. If null, will try to find one on this GameObject.")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private AudioClip dropClip;
    [SerializeField] private AudioClip throwClip;

    [Header("UI")]
    [SerializeField] private GameObject itemUIInfo;
    [SerializeField] private GameObject interactUIInfo;

    [Header("Outline Settings")]
    [Tooltip("Outline width when hovered (set to 0 to disable)")]
    [SerializeField] private float hoverOutlineWidth = 10f;
    [Tooltip("Outline width when not hovered (usually 0)")]
    [SerializeField] private float defaultOutlineWidth = 0f;

    private GameObject hoveredObject;
    private GameObject lastHovered;
    private GameObject heldObject;
    private Item heldItem;
    private Rigidbody heldRigidbody;
    private Collider[] heldColliders;
    private Coroutine moveCoroutine;

    // Public API
    public bool IsHolding() => heldObject != null;
    public GameObject GetHeldObject() => heldObject;
    public GameObject GetHoveredObject() => hoveredObject;

    private void Awake()
    {
        InitializeReferences();
        ValidateConfiguration();
    }

    private void InitializeReferences()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }
    }

    private void ValidateConfiguration()
    {
        if (selectedItemParent == null)
        {
            Debug.LogError("PickupController: selectedItemParent is not assigned!");
        }

        if (mainCamera == null)
        {
            Debug.LogError("PickupController: mainCamera is not assigned and Camera.main is null!");
        }
    }

    private void Update()
    {
        UpdateHover();
        UpdateOutlineForHover();
        UpdateInfoObject();

        HandleInput();
    }

    private void HandleInput()
    {
        if (heldObject == null)
        {
            if (Input.GetKeyDown(pickupKey))
            {
                TryInteractOrPickup();
            }
        }
        else
        {
            HandleHeldObjectInput();
        }
    }

    private void HandleHeldObjectInput()
    {
        if (Input.GetKeyDown(flashlightToggleKey))
        {
            ToggleFlashlight();
        }

        if (Input.GetKeyDown(pickupKey))
        {
            bool handled = TryInteractOrPickup();
            if (!handled)
            {
                Drop();
            }
        }
        else if (Input.GetKeyDown(throwKey))
        {
            Throw();
        }
    }

    private void ToggleFlashlight()
    {
        if (heldObject == null) return;

        var flashlight = heldObject.GetComponentInChildren<Flashlight>();
        if (flashlight != null)
        {
            flashlight.Toggle();
        }
    }

    private bool IsPickupCandidate(Transform target)
    {
        if (target == null) return false;

        Transform current = target;
        while (current != null)
        {
            if (current.GetComponent<Item>() != null) 
                return true;
            current = current.parent;
        }
        return false;
    }

    private void UpdateHover()
    {
        hoveredObject = null;
        if (mainCamera == null) return;

        Vector3 viewportPoint = new Vector3(VIEWPORT_CENTER_X, VIEWPORT_CENTER_Y, 0f);
        Ray ray = mainCamera.ViewportPointToRay(viewportPoint);
        
        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask))
            return;

        hoveredObject = FindInteractableObject(hit.collider.transform);
    }

    private GameObject FindInteractableObject(Transform hitTransform)
    {
        Transform current = hitTransform;
        while (current != null)
        {
            var interactable = current.GetComponentInParent<IInteractable>();
            if (interactable != null && HasVisiblePresence(current))
            {
                return current.gameObject;
            }

            var item = current.GetComponent<Item>();
            if (item != null && HasVisiblePresence(current))
            {
                return current.gameObject;
            }

            current = current.parent;
        }
        return null;
    }

    private bool HasVisiblePresence(Transform transform)
    {
        return transform.GetComponentInChildren<Renderer>() != null ||
               transform.GetComponentInParent<Collider>() != null ||
               transform.GetComponentInParent<Rigidbody>() != null;
    }

    private void UpdateOutlineForHover()
    {
        GameObject effectiveHover = GetEffectiveHoveredObject();

        if (effectiveHover != lastHovered)
        {
            UpdateOutlineForObject(lastHovered, defaultOutlineWidth);
            UpdateOutlineForObject(effectiveHover, hoverOutlineWidth);
            lastHovered = effectiveHover;
        }
    }

    private GameObject GetEffectiveHoveredObject()
    {
        if (hoveredObject == null) return null;

        var itemComponent = hoveredObject.GetComponentInParent<Item>();
        if (itemComponent != null && !itemComponent.CanPickup)
        {
            return null;
        }

        var interactable = hoveredObject.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            if (!CanInteract(interactable))
            {
                return null;
            }

            if (heldObject != null && !CanInteractWithHeldObject(interactable, itemComponent))
            {
                return null;
            }
        }
        else if (heldObject != null)
        {
            if (!InteractionValidator.IsAdditionalItemMatch(itemComponent, heldItem))
            {
                return null;
            }
        }

        return hoveredObject;
    }

    private bool CanInteract(IInteractable interactable)
    {
        try
        {
            return interactable.CanInteract();
        }
        catch
        {
            return true;
        }
    }

    private bool CanInteractWithHeldObject(IInteractable interactable, Item hoveredItem)
    {
        if (heldObject == null) return false;

        // Сначала проверяем, может ли интерактивный объект принять предмет в руках
        if (interactable.CanInteractWith(heldObject))
        {
            return true;
        }

        // Если нет, проверяем, является ли наведенный объект дополнительным предметом
        // (например, крышка для чашки)
        if (hoveredItem != null)
        {
            return InteractionValidator.IsAdditionalItemMatch(hoveredItem, heldItem);
        }

        // Если наведенный объект не является Item и не может принять предмет в руках,
        // то взаимодействие невозможно
        return false;
    }

    private void UpdateOutlineForObject(GameObject target, float width)
    {
        if (target == null) return;

        var door = target.GetComponentInParent<DoorController>();
        if (door != null)
        {
            door.SetOutlineWidth(width);
            if (door.PairedDoor != null)
            {
                door.PairedDoor.SetOutlineWidth(width);
            }
        }
        else
        {
            OutlineHelper.SetOutlineWidthRecursive(target, width);
        }
    }

    private void UpdateInfoObject()
    {
        bool shouldShow = ShouldShowInteractUI();
        SetInteractUIVisibility(shouldShow);
    }

    private bool ShouldShowInteractUI()
    {
        if (hoveredObject == null) return false;

        var itemComponent = hoveredObject.GetComponentInParent<Item>();
        if (itemComponent != null && !itemComponent.CanPickup)
        {
            return false;
        }

        var interactable = hoveredObject.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            if (!CanInteract(interactable))
            {
                return false;
            }

            if (heldObject != null && !CanInteractWithHeldObject(interactable, itemComponent))
            {
                return false;
            }
        }
        else if (heldObject != null)
        {
            if (!InteractionValidator.IsAdditionalItemMatch(itemComponent, heldItem))
            {
                return false;
            }
        }

        return true;
    }

    private void SetInteractUIVisibility(bool show)
    {
        if (interactUIInfo != null)
        {
            interactUIInfo.SetActive(show);
        }
    }


    private bool TryInteractOrPickup()
    {
        if (hoveredObject == null) return false;

        var hoveredItem = hoveredObject.GetComponentInParent<Item>();
        if (hoveredItem != null && !hoveredItem.CanPickup)
        {
            return true;
        }

        var interactable = hoveredObject.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            return TryInteractWithObject(interactable);
        }

        if (TryAttachAdditionalItem(hoveredItem))
        {
            return true;
        }

        if (heldObject == null && IsPickupCandidate(hoveredObject.transform))
        {
            Pickup(hoveredObject);
            return true;
        }

        return false;
    }

    private bool TryInteractWithObject(IInteractable interactable)
    {
        if (!CanInteract(interactable))
        {
            return true;
        }

        if (heldObject != null)
        {
            // Проверяем, подходит ли предмет по ID перед взаимодействием
            if (!interactable.CanInteractWith(heldObject))
            {
                // Предмет не подходит - возвращаем false, чтобы выполнился Drop
                return false;
            }

            // Предмет подходит - выполняем взаимодействие
            interactable.Interact(heldObject);
            ClearItem();
            return true;
        }

        interactable.Interact(gameObject);
        return true;
    }

    private bool TryAttachAdditionalItem(Item hoveredItem)
    {
        if (heldObject == null || hoveredItem == null || heldItem == null)
        {
            return false;
        }

        var requiredItem = heldItem.AdditionalItem;
        var targetPosition = heldItem.AdditionalPosition;
        
        if (requiredItem != null && targetPosition != null && hoveredItem.Id == requiredItem.Id)
        {
            Pickup(hoveredItem.gameObject, targetPosition.transform);
            return true;
        }

        return false;
    }

    public void Pickup(GameObject target)
    {
        Pickup(target, selectedItemParent);
    }

    public void Pickup(GameObject target, Transform targetParent)
    {
        if (target == null) return;

        heldObject = target;
        InitializeHeldObject();
        PlayPickupSound();

        SetInteractUIVisibility(false);
        OutlineHelper.SetOutlineWidthRecursive(heldObject, defaultOutlineWidth);

        Transform finalParent = targetParent != null ? targetParent : selectedItemParent;
        if (pickupDuration <= MIN_PICKUP_DURATION || finalParent == null)
        {
            AttachToParentImmediate(finalParent);
            OnPickupComplete();
        }
        else
        {
            StartPickupAnimation(finalParent);
        }
    }

    private void InitializeHeldObject()
    {
        heldRigidbody = heldObject.GetComponent<Rigidbody>();
        heldColliders = heldObject.GetComponentsInChildren<Collider>();
        heldItem = heldObject.GetComponent<Item>();

        if (heldRigidbody != null && setKinematicOnPickup)
        {
            heldRigidbody.linearVelocity = Vector3.zero;
            heldRigidbody.angularVelocity = Vector3.zero;
            heldRigidbody.isKinematic = true;
        }

        if (disableCollidersOnPickup && heldColliders != null)
        {
            foreach (var collider in heldColliders)
            {
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }
    }

    private void PlayPickupSound()
    {
        if (sfxSource != null && pickupClip != null)
        {
            sfxSource.PlayOneShot(pickupClip);
        }
    }

    private void AttachToParentImmediate(Transform parent)
    {
        if (heldObject == null || parent == null) return;

        heldObject.transform.SetParent(parent, false);
        heldObject.transform.localPosition = Vector3.zero;
        heldObject.transform.localRotation = Quaternion.identity;
    }

    private void StartPickupAnimation(Transform targetParent)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(MoveToParentCoroutine(heldObject.transform, targetParent, pickupDuration));
    }

    private IEnumerator MoveToParentCoroutine(Transform movingTransform, Transform targetParent, float duration)
    {
        Vector3 startPos = movingTransform.position;
        Quaternion startRot = movingTransform.rotation;
        Vector3 targetWorldPos = targetParent.TransformPoint(Vector3.zero);
        Quaternion targetWorldRot = targetParent.rotation;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
            float curveValue = pickupCurve.Evaluate(normalizedTime);
            
            movingTransform.position = Vector3.LerpUnclamped(startPos, targetWorldPos, curveValue);
            movingTransform.rotation = Quaternion.Slerp(startRot, targetWorldRot, curveValue);
            
            yield return null;
        }

        movingTransform.SetParent(targetParent, false);
        movingTransform.localPosition = Vector3.zero;
        movingTransform.localRotation = Quaternion.identity;

        moveCoroutine = null;
        OnPickupComplete();
    }

    private void OnPickupComplete()
    {
        SetActiveInfoItem(true);
        if (heldItem != null && heldItem.CompleteCurrentQuest && QuestManager.Instance != null)
        {
            QuestManager.Instance.CompleteCurrentQuest();
        }
    }

    public void Drop()
    {
        if (heldObject == null) return;

        var itemComponent = heldObject.GetComponent<Item>();
        if (itemComponent != null && !itemComponent.CanBeDropped)
        {
            return;
        }

        ReleaseHeldObject();
        PlayDropSound();
        ClearItem();
    }

    public void Throw()
    {
        if (heldObject == null) return;

        var itemComponent = heldObject.GetComponent<Item>();
        if (itemComponent != null && !itemComponent.CanBeDropped)
        {
            return;
        }

        ReleaseHeldObject();
        ApplyThrowForce();
        PlayThrowSound();
        ClearItem();
    }

    private void ReleaseHeldObject()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        OutlineHelper.SetOutlineWidthRecursive(heldObject, defaultOutlineWidth);
        heldObject.transform.SetParent(null, true);

        RestorePhysics();
        RestoreColliders();
    }

    private void RestorePhysics()
    {
        if (heldRigidbody != null && setKinematicOnPickup)
        {
            heldRigidbody.isKinematic = false;
        }
    }

    private void RestoreColliders()
    {
        if (disableCollidersOnPickup && heldColliders != null)
        {
            foreach (var collider in heldColliders)
            {
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }
        }
    }

    private void ApplyThrowForce()
    {
        if (heldRigidbody == null || mainCamera == null) return;

        heldRigidbody.isKinematic = false;
        heldRigidbody.linearVelocity = Vector3.zero;
        heldRigidbody.angularVelocity = Vector3.zero;
        heldRigidbody.AddForce(mainCamera.transform.forward * throwForce, ForceMode.VelocityChange);
    }

    private void PlayDropSound()
    {
        if (sfxSource != null && dropClip != null)
        {
            sfxSource.PlayOneShot(dropClip);
        }
    }

    private void PlayThrowSound()
    {
        if (sfxSource != null && throwClip != null)
        {
            sfxSource.PlayOneShot(throwClip);
        }
    }

    private void SetActiveInfoItem(bool value)
    {
        if (itemUIInfo != null)
        {
            itemUIInfo.SetActive(value);
        }
    }

    private void ClearItem()
    {
        heldObject = null;
        heldRigidbody = null;
        heldColliders = null;
        heldItem = null;
        SetActiveInfoItem(false);
    }
}
