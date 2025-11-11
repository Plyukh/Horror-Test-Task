using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TaskEntry
{
    [TextArea(2, 4)]
    public string description;      // текст задания, показываемый игроку
    public UnityEvent onComplete;   // события, вызываемые при выполнении задания
}
