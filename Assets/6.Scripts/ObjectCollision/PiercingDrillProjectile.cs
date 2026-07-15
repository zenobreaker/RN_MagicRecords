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
    private Vector3 normalVelocity;
    private bool isVelocityCaptured = false;


    protected override void OnProjectileSpawned()
    {
        // 총알이 깨어날 때 드릴 전용 변수만 초기화 (리지드바디 속도 등은 부모가 해줌)
        currentTargets.Clear();
        tickTimer = 0f;
        isVelocityCaptured = false;
    }

    protected override void OnProjectileDespawned()
    {
        // 풀로 돌아갈 때 리스트 청소
        currentTargets.Clear();
    }

    protected override void OnProjectileUpdate()
    {
        // 1. 다단 히트 (Tick) 로직 (수명 깎는 건 부모가 해줌)
        if (currentTargets.Count > 0)
        {
            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                tickTimer = 0f;
                foreach (Collider target in currentTargets)
                {
                    if (target == null || !target.gameObject.activeInHierarchy) continue;
                    DealDamage(target.gameObject, target.transform.position);
                }
            }
        }
    }

    protected override void ProcessHit(Collider other)
    {
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
}