using UnityEngine;

public interface IInteractable
{
    bool CanInteract();
    bool CanInteractWith(GameObject go);
    void Interact(GameObject interactor);
}