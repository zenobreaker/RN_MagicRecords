using UnityEngine;
using UnityEngine.AI;

public class LaunchComponent : MonoBehaviour
{
    [SerializeField, Min(1)] private int ChangeFrame = 5;
    private Rigidbody rigid;
    private NavMeshAgent agent;
    private StateComponent state;
    private Vector3 launchVelocity;
    private float remainingTime;
    private bool restoreAgent;
    private bool restoreAgentPosition;
    public bool IsDistancePush
    {
        get;
        private set;
    }
    public bool IsLaunching
    {
        get;
        private set;
    }
    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        state = GetComponent<StateComponent>();
    }

    public void ApplyLaunch(GameObject attacker, Weapon causer, HitData hitData)
    {
        if (hitData == null)
        {
            return;
        }

        ApplyKnockback(attacker, transform.position, hitData.Distance,
                    Mathf.Max(1, ChangeFrame) * Time.fixedDeltaTime);
    }


    public bool ApplyKnockback(GameObject attacker, Vector3 hitPoint, float power, float duration)
    {
        return BeginPush(attacker, hitPoint, power, duration, false);
    }

    /// <summary>피격 데이터의 월드 거리를 물리 프레임에 나누어 이동합니다.</summary>
    public bool ApplyHitPush(GameObject attacker, Vector3 hitPoint, float distance, float duration)
    {
        return BeginPush(attacker, hitPoint, distance, duration, true);
    }

    private bool BeginPush(GameObject attacker, Vector3 hitPoint, float amount, float duration, bool useDistance)
    {
        if (isActiveAndEnabled == false || rigid == null || rigid.isKinematic ||
            (state != null && (state.DeadMode || state.StopMode)) ||
            float.IsFinite(amount) == false || amount <= 0f || float.IsFinite(duration) == false || duration <= 0f)
        {
            return false;
        }

        Vector3 direction = attacker != null ? transform.position - attacker.transform.position : transform.position - hitPoint;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = attacker != null ? attacker.transform.forward : -transform.forward;
            direction.y = 0f;
        }
        if (float.IsFinite(direction.x) == false || float.IsFinite(direction.z) == false || direction.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        CancelLaunch();
        IsDistancePush = useDistance;
        restoreAgent = useDistance == false && agent != null && agent.enabled;
        if (restoreAgent)
        {
            agent.enabled = false;
        }

        remainingTime = duration;
        launchVelocity = direction.normalized * (useDistance ? amount / duration : amount);
        IsLaunching = true;
        if (useDistance)
        {
            // 경로를 지우지 않고 위치 제어만 잠시 물리 이동에 맡깁니다.
            restoreAgentPosition = agent != null && agent.enabled && agent.updatePosition;
            if (restoreAgentPosition)
            {
                agent.updatePosition = false;
            }

            rigid.linearVelocity = new Vector3(0f, rigid.linearVelocity.y, 0f);
        }
        else
        {
            ApplyVelocity();
        }
        return true;
    }

    private void FixedUpdate()
    {
        if (IsLaunching == false)
        {
            return;
        }

        if (remainingTime <= 0f || rigid == null || rigid.isKinematic ||
                    (state != null && (state.DeadMode || state.StopMode)))
        {
            CancelLaunch();
            return;
        }
        float stepTime = Mathf.Min(remainingTime, Time.fixedDeltaTime);
        if (IsDistancePush)
        {
            MoveHitPush(launchVelocity * stepTime);
        }
        else
        {
            ApplyVelocity();
        }
        remainingTime -= stepTime;
    }

    /// <summary>벽과 이동 가능한 바닥의 경계를 넘지 않는 수평 이동입니다.</summary>
    private void MoveHitPush(Vector3 step)
    {
        Vector3 start = rigid.position;
        Vector3 direction = step.normalized;
        float distance = step.magnitude;
        if (rigid.SweepTest(direction, out RaycastHit obstacle, distance, QueryTriggerInteraction.Ignore))
        {
            distance = Mathf.Max(0f, obstacle.distance - Physics.defaultContactOffset);
        }
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.nextPosition = start;
            if (agent.Raycast(start + direction * distance, out NavMeshHit edge))
            {
                distance = Mathf.Clamp(Vector3.Dot(edge.position - start, direction), 0f, distance);
            }
        }

        rigid.MovePosition(start + direction * distance);
    }

    private void ApplyVelocity()
    {
        rigid.linearVelocity = new Vector3(launchVelocity.x, rigid.linearVelocity.y, launchVelocity.z);
    }

    public void CancelLaunch()
    {
        if (IsLaunching && rigid != null && rigid.isKinematic == false)
        {
            rigid.linearVelocity = new Vector3(0f, rigid.linearVelocity.y, 0f);
        }

        IsLaunching = false;
        IsDistancePush = false;
        remainingTime = 0f;
        if (restoreAgentPosition && agent != null)
        {
            if (agent.enabled && agent.isOnNavMesh && rigid != null)
            {
                agent.nextPosition = rigid.position;
            }
            agent.updatePosition = true;
        }
        restoreAgentPosition = false;
        if (restoreAgent && agent != null)
        {
            agent.enabled = true;
        }

        restoreAgent = false;
    }
    private void OnDisable()
    {
        CancelLaunch();
    }
}
