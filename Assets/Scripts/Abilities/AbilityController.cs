using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AbilityController : MonoBehaviour
{
    [Tooltip("Index 0 = Fire, 1 = Fly, 2 = Tail")]
    [SerializeField] private AbilityData[] abilities = new AbilityData[3];

    [Header("Basic Attack(Right click)")]
    [SerializeField] private AbilityData basicAttack;

    [Header("Shared")]
    [SerializeField] private Transform castPoint;
    [SerializeField] private ParticleSystem fireAttackParticles;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private Animator animator;
    [SerializeField, Range(0f, 1f)] private float abilitySfxVolume = 1f;
    [SerializeField, Min(0f)] private float recoveryTime = 0.15f;
    [SerializeField, Min(0.1f)] private float flyTravelDuration = 1f;

    private readonly Dictionary<AbilityData, float> _cooldownTimers = new();
    private List<AbilityData> _allTracked;
    private Health _health;
    private NavMeshAgent _agent;
    private Coroutine _castRoutine;
    private bool _isCasting;
    private AbilityData _castingAbility;

    public event Action<AbilityData> OnAbilityCast;
    public event Action<AbilityData, float> OnCooldownUpdated;

    public AbilityData[] Abilities => abilities;
    public float BasicAttackRange => basicAttack != null ? basicAttack.range : 0f;
    public bool IsCasting => _isCasting;
    public bool IsBasicAttackCasting => _isCasting && _castingAbility == basicAttack;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _agent = GetComponent<NavMeshAgent>();
        if (sfxSource == null && Camera.main != null)
        {
            sfxSource = Camera.main.GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = Camera.main.gameObject.AddComponent<AudioSource>();
        }
        AttachCastPointToHead();
        _allTracked = new List<AbilityData>(abilities);
        if (basicAttack != null) _allTracked.Add(basicAttack);
    }

    private void Update()
    {
        foreach (var ability in _allTracked)
        {
            if (ability == null) continue;
            if (_cooldownTimers.TryGetValue(ability, out var t) && t > 0f)
            {
                t -= Time.deltaTime;
                _cooldownTimers[ability] = Mathf.Max(0f, t);
                OnCooldownUpdated?.Invoke(ability, _cooldownTimers[ability] / ability.cooldown);
            }
        }
    }

    public bool IsReady(int index)
    {
        if (index < 0 || index >= abilities.Length || abilities[index] == null) return false;
        return IsReady(abilities[index]);
    }

    public bool IsBasicAttackReady() => basicAttack != null && IsReady(basicAttack);

    private bool IsReady(AbilityData ability)
    {
        bool canCastDuringBasicAttack = _isCasting && _castingAbility == basicAttack && ability != basicAttack;
        return (!_isCasting || canCastDuringBasicAttack) &&
               (!_cooldownTimers.TryGetValue(ability, out var t) || t <= 0f);
    }

    public void TryCast(int index)
    {
        if (!IsReady(index)) return;
        Vector3 forwardTarget = transform.position + transform.forward * abilities[index].range;
        StartCast(abilities[index], forwardTarget);
    }

    public void TryCast(int index, Vector3 targetPoint)
    {
        if (!IsReady(index)) return;
        StartCast(abilities[index], targetPoint);
    }

    public void TryCastBasicAttack()
    {
        if (!IsBasicAttackReady()) return;
        StartCast(basicAttack, transform.position);
    }

    private void StartCast(AbilityData ability, Vector3 targetPoint)
    {
        if (_isCasting && _castingAbility == basicAttack)
        {
            StopCoroutine(_castRoutine);
            _isCasting = false;
            _castingAbility = null;
        }

        _castRoutine = StartCoroutine(CastRoutine(ability, targetPoint));
    }

    private IEnumerator CastRoutine(AbilityData ability, Vector3 targetPoint)
    {
        _isCasting = true;
        _castingAbility = ability;
        _cooldownTimers[ability] = ability.cooldown;
        OnAbilityCast?.Invoke(ability);

        if (animator != null && !string.IsNullOrEmpty(ability.animationTrigger))
            animator.SetTrigger(ability.animationTrigger);

        if (ability.type == AbilityType.Fly)
        {
            Vector3 landingPosition = transform.position;
            bool reachedLanding = false;
            yield return FlyToTarget(ability, targetPoint, (position, success) =>
            {
                landingPosition = position;
                reachedLanding = success;
            });

            if (reachedLanding)
            {
                if (ability.sfxClip != null && sfxSource != null)
                    sfxSource.PlayOneShot(ability.sfxClip, abilitySfxVolume);

                if (_health == null || !_health.IsDead)
                    ResolveDamageAt(ability, landingPosition);
            }
        }
        else
        {
            yield return new WaitForSeconds(ability.castTime);

            if (ability.sfxClip != null && sfxSource != null)
                sfxSource.PlayOneShot(ability.sfxClip, abilitySfxVolume);

            if (_health == null || !_health.IsDead)
            {
                if (ability.type == AbilityType.Fire && fireAttackParticles != null)
                {
                    if (!fireAttackParticles.gameObject.activeSelf)
                        fireAttackParticles.gameObject.SetActive(true);
                    fireAttackParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    fireAttackParticles.Play(true);
                }

                ResolveDamage(ability);
            }

            if (ability.type == AbilityType.Fire && fireAttackParticles != null)
            {
                yield return WaitForFireAnimation(ability);
                fireAttackParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        yield return new WaitForSeconds(recoveryTime);
        _isCasting = false;
        _castingAbility = null;
        _castRoutine = null;
    }

    private IEnumerator FlyToTarget(AbilityData ability, Vector3 targetPoint, Action<Vector3, bool> onLanded)
    {
        Vector3 start = transform.position;
        Vector3 direction = targetPoint - start;
        direction.y = 0f;
        bool canWarpAgent = _agent != null && _agent.enabled && _agent.isOnNavMesh;
        bool pathFound = false;
        List<Vector3> route = new() { start };
        float routeLength = 0f;

        if (canWarpAgent && direction.sqrMagnitude > 0.001f)
        {
            Vector3 requestedLanding = start + direction.normalized * Mathf.Min(direction.magnitude, ability.range);
            float sampleRadius = Mathf.Max(0.25f, Mathf.Min(1f, ability.range * 0.1f));
            if (NavMesh.SamplePosition(requestedLanding, out NavMeshHit navHit, sampleRadius, _agent.areaMask))
            {
                NavMeshPath navPath = new();
                if (NavMesh.CalculatePath(start, navHit.position, _agent.areaMask, navPath) &&
                    navPath.status == NavMeshPathStatus.PathComplete)
                {
                    pathFound = true;
                    Vector3[] corners = navPath.corners;
                    float remainingDistance = ability.range;
                    for (int i = 1; i < corners.Length && remainingDistance > 0f; i++)
                    {
                        Vector3 segmentStart = route[route.Count - 1];
                        Vector3 segment = corners[i] - segmentStart;
                        float segmentLength = segment.magnitude;
                        if (segmentLength <= 0.001f) continue;

                        if (segmentLength <= remainingDistance)
                        {
                            route.Add(corners[i]);
                            routeLength += segmentLength;
                            remainingDistance -= segmentLength;
                        }
                        else
                        {
                            route.Add(segmentStart + segment / segmentLength * remainingDistance);
                            routeLength += remainingDistance;
                            remainingDistance = 0f;
                        }
                    }
                }
            }
        }

        Vector3 landing = route[route.Count - 1];

        Vector3 facing = landing - start;
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);

        bool oldUpdatePosition = false;
        bool oldUpdateRotation = false;
        if (canWarpAgent)
        {
            _agent.isStopped = true;
            oldUpdatePosition = _agent.updatePosition;
            oldUpdateRotation = _agent.updateRotation;
            _agent.updatePosition = false;
            _agent.updateRotation = false;
        }

        yield return WaitForFlyAnimationStart(ability);

        float elapsed = 0f;
        while (elapsed < flyTravelDuration && routeLength > 0f)
        {
            if (_health != null && _health.IsDead) break;
            elapsed += Time.deltaTime;
            float distanceAlongRoute = routeLength * Mathf.Clamp01(elapsed / flyTravelDuration);
            transform.position = GetPointAlongRoute(route, distanceAlongRoute);
            yield return null;
        }

        transform.position = landing;
        if (canWarpAgent)
        {
            _agent.Warp(landing);
            _agent.updatePosition = oldUpdatePosition;
            _agent.updateRotation = oldUpdateRotation;
            _agent.isStopped = true;
        }

        onLanded?.Invoke(landing, pathFound);
    }

    private Vector3 GetPointAlongRoute(List<Vector3> route, float distance)
    {
        for (int i = 1; i < route.Count; i++)
        {
            float segmentLength = Vector3.Distance(route[i - 1], route[i]);
            if (distance <= segmentLength)
                return Vector3.Lerp(route[i - 1], route[i], segmentLength > 0f ? distance / segmentLength : 0f);

            distance -= segmentLength;
        }

        return route[route.Count - 1];
    }

    private IEnumerator WaitForFlyAnimationStart(AbilityData ability)
    {
        if (animator == null) yield break;

        float timeout = 1f;
        while (timeout > 0f)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(ability.animationTrigger)) yield break;
            if (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(ability.animationTrigger))
                yield break;

            timeout -= Time.deltaTime;
            yield return null;
        }
    }

    private void AttachCastPointToHead()
    {
        if (castPoint == null || animator == null) return;

        Transform[] bones = animator.GetComponentsInChildren<Transform>(true);
        foreach (Transform bone in bones)
        {
            if (!string.Equals(bone.name, "Head", StringComparison.OrdinalIgnoreCase)) continue;
            castPoint.SetParent(bone, true);
            return;
        }
    }

    private IEnumerator WaitForFireAnimation(AbilityData ability)
    {
        if (animator == null)
        {
            yield return new WaitForSeconds(fireAttackParticles.main.duration);
            yield break;
        }

        bool animationStarted = false;
        float elapsed = 0f;
        float timeout = Mathf.Max(ability.castTime, fireAttackParticles.main.duration) + 1f;

        while (elapsed < timeout)
        {
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            bool isFireState = currentState.IsName(ability.animationTrigger);

            if (animator.IsInTransition(0))
                isFireState |= animator.GetNextAnimatorStateInfo(0).IsName(ability.animationTrigger);

            if (isFireState)
                animationStarted = true;
            else if (animationStarted)
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void ResolveDamage(AbilityData ability)
    {
        Vector3 center = castPoint.position + castPoint.forward * (ability.range * 0.5f);
        ResolveDamageAt(ability, center);
    }

    private void ResolveDamageAt(AbilityData ability, Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, ability.range * 0.5f, targetLayer);

        HashSet<Health> damagedHealth = new();
        foreach (var hit in hits)
        {
            Health health = hit.GetComponentInParent<Health>();
            if (health != null && damagedHealth.Add(health))
            {
                health.TakeDamage(ability.damage, hit.ClosestPoint(center));
            }
        }
    }
}
