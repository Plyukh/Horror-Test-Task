using UnityEngine;

/// <summary>
/// Controller for NPCs that receive items from the player
/// </summary>
public class NPCItemReceiverController : BaseItemReceiverController
{
    public override void Interact(GameObject item)
    {
        if (!CanInteractWith(item))
        {
            ShowQuestInfoIfNeeded();
            return;
        }

        CompleteQuestIfNeeded();
        
        if (item != null)
        {
            Destroy(item);
        }
        
        canInteract = false;
    }
}