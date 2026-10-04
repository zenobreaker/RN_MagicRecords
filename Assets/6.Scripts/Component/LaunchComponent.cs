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
    public bool IsLaunching { get; private set; }
    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        state = GetComponent<StateComponent>();
    }

    public void ApplyLaunch(GameObject attacker, Weapon causer, HitData hitData)
    {
        if (hitData == null) return;
        ApplyKnockback(attacker, transform.position, hitData.Distance,
            Mathf.Max(1, ChangeFrame) * Time.fixedDeltaTime);
    }


    public bool ApplyKnockback(GameObject attacker, Vector3 hitPoint, float power, float duration)
    {
        if (!isActiveAndEnabled || rigid == null || rigid.isKinematic ||
            (state != null && (state.DeadMode || state.StopMode)) ||
            !float.IsFinite(power) || power <= 0f || !float.IsFinite(duration) || duration <= 0f) return false;
        Vector3 direction = attacker != null ? transform.position - attacker.transform.position : transform.position - hitPoint;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = attacker != null ? attacker.transform.forward : -transform.forward;
            direction.y = 0f;
        }
        if (!float.IsFinite(direction.x) || !float.IsFinite(direction.z) || direction.sqrMagnitude < 0.0001f)
            return false;

        CancelLaunch();
        restoreAgent = agent != null && agent.enabled;
        if (restoreAgent) agent.enabled = false;
        remainingTime = duration;
        launchVelocity = direction.normalized * power;
        IsLaunching = true;
        ApplyVelocity();
        return true;
    }

    private void FixedUpdate()
    {
        if (!IsLaunching) return;
        if (remainingTime <= 0f || rigid == null || rigid.isKinematic ||
            (state != null && (state.DeadMode || state.StopMode)))
        {
            CancelLaunch();
            return;
        }
        ApplyVelocity();
        remainingTime -= Time.fixedDeltaTime;
    }

    private void ApplyVelocity()
    {
        rigid.linearVelocity = new Vector3(launchVelocity.x, rigid.linearVelocity.y, launchVelocity.z);
    }

    public void CancelLaunch()
    {
        if (IsLaunching && rigid != null && !rigid.isKinematic)
            rigid.linearVelocity = new Vector3(0f, rigid.linearVelocity.y, 0f);
        IsLaunching = false;
        remainingTime = 0f;
        if (restoreAgent && agent != null) agent.enabled = true;
        restoreAgent = false;
    }
    private void OnDisable() => CancelLaunch();
}
