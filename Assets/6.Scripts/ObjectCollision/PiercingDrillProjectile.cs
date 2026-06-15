using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PiercingDrillProjectile 
    : BaseProjectile
    , ISkillEffect
{
    [Header("Projectile Settings")]
    [SerializeField] private float force = 1000.0f;
    [SerializeField] private float life = 5.0f;
    [SerializeField] private LayerMask ignoreLayer;
    [SerializeField] private string bombEffectName = "";
    [SerializeField] private string impactSoundName = "";

    [Header("Piercing Drill Settings")]
    [Tooltip("관통 중일 때의 속도 배율 (0.2 = 평소 속도의 20%로 감속)")]
    [SerializeField] private float drillingSpeedRatio = 0.2f;
    [Tooltip("다단히트 데미지 간격 (초)")]
    [SerializeField] private float tickInterval = 0.2f;

    // --- 내부 변수 ---
    private float curLife = 0f;
    private Rigidbody rigid;
    private new Collider collider;


    // 드릴 상태 추적
    private HashSet<Collider> currentTargets = new HashSet<Collider>();
    private float tickTimer = 0f;
    private Vector3 normalVelocity;
    private bool isVelocityCaptured = false;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        collider = GetComponent<Collider>();
    }

    
    // =======================================================
    // 🔄 생명주기 및 물리 이동 로직
    // =======================================================
    private void OnEnable()
    {
        if (rigid == null) return;

        rigid.linearVelocity = Vector3.zero;
        rigid.AddForce(transform.forward * force);
        curLife = life;

        currentTargets.Clear();
        tickTimer = 0f;
        isVelocityCaptured = false;
    }

    protected override  void OnDisable()
    {
        base.OnDisable();

        ObjectPooler.ReturnToPool(this.gameObject);
        currentTargets.Clear();

        if (rigid != null)
        {
            rigid.linearVelocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }
    }

    private void Update()
    {
        // 1. 수명 관리
        if (life != -1)
        {
            curLife -= Time.deltaTime;
            if (curLife <= 0f)
            {
                this.gameObject.SetActive(false);
                return;
            }
        }

        // 2. 다단 히트 (Tick) 로직
        if (currentTargets.Count > 0)
        {
            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                tickTimer = 0f;
                foreach (Collider target in currentTargets)
                {
                    ApplyDamage(target);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (rigid == null) return;

        // AddForce 이후의 정상 속도 캡처
        if (!isVelocityCaptured && rigid.linearVelocity.sqrMagnitude > 0)
        {
            normalVelocity = rigid.linearVelocity;
            isVelocityCaptured = true;
        }

        // 드릴 마찰(감속) 처리
        if (isVelocityCaptured)
        {
            // 방어 코드: 죽은 적 리스트에서 청소
            currentTargets.RemoveWhere(c => c == null || !c.gameObject.activeInHierarchy);

            bool isDrilling = currentTargets.Count > 0;
            rigid.linearVelocity = isDrilling ? normalVelocity * drillingSpeedRatio : normalVelocity;
        }
    }

    // =======================================================
    // ⚔️ 충돌 및 데미지 로직
    // =======================================================
    private void OnTriggerEnter(Collider other)
    {
        if (ignores.Contains(other.gameObject)) return;
        if (ignoreLayer.Contains(other.gameObject)) return;
        if (IsFriendlyFire(other.gameObject))
            return;

        // 관통 시작: 목록에 추가하고 즉시 1타 데미지
        if (currentTargets.Add(other))
        {
            ApplyDamage(other);

            // Play Sound
            {
                SoundManager.Instance.SafeInvoke(v => v.PlaySFX(impactSoundName));
            }

            // 관통 이펙트 재생 (필요시)
            if (!string.IsNullOrEmpty(bombEffectName))
            {
                ObjectPooler.SpawnFromPool(bombEffectName, transform.position);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsFriendlyFire(other.gameObject))
            return;

        // 완전히 뚫고 지나가면 목록에서 제거 (속도 원상복구용)
        if (currentTargets.Contains(other))
        {
            currentTargets.Remove(other);
        }
    }

    private void ApplyDamage(Collider target)
    {
        IDamagable damage = target.GetComponent<IDamagable>();
        if (damage != null)
        {
            Vector3 hitPoint = collider.ClosestPoint(target.transform.position);
            hitPoint = target.transform.InverseTransformPoint(hitPoint);

            damage.OnDamage(ownerObject, null, hitPoint, damageEvent);
        }
    }
}