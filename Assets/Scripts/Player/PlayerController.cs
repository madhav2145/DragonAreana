using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AbilityController))]
public class PlayerController : MonoBehaviour
{
    private enum Order { None, MoveToPoint, MoveToAttack, Attacking }

    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask clickMask;

    [SerializeField] private GameObject moveIcon;

    [SerializeField] private AudioClip clickSound;
    [SerializeField, Range(0f, 1f)] private float clickSoundVolume = 1f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;

    private NavMeshAgent _agent;
    private AbilityController _abilities;
    private Health _health;

    private Order _order = Order.None;
    private Transform _attackTarget;
    private Collider _attackTargetCollider;
    private Health _attackTargetHealth;
    private int _armedAbilityIndex = -1; // -1 = nothing armed

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _abilities = GetComponent<AbilityController>();
        _health = GetComponent<Health>();
        if (cam == null) cam = Camera.main;
        if (sfxSource == null && cam != null)
        {
            sfxSource = cam.GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = cam.gameObject.AddComponent<AudioSource>();
        }
        _agent.stoppingDistance = 0f;
    }

    private void Update()
    {
        if (_health != null && _health.IsDead)
        {
            _order = Order.None;
            _attackTarget = null;
            _attackTargetCollider = null;
            _attackTargetHealth = null;
            _armedAbilityIndex = -1;
            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
            UpdateAnimator();
            return;
        }

        HandleAbilityArming();
        HandleMouseInput();
        HandleOrderExecution();
        UpdateAnimator();
    }

    private void HandleAbilityArming()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.qKey.wasPressedThisFrame) ArmAbility(0);
        if (keyboard.wKey.wasPressedThisFrame) ArmAbility(1);
        if (keyboard.eKey.wasPressedThisFrame) ArmAbility(2);
        if (keyboard.escapeKey.wasPressedThisFrame) _armedAbilityIndex = -1; // cancel targeting like in Dota 2
    }

    private void ArmAbility(int index)
    {
        if (!_abilities.IsReady(index)) return;
        _armedAbilityIndex = index;
    }

    private void HandleMouseInput()
    {
        if (_abilities.IsCasting && !_abilities.IsBasicAttackCasting) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.rightButton.wasPressedThisFrame) HandleRightClick();
        if (mouse.leftButton.wasPressedThisFrame && _armedAbilityIndex >= 0) HandleLeftClickCast();
    }

    private void HandleRightClick()
    {
        if (!RaycastFromMouse(out RaycastHit hit)) return;
        _armedAbilityIndex = -1; // right-click always cancels ability targeting
        Debug.Log($"Hit {hit.collider.name}, tag={hit.collider.tag}, layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");
        if (hit.collider.CompareTag("Dragon"))
        {
            // Debug.Log("Right-clicked on dragon: " + hit.collider.name);
            _attackTargetHealth = hit.collider.GetComponentInParent<Health>();
            if (_attackTargetHealth == null || _attackTargetHealth.IsDead ||
                _attackTargetHealth == GetComponent<Health>()) return;
            _attackTarget = _attackTargetHealth.transform;
            _attackTargetCollider = hit.collider;
            _order = Order.MoveToAttack;
        }
        else if (hit.collider.CompareTag("Ground"))
        {
            // Debug.Log("Right-clicked on ground: " + hit.point);
            _attackTarget = null;
            _attackTargetCollider = null;
            _attackTargetHealth = null;
            _order = Order.MoveToPoint;
            _agent.isStopped = false;
            _agent.stoppingDistance = 0f;
            _agent.SetDestination(hit.point);
            Instantiate(moveIcon, hit.point + Vector3.up * 0.1f, Quaternion.identity);
            if (clickSound != null && sfxSource != null)
                sfxSource.PlayOneShot(clickSound, clickSoundVolume);
        }
    }

    private void HandleLeftClickCast()
    {
        if (!_abilities.IsReady(_armedAbilityIndex)) { _armedAbilityIndex = -1; return; }
        if (!RaycastFromMouse(out RaycastHit hit)) return;

        bool resumeAttack = _attackTargetHealth != null &&
                            (_order == Order.MoveToAttack || _order == Order.Attacking);
        if (!resumeAttack)
        {
            _order = Order.None;
            _attackTarget = null;
            _attackTargetCollider = null;
            _attackTargetHealth = null;
        }
        _agent.isStopped = true;

        Vector3 facePoint = hit.point;
        facePoint.y = transform.position.y;
        Vector3 dir = facePoint - transform.position;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        _abilities.TryCast(_armedAbilityIndex, hit.point);
        _armedAbilityIndex = -1;
    }

    private bool RaycastFromMouse(out RaycastHit hit)
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || cam == null)
        {
            hit = default;
            return false;
        }

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        return Physics.Raycast(ray, out hit, 200f, clickMask);
    }


    private void HandleOrderExecution()
    {
        if (_abilities.IsCasting) return;

        switch (_order)
        {
            case Order.MoveToPoint:
                if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.05f)
                    _order = Order.None;
                break;

            case Order.MoveToAttack:
                PursueAndAttack();
                break;

            case Order.Attacking:
                KeepAttacking();
                break;
        }
    }

    private void PursueAndAttack()
    {
        if (!HasLiveAttackTarget()) { ClearAttackOrder(); return; }

        float dist = GetAttackTargetSurfaceDistance(out Vector3 destination);
        if (dist > attackRange)
        {
            _agent.isStopped = false;
            _agent.stoppingDistance = attackRange + _agent.radius;
            _agent.SetDestination(destination);
        }
        else
        {
            _agent.isStopped = true;
            _order = Order.Attacking;
        }
    }

    private void KeepAttacking()
    {
        if (!HasLiveAttackTarget()) { ClearAttackOrder(); return; }

        float dist = GetAttackTargetSurfaceDistance(out _);
        if (dist > attackRange * 1.15f) // small buffer so it doesn't flicker in/out of range
        {
            _order = Order.MoveToAttack;
            return;
        }

        FaceTarget(_attackTarget.position);

        if (_abilities.IsBasicAttackReady())
        {
            Debug.Log("Casting basic attack on " + _attackTarget.name);
            _abilities.TryCastBasicAttack();
        }
    }

    private float GetAttackTargetSurfaceDistance(out Vector3 targetPoint)
    {
        targetPoint = _attackTargetCollider != null
            ? _attackTargetCollider.ClosestPoint(transform.position)
            : _attackTarget.position;

        float distanceToSurface = Vector3.Distance(transform.position, targetPoint);
        return Mathf.Max(0f, distanceToSurface - _agent.radius);
    }

    private bool HasLiveAttackTarget()
    {
        return _attackTarget != null && _attackTargetHealth != null && !_attackTargetHealth.IsDead;
    }

    private void ClearAttackOrder()
    {
        _order = Order.None;
        _attackTarget = null;
        _attackTargetCollider = null;
        _attackTargetHealth = null;
        _agent.isStopped = true;
        _agent.stoppingDistance = 0f;
    }

    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        bool moving = !_agent.isStopped && _agent.velocity.sqrMagnitude > 0.1f;
        animator.SetBool("IsMoving", moving);
        bool inRange = _order == Order.Attacking && HasLiveAttackTarget();
        animator.SetBool("InRange", inRange);
    }
}
