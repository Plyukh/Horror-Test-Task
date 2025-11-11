using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object that can switch between Normal and Broken states
/// </summary>
public class TwoStateObject : MonoBehaviour
{
    [Header("Children (assign existing children)")]
    [SerializeField] private List<StateTag> objects = new List<StateTag>();

    [Header("Options")]
    [SerializeField] private ObjectState startState = ObjectState.Normal;

    private ObjectState currentState;

    public ObjectState GetState() => currentState;

    public void SetNormal() => SetState(ObjectState.Normal);

    public void SetBroken() => SetState(ObjectState.Broken);

    public void ToggleState() => SetState(currentState == ObjectState.Normal ? ObjectState.Broken : ObjectState.Normal);

    public void SetState(ObjectState newState)
    {
        ApplyState(newState);
    }

    private void Start()
    {
        ApplyState(startState);
    }

    private void ApplyState(ObjectState newState)
    {
        currentState = newState;

        if (objects == null) return;

        foreach (var tag in objects)
        {
            if (tag == null || tag.gameObject == null) continue;

            bool shouldBeActive = tag.state == newState;
            if (tag.gameObject.activeSelf != shouldBeActive)
            {
                tag.gameObject.SetActive(shouldBeActive);
            }
        }
    }
}