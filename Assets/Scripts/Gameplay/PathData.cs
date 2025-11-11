using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class PathData
{
    public string name;
    public List<Transform> waypoints = new List<Transform>();
    public bool loop = false;
    public UnityEvent onPathFinished;

    [Header("Doors")]
    [Tooltip("Doors that NPC should open when approaching while following this path")]
    public List<DoorController> doorsToOpen = new List<DoorController>();
}
