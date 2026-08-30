using System;
using System.Collections.Generic;
using UnityEngine;

public enum HitDetectionShape
{
    Sphere,
    Capsule,
    Fan,
}

[ModuleCategory("Combat/Hit Detection")]
[Serializable]
public class Module_HitDetection : SkillModule
{
    [Header("Detection")]
    public HitDetectionShape shape = HitDetectionShape.Sphere;

    [Tooltip("판정 중심 위치")]
    public Vector3 localCenter = Vector3.forward;

    [Header("Detection Settings")]
    [Tooltip("판정을 유지할 시간. 0 이하이면 페이즈 종료까지 유지")]
    public float detectionDuration = 0f;

    [Tooltip("판정 검사 간격")]
    public float CheckInterval = .05f;

    private float checkTimer; 

    [Tooltip("판정 활성화 중 같은 대상은 한 번만 피격")]
    public bool preventDuplicateHit = true;

    [Header("Target")]
    public LayerMask targetLayer;

    [Tooltip("OnNotify 호출 시 즉시 한 번 판정")]
    public bool detectImmediately = true;

    [Header("Sphere")]
    [Tooltip("Sphere 판정 반경")]
    public float radius = 1f;

    [Header("Header")]
    [Tooltip("Capsule 판정의 두 번째 위치")]
    public Vector3 localEnd = Vector3.forward * 2f;

    [Tooltip("Capsule 판정 반경")]
    public float capsuleRadius = 0.5f;

    [Header("Fan")]
    [Tooltip("Fan 반경")]
    public float fanRadius = 3f;

    [Range(0f, 360f)]
    [Tooltip("부채꼴 각도")]
    public float fanAngle = 90f; 

    //=========================================================================
    // Hit 시작 
    //=========================================================================

    public override void OnNotify(
        Character owner,
        ActiveSkill skill,
        PhaseSkill phaseSkill)
    {
        if (owner == null || skill == null)
            return;


        skill.Runtime.Hit.Begin();

        if (detectImmediately)
        {
            Detect(owner, skill);
        }

        checkTimer = CheckInterval; 
    }

    public override void FixedUpdate(Character owner, ActiveSkill skill, PhaseSkill phaseSkill, float fixedDeltaTime)
    {
        if (owner == null || skill == null) return;

        if (!skill.Runtime.Hit.IsDectecting)
            return;

        checkTimer -= fixedDeltaTime;
        if (checkTimer > 0.0f)
            return;

        checkTimer = CheckInterval;

        Detect(owner, skill);
    }

    private void Detect(Character owner, ActiveSkill skill)
    {
        Vector3 center = owner.transform.TransformPoint(localCenter);

        Collider[] hits = { };

        switch (shape)
        {
            case HitDetectionShape.Sphere:
                hits = Physics.OverlapSphere(center, radius,
                    targetLayer, QueryTriggerInteraction.Ignore);
                break;
            case HitDetectionShape.Capsule:
                Vector3 end = owner.transform.TransformPoint(localEnd);


                hits = Physics.OverlapCapsule(
                    center,
                    end,
                    capsuleRadius,
                    targetLayer,
                    QueryTriggerInteraction.Ignore);
                break;

            case HitDetectionShape.Fan:

                // ------------------------------------------------
                // 1. 먼저 반경 안의 Collider를 전부 탐색
                // ------------------------------------------------

                Collider[] candidates =
                    Physics.OverlapSphere(
                        center,
                        fanRadius,
                        targetLayer,
                        QueryTriggerInteraction.Ignore);

                List<Collider> fanHits =
                    new List<Collider>();


                // ------------------------------------------------
                // 2. 부채꼴 각도 판정
                // ------------------------------------------------

                Vector3 forward =
                    owner.transform.forward;

                // 수평 부채꼴
                forward.y = 0f;
                forward.Normalize();

                float halfAngle =
                    fanAngle * 0.5f;

                float cosHalfAngle =
                    Mathf.Cos(halfAngle * Mathf.Deg2Rad);


                foreach (Collider candidate in candidates)
                {
                    if (candidate == null)
                        continue;

                    // Collider의 중심을 기준으로 방향 계산
                    Vector3 targetPosition =
                        candidate.bounds.center;

                    Vector3 direction =
                        targetPosition - center;

                    direction.y = 0f;

                    // 중심에 겹쳐서 방향이 없는 경우
                    if (direction.sqrMagnitude <= 0.0001f)
                    {
                        fanHits.Add(candidate);
                        continue;
                    }

                    direction.Normalize();


                    // ------------------------------------------------
                    // Dot Product를 이용한 부채꼴 각도 판정
                    //
                    // cos(halfAngle)보다 크면
                    // forward 기준 ±halfAngle 안쪽
                    // ------------------------------------------------

                    float dot =
                        Vector3.Dot(forward, direction);

                    if (dot >= cosHalfAngle)
                    {
                        fanHits.Add(candidate);
                    }
                }

                hits = fanHits.ToArray();

                break;

                break;
            default:
                break;
        }

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            GameObject target = GetDamageTarget(hit);

            if (target == null) continue;

            // 자기 / 아군 처리 
            if (CombatHelper.IsFriendly(owner, target,
                skill.Runtime?.Hit.Ignores))
                continue;

            // 같은 대상 중복 처리 
            if (preventDuplicateHit)
            {
                if (!skill.Runtime.Hit.TryAddTarget(target))
                    continue;
            }

            // 데미지 
            Vector3 hitPoint = hit.ClosestPoint(center);

            CombatHelper.ApplyDamage(
                owner,
                skill.damageData,
                target,
                hitPoint);

        }
    }


    // =========================================================
    // Target
    // =========================================================

    private GameObject GetDamageTarget(Collider collider)
    {
        if (collider == null)
            return null;

        Character character =
            collider.GetComponentInParent<Character>();

        if (character != null)
            return character.gameObject;

        return collider.gameObject;
    }

    // =========================================================
    // End
    // =========================================================

    public void EndDetection(ActiveSkill skill)
    {
        if (skill == null)
            return;

        skill.Runtime.Hit.End();
    }


    public override SkillModule Clone()
    {
        return (Module_HitDetection)base.Clone();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
            return;

        if (shape == HitDetectionShape.Sphere)
        {
            DrawSphereGizmo();
        }
        else if (shape == HitDetectionShape.Capsule)
        {
            DrawCapsuleGizmo();
        }
        else if (shape == HitDetectionShape.Fan)
        {
            DrawFanGizmo();
        }
    }

    private void DrawSphereGizmo()
    {
        Gizmos.DrawWireSphere(
            localCenter,
            radius);
    }

    private void DrawCapsuleGizmo()
    {
        Vector3 start = localCenter;
        Vector3 end = localEnd;

        Gizmos.DrawWireSphere(
            start,
            capsuleRadius);

        Gizmos.DrawWireSphere(
            end,
            capsuleRadius);

        Vector3 direction =
            (end - start).normalized;

        Vector3 perpendicular =
            Vector3.Cross(
                direction,
                Vector3.up).normalized;

        Vector3 perpendicular2 =
            Vector3.Cross(
                direction,
                perpendicular).normalized;

        Vector3[] dirs =
        {
            perpendicular,
            -perpendicular,
            perpendicular2,
            -perpendicular2
        };

        foreach (Vector3 dir in dirs)
        {
            Gizmos.DrawLine(
                start + dir * capsuleRadius,
                end + dir * capsuleRadius);
        }
    }

    private void DrawFanGizmo()
    {
        Vector3 origin = localCenter;

        Vector3 forward =
            Vector3.forward;

        int segments = 24;

        Vector3 previous =
            origin +
            Quaternion.Euler(
                0f,
                -fanAngle * 0.5f,
                0f) *
            forward *
            fanRadius;

        for (int i = 1; i <= segments; i++)
        {
            float angle =
                -fanAngle * 0.5f +
                fanAngle * i / segments;

            Vector3 next =
                origin +
                Quaternion.Euler(
                    0f,
                    angle,
                    0f) *
                forward *
                fanRadius;

            Gizmos.DrawLine(
                previous,
                next);

            previous = next;
        }

        Vector3 left =
            Quaternion.Euler(
                0f,
                -fanAngle * 0.5f,
                0f) *
            forward *
            fanRadius;

        Vector3 right =
            Quaternion.Euler(
                0f,
                fanAngle * 0.5f,
                0f) *
            forward *
            fanRadius;

        Gizmos.DrawLine(
            origin,
            origin + left);

        Gizmos.DrawLine(
            origin,
            origin + right);
    }

#endif
}