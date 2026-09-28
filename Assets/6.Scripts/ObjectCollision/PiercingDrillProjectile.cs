using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PiercingDrillProjectile
    : AbstractProjectile
{
    [Header("Piercing Drill Settings")]
    [Tooltip("관통 중일 때의 속도 배율 (0.2 = 평소 속도의 20%로 감속)")]
    [SerializeField] private float drillingSpeedRatio = 0.2f;
    [Tooltip("다단히트 데미지 간격 (초)")]
    [SerializeField] private float tickInterval = 0.2f;

    // --- 내부 변수 ---
    // 드릴 상태 추적
    private HashSet<Collider> currentTargets = new HashSet<Collider>();
    private float tickTimer = 0f;
    private bool isAttached;
    private GameObject attachedTarget;
    private bool drillDefaultsCached;
    private float defaultDrillingSpeedRatio;
    private readonly List<Collider> tickTargets = new List<Collider>();

    public bool IsAttached => isAttached;
    public float MovementSpeed => isAttached ? 0f :
        force * (currentTargets.Count > 0 ? drillingSpeedRatio : 1f);

    protected override void Awake()
    {
        base.Awake();
        CacheDrillDefaults();
    }

    private void CacheDrillDefaults()
    {
        if (drillDefaultsCached) return;
        drillDefaultsCached = true;
        defaultDrillingSpeedRatio = drillingSpeedRatio;
    }


    protected override void OnProjectileSpawned()
    {
        // 총알이 깨어날 때 드릴 전용 변수만 초기화 (리지드바디 속도 등은 부모가 해줌)
        currentTargets.Clear();
        tickTimer = 0f;
        isAttached = false;
        attachedTarget = null;
    }

    protected override void OnProjectileDespawned()
    {
        // 풀로 돌아갈 때 리스트 청소
        currentTargets.Clear();
        tickTargets.Clear();
        tickTimer = 0f;
        drillingSpeedRatio = defaultDrillingSpeedRatio;
        isAttached = false;
        attachedTarget = null;
    }

    protected override void OnProjectileUpdate()
    {
        if (isAttached)
        {
            if (attachedTarget == null || !attachedTarget.activeInHierarchy)
            {
                gameObject.SetActive(false);
                return;
            }

            // 부착 후에는 접촉 콜라이더 목록과 무관하게 해당 적에게만 지속 피해.
            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                tickTimer = 0f;
                DealDamage(attachedTarget, attachedTarget.transform.InverseTransformPoint(transform.position));
            }
            return;
        }

        // 1. 다단 히트 (Tick) 로직 (수명 깎는 건 부모가 해줌)
        if (currentTargets.Count > 0)
        {
            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                tickTimer = 0f;
                tickTargets.Clear();
                tickTargets.AddRange(currentTargets);
                for (int i = 0; i < tickTargets.Count && isActiveAndEnabled; i++)
                {
                    Collider target = tickTargets[i];
                    if (target == null || !target.gameObject.activeInHierarchy) continue;
                    DealDamage(target.gameObject, target.transform.position);
                    if (isAttached) break;
                }
            }
        }
    }

    protected override void ProcessHit(Collider other)
    {
        if (isAttached) return;
        // 2. 관통 시작: 목록에 추가하고 즉시 1타 데미지
        if (currentTargets.Add(other))
        {
            Vector3 hitPoint = collider.ClosestPoint(other.transform.position);
            hitPoint = other.transform.InverseTransformPoint(hitPoint);

            DealDamage(other.gameObject, hitPoint);
        }
    }


    private void FixedUpdate()
    {
        if (rigid == null) return;

        // 부착 중에는 관통 속도 복원 로직이 다시 탄두를 밀어내지 않도록 합니다.
        if (isAttached)
        {
            rigid.linearVelocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
            return;
        }

        currentTargets.RemoveWhere(c => c == null || !c.gameObject.activeInHierarchy);
        // 유도 방향은 유지하고, 이번 발사의 속도를 기준으로 감속합니다.
        Vector3 direction = rigid.linearVelocity.sqrMagnitude > 0f
            ? rigid.linearVelocity.normalized : transform.forward;
        rigid.linearVelocity = direction * MovementSpeed;
    }

    
    private void OnTriggerExit(Collider other)
    {
        if (isAttached) return;
        if (IsFriendlyFire(other.gameObject))
            return;

        // 완전히 뚫고 지나가면 목록에서 제거 (속도 원상복구용)
        if (currentTargets.Contains(other))
        {
            currentTargets.Remove(other);
        }
    }

    public void SetDrilingSpeedRatio(float ratio)
    {
        CacheDrillDefaults();
        drillingSpeedRatio = Mathf.Clamp01(ratio);
    }

    public void AttachToTarget(GameObject target)
    {
        if (isAttached || target == null) return;
        isAttached = true;
        attachedTarget = target;
        tickTimer = 0f;
        if (rigid != null)
        {
            rigid.linearVelocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }
    }
}
