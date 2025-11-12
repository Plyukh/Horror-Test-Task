using UnityEngine;

public class GeneratorController : MonoBehaviour, IInteractable
{
    [Header("Main")]
    [SerializeField] private bool canInteract = true;

    [Header("Visual")]
    [SerializeField] private GameObject effectGameObject;

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
        effectGameObject.SetActive(true);
        canInteract = false;
        QuestManager.Instance.CompleteCurrentQuest();
    }
}
