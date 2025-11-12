using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Контроллер существа, которое преследует игрока
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CreatureController : MonoBehaviour
{
    private const float MIN_DISTANCE_TO_PLAYER = 0.5f;
    private const float GROUNDED_STICK = -1f;

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

    [Header("Audio")]
    [Tooltip("Звук при переходе в режим преследования")]
    [SerializeField] private AudioClip chaseStartClip;
    [Tooltip("Звук при переходе в спокойное состояние")]
    [SerializeField] private AudioClip calmClip;
    [Tooltip("Звук дыхания/движения (зацикленный)")]
    [SerializeField] private AudioClip movementLoopClip;

    private CreatureState currentState;
    private float verticalVelocity = 0f;
    private Vector3 lastKnownPlayerPosition;

    // Public API
    public CreatureState CurrentState => currentState;
    public bool IsChasing => currentState == CreatureState.Chasing;
    public bool IsCalm => currentState == CreatureState.Calm;

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

    }

    private void UpdateChasingState()
    {
        if (playerTarget == null) return;

        Vector3 toPlayer = playerTarget.position - transform.position;
        Vector3 flatToPlayer = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
        float distanceToPlayer = flatToPlayer.magnitude;

        // Если слишком близко к игроку, останавливаемся
        if (distanceToPlayer <= stopDistance)
        {
            FaceDirection(flatToPlayer.normalized);
            return;
        }

        // Движемся к игроку
        Vector3 moveDirection = flatToPlayer.normalized;
        Vector3 movement = moveDirection * chaseSpeed;
        Vector3 motion = movement + Vector3.up * verticalVelocity;

        characterController.Move(motion * Time.deltaTime);
        FaceDirection(moveDirection);

        lastKnownPlayerPosition = playerTarget.position;
    }

    private void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;

        float rotateSpeed = currentState == CreatureState.Chasing ? chaseRotateSpeed : calmRotateSpeed;
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
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
        // Останавливаем движение при смене состояния
        verticalVelocity = 0f;

        // Обновляем анимации
        bool isChasing = newState == CreatureState.Chasing;

        // Проигрываем звуки
        PlayStateChangeSound(newState);

        // Обновляем зацикленный звук движения
        UpdateMovementSound(newState);
    }

    private void PlayStateChangeSound(CreatureState state)
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

        // Рисуем линию к игроку
        if (playerTarget != null && currentState == CreatureState.Chasing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTarget.position);
        }
    }
}

