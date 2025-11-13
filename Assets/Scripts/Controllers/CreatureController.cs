using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Контроллер существа, которое преследует игрока
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class CreatureController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Игрок, за которым будет гнаться существо. Если не назначен, будет искаться автоматически.")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [Tooltip("Аниматор для управления анимациями существа")]
    [SerializeField] private Animator animator;
    [Tooltip("AudioSource для звуков существа")]
    [SerializeField] private AudioSource audioSource;

    [Header("State")]
    [Tooltip("Начальное состояние существа")]
    [SerializeField] private CreatureState initialState = CreatureState.Calm;
    [SerializeField] private GameObject basePosition;

    [Header("Calm State Settings")]
    [Tooltip("Скорость движения в спокойном состоянии")]
    [SerializeField] private float calmMoveSpeed = 1.0f;
    [Tooltip("Скорость поворота в спокойном состоянии")]
    [SerializeField] private float calmRotateSpeed = 90f;

    [Header("Chasing State Settings")]
    [Tooltip("Скорость преследования игрока")]
    [SerializeField] private float chaseSpeed = 4.0f;
    [Tooltip("Скорость поворота при преследовании")]
    [SerializeField] private float chaseRotateSpeed = 180f;
    [Tooltip("Дистанция остановки перед игроком")]
    [SerializeField] private float stopDistance = 1.5f;
    [Tooltip("Дистанция обнаружения игрока (для автоматического переключения в режим преследования)")]
    [SerializeField] private float detectionDistance = 10f;
    [Tooltip("Как часто обновлять путь к игроку (в секундах)")]
    [SerializeField] private float pathUpdateInterval = 0.5f;

    [Header("Audio")]
    [Tooltip("Звук при переходе в режим преследования")]
    [SerializeField] private AudioClip chaseStartClip;
    [Tooltip("Звук при переходе в спокойное состояние")]
    [SerializeField] private AudioClip calmClip;
    [Tooltip("Звук дыхания/движения (зацикленный)")]
    [SerializeField] private AudioClip movementLoopClip;

    [Header("Player Catch Settings")]
    [Tooltip("Камера игрока для управления")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Точка позиции, к которой будет перемещаться камера при поимке игрока")]
    [SerializeField] private Transform cameraPositionPoint;
    [Tooltip("Точка, в которую будет смотреть камера при поимке игрока")]
    [SerializeField] private Transform cameraLookAtPoint;
    [Tooltip("Значение FOV для зума камеры при поимке")]
    [SerializeField] private float catchCameraFOV = 30f;
    [Tooltip("Скорость перехода камеры к точке взгляда")]
    [SerializeField] private float cameraTransitionSpeed = 2f;
    [Tooltip("Имя bool параметра в аниматоре, который нужно отключить при поимке")]
    [SerializeField] private string animatorBoolToDisable = "Chasing";
    [Tooltip("Компонент управления игроком для отключения")]
    [SerializeField] private PlayerMovementController playerMovementController;

    [Header("UI")]
    [SerializeField] private GameObject deathWindow;

    private CreatureState currentState;
    private Vector3 lastKnownPlayerPosition;
    private float pathUpdateTimer = 0f;
    private bool hasCaughtPlayer = false;
    private float originalCameraFOV;
    private Coroutine cameraTransitionCoroutine;

    // Public API
    public CreatureState CurrentState => currentState;
    public bool IsChasing => currentState == CreatureState.Chasing;
    public bool IsCalm => currentState == CreatureState.Calm;

    private void Awake()
    {
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Настраиваем NavMeshAgent
        if (navMeshAgent != null)
        {
            navMeshAgent.stoppingDistance = stopDistance;
            navMeshAgent.autoBraking = true;
        }

        // Ищем камеру, если не назначена
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindFirstObjectByType<Camera>();
            }
        }

        // Сохраняем оригинальный FOV камеры
        if (playerCamera != null)
        {
            originalCameraFOV = playerCamera.fieldOfView;
        }

        // Ищем PlayerMovementController, если не назначен
        if (playerMovementController == null)
        {
            if (playerTarget != null)
            {
                playerMovementController = playerTarget.GetComponent<PlayerMovementController>();
            }
            if (playerMovementController == null)
            {
                playerMovementController = FindFirstObjectByType<PlayerMovementController>();
            }
        }
    }

    private void Start()
    {
        SetStateWithAnimator(initialState);
    }

    private void Update()
    {
        UpdateState();
    }

    public void SetBasePosition()
    {
        transform.position = basePosition.transform.position;
        transform.rotation = basePosition.transform.rotation;
        hasCaughtPlayer = false;
    }

    private void UpdateState()
    {
        switch (currentState)
        {
            case CreatureState.Calm:
                UpdateCalmState();
                break;
            case CreatureState.Chasing:
                UpdateChasingState();
                break;
        }
    }

    private void UpdateCalmState()
    {
        // В спокойном состоянии останавливаем NavMeshAgent
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
        {
            if (navMeshAgent.hasPath)
            {
                navMeshAgent.ResetPath();
            }
        }
    }

    private void UpdateChasingState()
    {
        if (playerTarget == null || navMeshAgent == null || !navMeshAgent.isActiveAndEnabled) return;

        if (!navMeshAgent.isOnNavMesh)
        {
            return;
        }

        // Проверяем, догнали ли мы игрока
        if (!hasCaughtPlayer && HasReachedPlayer())
        {
            OnPlayerCaught();
            return;
        }

        // Если уже поймали игрока, не обновляем путь
        if (hasCaughtPlayer) return;

        // Обновляем путь к игроку с интервалом
        pathUpdateTimer += Time.deltaTime;
        if (pathUpdateTimer >= pathUpdateInterval)
        {
            pathUpdateTimer = 0f;
            UpdatePathToPlayer();
        }
    }

    private bool HasReachedPlayer()
    {
        if (navMeshAgent == null || playerTarget == null) return false;

        // Проверяем, достиг ли NavMeshAgent цели
        if (navMeshAgent.remainingDistance <= stopDistance && navMeshAgent.remainingDistance > 0f)
        {
            return true;
        }

        // Дополнительная проверка по прямой дистанции
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        return distanceToPlayer <= stopDistance;
    }

    private void OnPlayerCaught()
    {
        hasCaughtPlayer = true;
        deathWindow.SetActive(true);

        // Останавливаем существо
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.ResetPath();
        }

        // Отключаем bool в аниматоре
        if (animator != null && !string.IsNullOrEmpty(animatorBoolToDisable))
        {
            animator.SetBool(animatorBoolToDisable, false);
            
        }

        // Отключаем управление игроком
        if (playerMovementController != null)
        {
            playerMovementController.SetInputEnabled(false);
        }

        // Запускаем переход камеры
        if (playerCamera != null && cameraPositionPoint != null && cameraLookAtPoint != null)
        {
            if (cameraTransitionCoroutine != null)
            {
                StopCoroutine(cameraTransitionCoroutine);
            }
            cameraTransitionCoroutine = StartCoroutine(TransitionCameraToPoint());
        }
    }

    private System.Collections.IEnumerator TransitionCameraToPoint()
    {
        if (playerCamera == null || cameraPositionPoint == null || cameraLookAtPoint == null) yield break;

        Vector3 startPosition = playerCamera.transform.position;
        Quaternion startRotation = playerCamera.transform.rotation;
        float startFOV = playerCamera.fieldOfView;

        float elapsedTime = 0f;

        // Фаза перехода
        while (elapsedTime < 1f / cameraTransitionSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime * cameraTransitionSpeed);

            // Целевая позиция камеры (обновляется каждый кадр, если точка движется)
            Vector3 targetPosition = cameraPositionPoint.position;

            // Вычисляем целевой поворот камеры (смотреть на точку из текущей позиции)
            Vector3 directionToPoint = (cameraLookAtPoint.position - targetPosition).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToPoint);

            // Плавно перемещаем камеру к позиции
            playerCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            // Плавно поворачиваем камеру к точке
            playerCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            // Плавно зумим камеру
            playerCamera.fieldOfView = Mathf.Lerp(startFOV, catchCameraFOV, t);

            yield return null;
        }

        // Фаза постоянного обновления позиции камеры
        while (hasCaughtPlayer)
        {
            // Обновляем позицию камеры каждый кадр
            playerCamera.transform.position = cameraPositionPoint.position;

            // Вычисляем поворот камеры (смотреть на точку из текущей позиции)
            Vector3 directionToPoint = (cameraLookAtPoint.position - cameraPositionPoint.position).normalized;
            playerCamera.transform.rotation = Quaternion.LookRotation(directionToPoint);

            // Поддерживаем FOV
            playerCamera.fieldOfView = catchCameraFOV;

            yield return null;
        }
    }

    private void UpdatePathToPlayer()
    {
        if (playerTarget == null || navMeshAgent == null || !navMeshAgent.isOnNavMesh) return;

        Vector3 targetPosition = playerTarget.position;
        
        // Проверяем, изменилась ли позиция игрока
        if (Vector3.Distance(targetPosition, lastKnownPlayerPosition) > 0.1f)
        {
            NavMeshHit hit;
            // Проверяем, доступна ли позиция игрока на NavMesh
            if (NavMesh.SamplePosition(targetPosition, out hit, 5f, NavMesh.AllAreas))
            {
                navMeshAgent.SetDestination(hit.position);
                lastKnownPlayerPosition = targetPosition;
            }
            else
            {
                // Если позиция игрока недоступна, пытаемся найти ближайшую точку на NavMesh
                if (NavMesh.FindClosestEdge(targetPosition, out hit, NavMesh.AllAreas))
                {
                    navMeshAgent.SetDestination(hit.position);
                    lastKnownPlayerPosition = hit.position;
                }
            }
        }
    }

    /// <summary>
    /// Переключить состояние существа на Спокойствие
    /// </summary>
    public void SetCalmState()
    {
        SetState(CreatureState.Calm);
    }

    /// <summary>
    /// Переключить состояние существа на Преследование
    /// </summary>
    public void SetChasingState()
    {
        SetState(CreatureState.Chasing);
    }

    /// <summary>
    /// Установить состояние существа
    /// </summary>
    public void SetState(CreatureState newState)
    {
        if (currentState == newState) return;

        CreatureState previousState = currentState;
        currentState = newState;

        OnStateChanged(previousState, newState);
    }

    public void SetStateWithAnimator(CreatureState newState)
    {
        if(newState == CreatureState.Calm)
        {
            animator.SetBool("Chasing", false);
        }
        else
        {
            animator.SetBool("Chasing", true);
        }
    }

    private void OnStateChanged(CreatureState previousState, CreatureState newState)
    {
        if (navMeshAgent == null || !navMeshAgent.isActiveAndEnabled) return;

        // Настраиваем параметры NavMeshAgent в зависимости от состояния
        switch (newState)
        {
            case CreatureState.Calm:
                navMeshAgent.speed = calmMoveSpeed;
                navMeshAgent.angularSpeed = calmRotateSpeed;
                // Останавливаем движение
                if (navMeshAgent.isOnNavMesh && navMeshAgent.hasPath)
                {
                    navMeshAgent.ResetPath();
                }
                break;

            case CreatureState.Chasing:
                navMeshAgent.speed = chaseSpeed;
                navMeshAgent.angularSpeed = chaseRotateSpeed;
                // Начинаем преследование
                if (playerTarget != null)
                {
                    UpdatePathToPlayer();
                }
                break;
        }
        // Обновляем зацикленный звук движения
        UpdateMovementSound(newState);
    }

    public void PlayStateChangeSound(CreatureState state)
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = null;
        switch (state)
        {
            case CreatureState.Chasing:
                clipToPlay = chaseStartClip;
                break;
            case CreatureState.Calm:
                clipToPlay = calmClip;
                break;
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    private void UpdateMovementSound(CreatureState state)
    {
        if (audioSource == null || movementLoopClip == null) return;

        if (state == CreatureState.Chasing)
        {
            if (audioSource.clip != movementLoopClip || !audioSource.isPlaying)
            {
                audioSource.clip = movementLoopClip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.clip == movementLoopClip && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }

    /// <summary>
    /// Установить цель для преследования
    /// </summary>
    public void SetTarget(Transform target)
    {
        playerTarget = target;
    }

    /// <summary>
    /// Получить текущую цель
    /// </summary>
    public Transform GetTarget()
    {
        return playerTarget;
    }

    private void OnDrawGizmosSelected()
    {
        // Рисуем дистанцию обнаружения
        if (currentState == CreatureState.Calm)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionDistance);
        }

        // Рисуем дистанцию остановки
        if (currentState == CreatureState.Chasing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stopDistance);
        }

        // Рисуем путь NavMeshAgent
        if (navMeshAgent != null && navMeshAgent.hasPath)
        {
            Gizmos.color = Color.cyan;
            Vector3[] corners = navMeshAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }

        // Рисуем линию к игроку
        if (playerTarget != null && currentState == CreatureState.Chasing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTarget.position);
        }
    }
}
