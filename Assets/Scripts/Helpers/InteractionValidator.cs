using UnityEngine;

/// <summary>
/// Helper class for validating interactions between items and interactables
/// </summary>
public static class InteractionValidator
{
    /// <summary>
    /// Checks if an item matches the required additional item for another item
    /// </summary>
    public static bool IsAdditionalItemMatch(Item hoveredItem, Item heldItem)
    {
        if (hoveredItem == null || heldItem == null) 
            return false;
            
        var requiredItem = heldItem.AdditionalItem;
        if (requiredItem == null) 
            return false;
            
        return hoveredItem.Id == requiredItem.Id;
    }

    /// <summary>
    /// Checks if an interactable can interact with a given GameObject (item)
    /// </summary>
    public static bool CanInteractWithItem(IInteractable interactable, GameObject item, int requiredId)
    {
        if (item == null || interactable == null) 
            return false;

        var itemComponent = item.GetComponent<Item>();
        if (itemComponent == null) 
            return false;

        return itemComponent.Id == requiredId;
    }
}

