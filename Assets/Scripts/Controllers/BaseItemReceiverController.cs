using UnityEngine;

/// <summary>
/// Base class for controllers that receive items with specific IDs
/// </summary>
public abstract class BaseItemReceiverController : MonoBehaviour, IInteractable
{
    [Header("Main")]
    [SerializeField] protected bool canInteract = true;

    [Tooltip("Required id for the held item to be accepted")]
    [SerializeField] protected int requiredID = 0;

    public bool CanInteract()
    {
        return canInteract;
    }

    public virtual bool CanInteractWith(GameObject go)
    {
        return InteractionValidator.CanInteractWithItem(this, go, requiredID);
    }

    public abstract void Interact(GameObject interactor);

    public void SetCanInteract(bool value)
    {
        canInteract = value;
    }

    protected void ShowQuestInfoIfNeeded()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ShowQuestInfo();
        }
    }

    protected void CompleteQuestIfNeeded()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.CompleteCurrentQuest();
        }
    }
}

