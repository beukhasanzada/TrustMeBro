using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavChaser : MonoBehaviour
{
    private enum State { Wander, Chase }

    [Header("Обнаружение")]
    [SerializeField] private float detectRange = 25f;   // с какого расстояния замечает игрока
    [SerializeField] private float loseRange = 35f;     // дальше этого теряет игрока

    [Header("Движение")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float stoppingDistance = 1.5f;   // на каком расстоянии останавливается у игрока
    [SerializeField] private float repathInterval = 0.2f;     // как часто обновляет путь (сек)

    [Header("Патруль")]
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float wanderWaitTime = 2f;       // пауза у точки

    [Header("Атака (ближний бой)")]
    [SerializeField] private bool canAttack = true;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField, Range(0.1f, 1f)] private float slowMultiplier = 0.4f;
    [SerializeField] private float slowDuration = 2f;

    [Header("Отладка")]
    [SerializeField] private bool debugLog = false;

    private NavMeshAgent agent;
    private Transform player;
    private State state = State.Wander;

    private Vector3 spawnPoint;
    private float nextRepathTime;
    private float wanderWaitTimer;
    private float nextAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        else Debug.LogWarning("NavChaser: не найден объект с тегом Player!");
    }

    private void Start()
    {
        spawnPoint = transform.position;

        if (!agent.isOnNavMesh)
            Debug.LogWarning("NavChaser: враг стоит не на NavMesh! Запеки навигацию (Bake) и проверь высоту врага.", this);

        StartWander();
    }

    private void Update()
    {
        if (player == null || !agent.isOnNavMesh) return;

        // Игра окончена: стоим
        if (GameTimer.Instance != null && GameTimer.Instance.isOver)
        {
            agent.isStopped = true;
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        // Смена состояний
        if (state == State.Wander && dist <= detectRange)
        {
            state = State.Chase;
            agent.speed = chaseSpeed;
            agent.isStopped = false;
            if (debugLog) Debug.Log("Враг заметил игрока");
        }
        else if (state == State.Chase && dist > loseRange)
        {
            StartWander();
        }

        if (state == State.Chase)
            DoChase(dist);
        else
            DoWander();
    }

    // ===== Погоня =====

    private void DoChase(float dist)
    {
        // Путь обновляем не каждый кадр, а раз в repathInterval
        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + repathInterval;
            agent.SetDestination(player.position);
        }

        // Рядом с игроком: смотрим на него
        if (dist <= stoppingDistance + 0.5f)
            FaceTarget(player.position);

        // Атака
        if (canAttack && dist <= attackRange && Time.time >= nextAttackTime)
            Attack();
    }

    private void Attack()
    {
        nextAttackTime = Time.time + attackCooldown;

        PlayerSlow slow = player.GetComponent<PlayerSlow>();
        if (slow != null)
            slow.ApplySlow(slowMultiplier, slowDuration);

        if (debugLog) Debug.Log("Враг ударил игрока");
    }

    // ===== Патруль =====

    private void StartWander()
    {
        state = State.Wander;
        agent.speed = wanderSpeed;
        agent.isStopped = false;
        wanderWaitTimer = 0f;
        PickWanderPoint();
    }

    private void DoWander()
    {
        // Дошли до точки: ждём и выбираем новую
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            wanderWaitTimer += Time.deltaTime;
            if (wanderWaitTimer >= wanderWaitTime)
            {
                wanderWaitTimer = 0f;
                PickWanderPoint();
            }
        }
    }

    private void PickWanderPoint()
    {
        Vector3 random = spawnPoint + Random.insideUnitSphere * wanderRadius;

        // Берём ближайшую точку на NavMesh, чтобы не отправить врага в стену
        if (NavMesh.SamplePosition(random, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            8f * Time.deltaTime);
    }
}