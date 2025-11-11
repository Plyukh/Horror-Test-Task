using System.Collections.Generic;
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
            // Удаляем все дочерние предметы (например, дополнительные предметы в иерархии)
            DestroyItemAndChildren(item);
        }
        
        canInteract = false;
    }

    /// <summary>
    /// Удаляет предмет и все его дочерние объекты с компонентами Item
    /// </summary>
    private void DestroyItemAndChildren(GameObject item)
    {
        if (item == null) return;

        // Собираем все объекты с компонентами Item в иерархии (включая сам item)
        HashSet<GameObject> itemsToDestroy = new HashSet<GameObject>();
        Item[] allItems = item.GetComponentsInChildren<Item>(true);
        
        // Добавляем все найденные предметы
        foreach (Item foundItem in allItems)
        {
            if (foundItem != null && foundItem.gameObject != null)
            {
                itemsToDestroy.Add(foundItem.gameObject);
            }
        }

        // Убеждаемся, что основной предмет тоже в списке (на случай, если у него нет компонента Item)
        itemsToDestroy.Add(item);

        // Удаляем все объекты
        // Unity автоматически удалит дочерние объекты при удалении родителя,
        // но лучше удалить явно для ясности
        foreach (GameObject objToDestroy in itemsToDestroy)
        {
            if (objToDestroy != null)
            {
                Destroy(objToDestroy);
            }
        }
    }
}