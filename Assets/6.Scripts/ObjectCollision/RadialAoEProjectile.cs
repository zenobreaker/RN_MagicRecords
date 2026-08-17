using System.Collections.Generic;
using UnityEngine;

public class RadialAoEProjectile : BaseProjectile
{
    [Header("Radial Settings")]
    [SerializeField] private int shardCount = 8;

    [Tooltip("각 얼음 파편이 차지하는 각도")]
    [Range(0f, 90f)]
    [SerializeField] private float shardAngle = 20f;

    [Tooltip("판정이 시작되는 중심 거리")]
    [SerializeField] private float innerRadius = 0f;

    [Tooltip("판정 최대 거리")]
    [SerializeField] private float maxRadius = 7f;

    [Tooltip("공격 방향 전체 회전")]
    [SerializeField] private float rotationOffset = 0f;

    [Header("Expansion")]
    [SerializeField] private float expandDuration = 0.5f;

    [SerializeField]
    private AnimationCurve expandCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Hit")]
    [SerializeField] private LayerMask hitLayer;

    [SerializeField] private bool multiHit = false;
    [Tooltip("지속 장판의 데미지 간격")]
    [SerializeField] private float tickRate = 0.2f;
    [Tooltip("확장 중 적을 검사하는 간격")]
    [SerializeField] private float hitCheckRate = 0.05f;


    private float currentRadius;
    private float expandTimer;
    private float tickTimer;
    private float hitCheckTimer;

    private readonly HashSet<GameObject> hitTargets = new();

    protected override void OnEnable()
    {
        base.OnEnable();

        currentRadius = 0f;
        expandTimer = 0f;
        tickTimer = 0f;
        hitCheckTimer = 0;

        hitTargets.Clear();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        ObjectPooler.ReturnToPool(this.gameObject);
    }

    protected override void Update()
    {
        base.Update();

        UpdateExpansion();

        hitCheckTimer -= Time.deltaTime;

        if(hitCheckTimer <= 0f)
        {
            hitCheckTimer = hitCheckRate;

            CheckRadialHit();
        }
 
        if (multiHit)
        {
            tickTimer -= Time.deltaTime;

            if (tickTimer <= 0f)
            {
                tickTimer = tickRate;

                hitTargets.Clear();
            }
        }
    }

    private void UpdateExpansion()
    {
        if (expandTimer >= expandDuration)
        {
            currentRadius = maxRadius;
            gameObject.SetActive(false);
            return;
        }

        expandTimer += Time.deltaTime;

        float normalized =
            expandDuration <= 0f
                ? 1f
                : Mathf.Clamp01(expandTimer / expandDuration);

        currentRadius =
            Mathf.Lerp(
                innerRadius,
                maxRadius,
                expandCurve.Evaluate(normalized)
            );
    }

    private void CheckRadialHit()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            currentRadius,
            hitLayer
        );

        foreach (Collider hit in hits)
        {
            if (IsFriendlyFire(hit.gameObject))
                continue;

            if (ownerObject != null &&
                hit.gameObject == ownerObject)
                continue;

            if (!multiHit && hitTargets.Contains(hit.gameObject))
                continue;

            if (!IsInsideRadialAttack(hit.transform.position))
                continue;

            DealDamage(
                hit.gameObject,
                hit.ClosestPoint(transform.position)
            );

            if (!multiHit)
                hitTargets.Add(hit.gameObject);
        }
    }

    private bool IsInsideRadialAttack(Vector3 targetPosition)
    {
        Vector3 offset = targetPosition - transform.position;

        // Y축 제거
        offset.y = 0f;

        float distance = offset.magnitude;

        // 중심부 / 최대거리 검사
        if (distance < innerRadius)
            return false;

        if (distance > currentRadius)
            return false;

        if (distance <= 0.001f)
            return true;

        float targetAngle =
            Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;

        targetAngle = NormalizeAngle(targetAngle);

        float sectorSize = 360f / shardCount;

        for (int i = 0; i < shardCount; i++)
        {
            float centerAngle =
                rotationOffset + sectorSize * i;

            centerAngle = NormalizeAngle(centerAngle);

            float delta =
                Mathf.Abs(
                    Mathf.DeltaAngle(
                        targetAngle,
                        centerAngle
                    )
                );

            if (delta <= shardAngle * 0.5f)
                return true;
        }

        return false;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle < 0f)
            angle += 360f;

        return angle;
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        DrawRadialGizmo();
    }

    private void DrawRadialGizmo()
    {
        Vector3 origin = transform.position;

        float sectorSize = 360f / shardCount;

        for (int i = 0; i < shardCount; i++)
        {
            float centerAngle =
                rotationOffset + sectorSize * i;

            float halfAngle = shardAngle * 0.5f;

            Vector3 left =
                Quaternion.Euler(0f, centerAngle - halfAngle, 0f)
                * Vector3.forward;

            Vector3 right =
                Quaternion.Euler(0f, centerAngle + halfAngle, 0f)
                * Vector3.forward;

            Gizmos.DrawLine(
                origin + left * innerRadius,
                origin + left * maxRadius
            );

            Gizmos.DrawLine(
                origin + right * innerRadius,
                origin + right * maxRadius
            );

            Gizmos.DrawLine(
                origin + left * maxRadius,
                origin + right * maxRadius
            );
        }
    }

#endif
}