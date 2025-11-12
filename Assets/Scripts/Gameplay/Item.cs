using UnityEngine;

/// <summary>
/// Component that marks an object as an interactable item
/// </summary>
public class Item : MonoBehaviour
{
    [Header("Main")]
    [SerializeField] private int id;

    [Header("Pickup behavior")]
    [SerializeField] private bool canBeDropped = true;
    [SerializeField] private bool canPickup = true;
    [SerializeField] private bool completeCurrentQuest = false;

    [Header("Optional")]
    [Tooltip("Additional item that can be attached to this item")]
    [SerializeField] private Item additionalItem;
    [Tooltip("Position where additional item should be placed")]
    [SerializeField] private GameObject additionalPosition;

    public int Id => id;
    public bool CanBeDropped => canBeDropped;
    public bool CanPickup => canPickup;
    public bool CompleteCurrentQuest => completeCurrentQuest;
    public Item AdditionalItem => additionalItem;
    public GameObject AdditionalPosition => additionalPosition;

    public void SetCanPickup(bool value)
    {
        canPickup = value;
    }

    public void SetCanBeDropped(bool value)
    {
        canBeDropped = value;
    }

    public void SetCompleteCurrentQuest(bool value)
    {
        completeCurrentQuest = value;
    }
}