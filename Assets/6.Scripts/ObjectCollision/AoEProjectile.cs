using System.Collections.Generic;
using UnityEngine;

public class AoEProjectile : BaseProjectile
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float expandDuration = 0.5f;
    [SerializeField]
    private AnimationCurve expandCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField] private float lifeTime = 1f;
    [SerializeField] private LayerMask hitLayer;

    [Header("Cone Shape Settings")]
    [SerializeField] private bool useConeShape = false;

    [Range(0f, 360f)]
    [SerializeField] private float coneAngle = 90f;

    [Header("Hit Settings")]
    [SerializeField] private bool isMultiHit = false;

    [Tooltip("지속 장판의 데미지 간격")]
    [SerializeField] private float tickRate = 0.5f;

    [Tooltip("확장 중 적을 검사하는 간격")]
    [SerializeField] private float hitCheckRate = 0.05f;

    [Header("Visuals")]
    [SerializeField] private ParticleSystem aoeParticle;

    [Tooltip("실제 얼음 이펙트의 자식 Transform. 전체 이펙트가 커지도록 스케일링됩니다.")]
    [SerializeField] private Transform visualRoot;

    private float lifeTimer;
    private float expandTimer;
    private float tickTimer;
    private float hitCheckTimer;

    private float currentRadius;

    // 같은 적의 여러 Collider에 중복 피격되는 것 방지
    private HashSet<GameObject> hitTargets = new();

    protected override void OnEnable()
    {
        base.OnEnable();

        lifeTimer = lifeTime;
        expandTimer = 0f;
        tickTimer = 0f;
        hitCheckTimer = 0f;

        currentRadius = 0f;

        hitTargets.Clear();

        UpdateAoESize();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        hitTargets.Clear();
        ObjectPooler.ReturnToPool(gameObject);
    }

    protected override void Update()
    {
        base.Update();

        // ---------------------------------------------------------
        // 1. 전체 수명
        // ---------------------------------------------------------

        lifeTimer -= Time.deltaTime;

        if (lifeTimer <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        // ---------------------------------------------------------
        // 2. 범위 확장
        // ---------------------------------------------------------

        if (expandTimer < expandDuration)
        {
            expandTimer += Time.deltaTime;

            float normalizedTime =
                expandDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(expandTimer / expandDuration);

            float curveValue = expandCurve.Evaluate(normalizedTime);

            currentRadius = explosionRadius * curveValue;

            UpdateAoESize();
        }
        else
        {
            currentRadius = explosionRadius;
        }

        // ---------------------------------------------------------
        // 3. 공격 판정
        // ---------------------------------------------------------

        hitCheckTimer -= Time.deltaTime;

        if (hitCheckTimer <= 0f)
        {
            hitCheckTimer = hitCheckRate;

            ExplodeHit();
        }

        // ---------------------------------------------------------
        // 4. 지속 피해
        // ---------------------------------------------------------

        if (isMultiHit)
        {
            tickTimer -= Time.deltaTime;

            if (tickTimer <= 0f)
            {
                tickTimer = tickRate;

                // 지속 피해용이므로 이전 피격 기록 초기화
                hitTargets.Clear();

                //ExplodeHit();
            }
        }
    }

    /// <summary>
    /// 현재 반경에 맞춰 VFX와 실제 판정 크기를 동기화
    /// </summary>
    private void UpdateAoESize()
    {
        // ---------------------------------------------------------
        // 1. VFX 전체 크기
        // ---------------------------------------------------------

        if (visualRoot != null)
        {
            float normalizedRadius =
                explosionRadius <= 0f
                    ? 1f
                    : currentRadius / explosionRadius;

            visualRoot.localScale =
                Vector3.one * normalizedRadius;
        }

        // ---------------------------------------------------------
        // 2. Particle Shape
        // ---------------------------------------------------------

        //if (aoeParticle == null)
        //    return;

        //var shape = aoeParticle.shape;

        //if (useConeShape)
        //{
        //    shape.shapeType = ParticleSystemShapeType.Cone;
        //    shape.angle = coneAngle / 2f;
        //    shape.radius = currentRadius;
        //}
        //else
        //{
        //    shape.shapeType = ParticleSystemShapeType.Sphere;
        //    shape.radius = currentRadius;
        //}
    }

    /// <summary>
    /// 현재 확장된 반경을 기준으로 범위 판정
    /// </summary>
    private void ExplodeHit()
    {
        if (currentRadius <= 0f)
            return;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            currentRadius,
            hitLayer
        );

        foreach (Collider hitCollider in hits)
        {
            if (hitCollider == null)
                continue;

            if (IsFriendlyFire(hitCollider.gameObject))
                continue;

            if (ownerObject != null &&
                hitCollider.transform.IsChildOf(ownerObject.transform))
                continue;

            // 실제 공격 대상 루트 찾기
            GameObject targetObject = GetDamageTarget(hitCollider);

            if (targetObject == null)
                continue;

            // 단일 피격이면 한 대상당 1회
            if (!isMultiHit && hitTargets.Contains(targetObject))
                continue;

            // -----------------------------------------------------
            // 부채꼴 판정
            // -----------------------------------------------------

            if (useConeShape)
            {
                Vector3 dirToTarget =
                    targetObject.transform.position -
                    transform.position;

                dirToTarget.y = 0f;

                if (dirToTarget.sqrMagnitude <= 0.0001f)
                    continue;

                Vector3 forward = transform.forward;
                forward.y = 0f;
                forward.Normalize();

                float angleToTarget =
                    Vector3.Angle(
                        forward,
                        dirToTarget.normalized
                    );

                if (angleToTarget > coneAngle * 0.5f)
                    continue;
            }

            // -----------------------------------------------------
            // 실제 데미지
            // -----------------------------------------------------

            Vector3 hitPoint =
                hitCollider.ClosestPoint(transform.position);

            DealDamage(
                targetObject,
                hitPoint
            );

            if (!isMultiHit)
                hitTargets.Add(targetObject);
        }
    }

    /// <summary>
    /// 여러 Collider를 가진 몬스터라도 하나의 대상으로 취급
    /// </summary>
    private GameObject GetDamageTarget(Collider collider)
    {
        if (collider == null)
            return null;

        if (collider.TryGetComponent<Character>(out Character character))
            return character.gameObject;

        Character parentCharacter =
            collider.GetComponentInParent<Character>();

        if (parentCharacter != null)
            return parentCharacter.gameObject;

        // Character가 없는 일반 대상이면 Collider 자체를 사용
        return collider.gameObject;
    }

    public void SetAoESize(
        float newRadius,
        float newConeAngle = 0f)
    {
        explosionRadius = newRadius;

        if (newConeAngle > 0f)
            coneAngle = newConeAngle;

        currentRadius = 0f;
        expandTimer = 0f;

        UpdateAoESize();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 origin = transform.position;

        float radius =
            Application.isPlaying
                ? currentRadius
                : explosionRadius;

        if (!useConeShape)
        {
            Gizmos.DrawWireSphere(origin, radius);
            return;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 rightLimit =
            Quaternion.Euler(0f, coneAngle * 0.5f, 0f) *
            forward *
            radius;

        Vector3 leftLimit =
            Quaternion.Euler(0f, -coneAngle * 0.5f, 0f) *
            forward *
            radius;

        Gizmos.DrawLine(origin, origin + rightLimit);
        Gizmos.DrawLine(origin, origin + leftLimit);

        int segments = 20;
        float step = coneAngle / segments;

        Vector3 currentPoint =
            origin + leftLimit;

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle =
                -coneAngle * 0.5f +
                step * i;

            Vector3 nextPoint =
                origin +
                Quaternion.Euler(0f, currentAngle, 0f) *
                forward *
                radius;

            Gizmos.DrawLine(
                currentPoint,
                nextPoint
            );

            currentPoint = nextPoint;
        }
    }
}