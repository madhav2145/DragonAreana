using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AbilityController))]
public class EnemyAI : MonoBehaviour
{
    private enum State { Idle, Chase, Attack }

    [SerializeField] private Transform target;          // the player dragon's transform
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float tailRange = 2.5f;
    [SerializeField] private float fireRange = 8f;
    [SerializeField] private float flyRangeMin = 4f;
    [SerializeField] private float decisionInterval = 0.5f;
    [SerializeField] private Animator animator;

    private NavMeshAgent _agent;
    private AbilityController _abilities;
    private Health _health;
    private Health _targetHealth;
    private State _state = State.Idle;
    private float _decisionTimer;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _abilities = GetComponent<AbilityController>();
        _health = GetComponent<Health>();
        _targetHealth = target != null ? target.GetComponent<Health>() : null;
    }

    private void OnEnable()
    {
        if (_health != null) _health.OnDeath += HandleDeath;
        if (_targetHealth != null) _targetHealth.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (_health != null) _health.OnDeath -= HandleDeath;
        if (_targetHealth != null) _targetHealth.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        if (target == null || IsCombatantDead())
        {
            StopAgent();
            UpdateAnimator();
            return;
        }

        // Don't move or replan mid-cast - keeps attacks readable and fair
        if (_abilities.IsCasting)
        {
            _agent.isStopped = true;
            UpdateAnimator();
            return;
        }

        _decisionTimer -= Time.deltaTime;
        if (_decisionTimer <= 0f)
        {
            _decisionTimer = decisionInterval;
            Decide();
        }

        Act();
        UpdateAnimator();
    }

    private float DistanceToTarget() => Vector3.Distance(transform.position, target.position);

    private void Decide()
    {
        float distance = DistanceToTarget();
        if (distance > detectionRange)
            _state = State.Idle;
        else if (distance > fireRange)
            _state = State.Chase;
        else
            _state = State.Attack;
    }

    private void Act()
    {
        switch (_state)
        {
            case State.Chase:
                _agent.isStopped = false;
                _agent.stoppingDistance = fireRange;
                _agent.SetDestination(target.position);
                break;

            case State.Attack:
                _agent.isStopped = true;
                FaceTarget();
                ChooseAbility(DistanceToTarget());
                break;

            case State.Idle:
            default:
                StopAgent();
                break;
        }
    }

    private void ChooseAbility(float dist)
    {
        // Distance-based choice as required by the brief: melee when close,
        // ranged fire at mid/long distance, fly as an occasional wildcard.
        if (dist <= tailRange && _abilities.IsReady(1))
        {
            _abilities.TryCast(1);
        }
        else if (dist > tailRange && dist <= fireRange && _abilities.IsReady(0))
        {
            _abilities.TryCast(0);
        }
        else if (dist >= flyRangeMin && _abilities.IsReady(2))
        {
            _abilities.TryCast(2, target.position);
        }
        else if (_abilities.IsBasicAttackReady())
        {
            if (dist <= _abilities.BasicAttackRange)
            {
                _abilities.TryCastBasicAttack();
            }
            else
            {
                _agent.isStopped = false;
                _agent.stoppingDistance = _abilities.BasicAttackRange;
                _agent.SetDestination(target.position);
            }
        }
        else
        {
            // Stay at the edge of attack range while abilities recover.
            _agent.isStopped = true;
        }
    }

    private void FaceTarget()
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetBool("IsMoving", !_agent.isStopped && _agent.velocity.sqrMagnitude > 0.1f);
        animator.SetBool("InRange", _state == State.Attack && !IsCombatantDead());
    }

    private bool IsCombatantDead()
    {
        return (_health != null && _health.IsDead) ||
               (_targetHealth != null && _targetHealth.IsDead);
    }

    private void HandleDeath()
    {
        _state = State.Idle;
        StopAgent();
        UpdateAnimator();
    }

    private void StopAgent()
    {
        if (!_agent.enabled || !_agent.isOnNavMesh) return;
        _agent.isStopped = true;
        _agent.ResetPath();
    }
}
