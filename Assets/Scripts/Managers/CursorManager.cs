using UnityEngine;

/// <summary>
/// Управляет видимостью и блокировкой курсора в зависимости от состояния UI окон
/// </summary>
public class CursorManager : MonoBehaviour
{
    private static CursorManager instance;

    [Header("UI Windows")]
    [Tooltip("Окно смерти (Death Window)")]
    [SerializeField] private GameObject deathWindow;
    [Tooltip("Окно победы (Win Window)")]
    [SerializeField] private GameObject winWindow;

    private bool wasDeathWindowActive = false;
    private bool wasWinWindowActive = false;

    public static CursorManager Instance => instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Инициализируем курсор как скрытый и заблокированный
        UpdateCursorState();
    }

    private void Update()
    {
        // Проверяем изменения состояния окон каждый кадр
        bool isDeathWindowActive = deathWindow != null && deathWindow.activeSelf;
        bool isWinWindowActive = winWindow != null && winWindow.activeSelf;

        // Обновляем курсор только если состояние изменилось
        if (isDeathWindowActive != wasDeathWindowActive || isWinWindowActive != wasWinWindowActive)
        {
            UpdateCursorState();
            wasDeathWindowActive = isDeathWindowActive;
            wasWinWindowActive = isWinWindowActive;
        }

        // Убеждаемся, что Escape не показывает курсор
        // Если курсор должен быть скрыт, принудительно скрываем его
        if (!ShouldShowCursor())
        {
            if (Cursor.visible || Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void UpdateCursorState()
    {
        bool shouldShowCursor = ShouldShowCursor();

        if (shouldShowCursor)
        {
            // Показываем курсор и разблокируем его
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Скрываем курсор и блокируем его
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private bool ShouldShowCursor()
    {
        // Показываем курсор только если одно из окон активно
        bool deathActive = deathWindow != null && deathWindow.activeSelf;
        bool winActive = winWindow != null && winWindow.activeSelf;

        return deathActive || winActive;
    }

    /// <summary>
    /// Принудительно обновить состояние курсора
    /// </summary>
    public void ForceUpdateCursor()
    {
        UpdateCursorState();
    }

    /// <summary>
    /// Установить ссылку на окно смерти
    /// </summary>
    public void SetDeathWindow(GameObject window)
    {
        deathWindow = window;
        ForceUpdateCursor();
    }

    /// <summary>
    /// Установить ссылку на окно победы
    /// </summary>
    public void SetWinWindow(GameObject window)
    {
        winWindow = window;
        ForceUpdateCursor();
    }

    public void Quit()
    {
        Application.Quit();
    }
}