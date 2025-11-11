using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages quest progression and UI display
/// </summary>
public class QuestManager : MonoBehaviour
{
    private const float DEFAULT_INFO_DURATION = 2f;
    private const int DESCRIPTION_START_INDEX = 2;

    public static QuestManager Instance { get; private set; }

    [Header("Tasks")]
    [SerializeField] private List<TaskEntry> tasks = new List<TaskEntry>();

    [Header("UI")]
    [SerializeField] private Text questInfoText;
    [SerializeField] private Text uiTextPrefab;
    [SerializeField] private GameObject uiTextPrefabParent;

    private int currentIndex = -1;
    private Animator currentTextAnimator;
    private float infoDisplayDuration = 0f;
    private float currentDuration = 0f;

    // Legacy property for backward compatibility
    public static QuestManager instance => Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple QuestManager instances found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        ValidateReferences();
        CompleteCurrentQuest();
    }

    private void ValidateReferences()
    {
        if (questInfoText == null)
        {
            Debug.LogError("QuestManager: questInfoText is not assigned!");
        }

        if (uiTextPrefab == null)
        {
            Debug.LogError("QuestManager: uiTextPrefab is not assigned!");
        }

        if (uiTextPrefabParent == null)
        {
            Debug.LogError("QuestManager: uiTextPrefabParent is not assigned!");
        }

        if (tasks == null || tasks.Count == 0)
        {
            Debug.LogWarning("QuestManager: No tasks assigned!");
        }
    }

    private void Update()
    {
        if (infoDisplayDuration > 0f)
        {
            currentDuration += Time.deltaTime;
            if (currentDuration >= infoDisplayDuration)
            {
                HideQuestInfo();
            }
        }
    }

    public void CompleteCurrentQuest()
    {
        if (tasks == null || tasks.Count == 0)
        {
            Debug.LogWarning("QuestManager: Cannot complete quest - no tasks available.");
            return;
        }

        if (currentIndex >= 0 && currentIndex < tasks.Count)
        {
            var completedTask = tasks[currentIndex];
            if (completedTask != null && completedTask.onComplete != null)
            {
                completedTask.onComplete.Invoke();
            }
        }

        currentIndex++;

        if (currentIndex >= tasks.Count)
        {
            Debug.Log("QuestManager: All tasks completed!");
            return;
        }

        if (currentTextAnimator != null)
        {
            currentTextAnimator.SetTrigger("Complete Quest");
        }

        CreateNewQuestUI();
    }

    private void CreateNewQuestUI()
    {
        if (uiTextPrefab == null || uiTextPrefabParent == null || questInfoText == null)
        {
            return;
        }

        if (currentIndex < 0 || currentIndex >= tasks.Count)
        {
            return;
        }

        var currentTask = tasks[currentIndex];
        if (currentTask == null)
        {
            Debug.LogWarning($"QuestManager: Task at index {currentIndex} is null!");
            return;
        }

        Text newText = Instantiate(uiTextPrefab, uiTextPrefabParent.transform);
        if (newText == null)
        {
            Debug.LogError("QuestManager: Failed to instantiate uiTextPrefab!");
            return;
        }

        string description = currentTask.description ?? string.Empty;
        newText.text = description;

        // Remove first 2 characters from description for quest info display
        questInfoText.text = description.Length > DESCRIPTION_START_INDEX 
            ? description.Substring(DESCRIPTION_START_INDEX) 
            : string.Empty;

        currentTextAnimator = newText.GetComponent<Animator>();
    }

    public void ShowQuestInfo()
    {
        if (questInfoText == null) return;

        if (!questInfoText.gameObject.activeInHierarchy)
        {
            questInfoText.gameObject.SetActive(true);
            infoDisplayDuration = DEFAULT_INFO_DURATION;
            currentDuration = 0f;
        }
    }

    private void HideQuestInfo()
    {
        if (questInfoText != null)
        {
            questInfoText.gameObject.SetActive(false);
        }

        infoDisplayDuration = 0f;
        currentDuration = 0f;
    }

    public int GetCurrentTaskIndex() => currentIndex;
    public bool HasMoreTasks() => currentIndex >= 0 && currentIndex < tasks.Count;
}