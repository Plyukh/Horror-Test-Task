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

    private CreatureState currentState;
    private Vector3 lastKnownPlayerPosition;
    private float pathUpdateTimer = 0f;

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
    }

    private void Start()
    {
        SetStateWithAnimator(initialState);
    }

    private void Update()
    {
        UpdateState();
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

        // Обновляем путь к игроку с интервалом
        pathUpdateTimer += Time.deltaTime;
        if (pathUpdateTimer >= pathUpdateInterval)
        {
            pathUpdateTimer = 0f;
            UpdatePathToPlayer();
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
