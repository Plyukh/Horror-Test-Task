using UnityEngine;

public class CarController : MonoBehaviour, IInteractable
{
    [Header("Main")]
    [SerializeField] private bool canInteract = true;

    public bool CanInteract()
    {
        return canInteract;
    }

    public bool CanInteractWith(GameObject go)
    {
        return false;
    }

    public void Interact(GameObject interactor)
    {
        canInteract = false;
        QuestManager.Instance.CompleteCurrentQuest();
    }
}
