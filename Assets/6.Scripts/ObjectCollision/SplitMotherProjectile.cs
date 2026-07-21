using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

[RequireComponent(typeof(Rigidbody))]
public class SplitMotherProjectile 
    : AbstractProjectile
{
    [Header("Split (Child) Settings")]
    [Tooltip("분열되어 퍼질 자탄의 프리팹 이름")]
    [SerializeField] private string childObjectName = "Projectile_Normal";
    [Tooltip("한 번 뿌릴 때 발사할 자탄의 개수")]
    [SerializeField] private int childCount = 6;
    [Tooltip("자탄을 몇 초 주기로 발사할 것인가?")]
    [SerializeField] private float fireInterval = 0.3f;
    [Tooltip("자탄이 가질 데미지 배율 (0.3 = 모탄 데미지의 30%)")]
    [SerializeField] private float childDamageMultiplier = 0.3f;
    [Tooltip("자탄을 한 번 뿌린 뒤 전방위 패턴을 회전시킬 각도")]
    [SerializeField] private float spreadRotationPerShot = 15f;

    private float fireTimer = 0f;
    private float currentSpreadRotation = 0f;
    private float motherSpeedMultiplier = 1f;
    private bool childHomingEnabled;
    private float childHomingSearchRadius;
    private float childHomingTurnSpeed;
    private LayerMask childHomingEnemyLayer;
    private Transform childHomingTargetOrigin;
    private bool isCrit;

    // 최초 주입 시 자탄용 데미지 데이터를 딱 한 번만 만들어 두고 캐싱.
    private DamageData cachedChildDamageData;
    private float cachedMultiplier = 1.0f;

    // =======================================================
    // ISkillEffect 인터페이스 구현부
    // =======================================================
    public override void SetDamageInfo(Character attacker, DamageData damageData
        , bool bExtraCrit = false, float mulitplier = 1.0f)
    {
        base.SetDamageInfo(attacker, damageData, bExtraCrit, mulitplier);
        if (attacker == null || damageData == null) return;
        isCrit = bExtraCrit;

        cachedChildDamageData = new DamageData
        {
            damageType = damageData.damageType,
            baseDamage = damageData.baseDamage * childDamageMultiplier,
            statCoefficient = damageData.statCoefficient * childDamageMultiplier,
            bDownable = damageData.bDownable,
            bLauncher = damageData.bLauncher,
            SoundName = damageData.SoundName,
            impulseDirection = damageData.impulseDirection,
            csp = damageData.csp,
            hitData = damageData.hitData
        };

        cachedMultiplier = mulitplier;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        fireTimer = 0f;
        currentSpreadRotation = 0f;
        motherSpeedMultiplier = 1f;
        childHomingEnabled = false;
        childHomingTargetOrigin = null;
    }

    public void SetMotherSpeedMultiplier(float multiplier)
    {
        motherSpeedMultiplier = Mathf.Max(0.01f, multiplier);
        force *= motherSpeedMultiplier;                                                                 
    }

    public void EnableChildHoming(float searchRadius, float turnSpeed, LayerMask enemyLayer,
        Transform targetSearchOrigin)
    {
        childHomingEnabled = true;
        childHomingSearchRadius = searchRadius;
        childHomingTurnSpeed = turnSpeed;
        childHomingEnemyLayer = enemyLayer;
        childHomingTargetOrigin = targetSearchOrigin;
    }

    protected override void OnProjectileUpdate()
    {
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireInterval)
        {
            fireTimer = 0f;
            SpawnChildSpread(); // (자탄 360도 스폰 로직 동일)
        }
    }

    // =======================================================
    // 자탄 360도 발사 매커니즘
    // =======================================================
    private void SpawnChildSpread()
    {
        if (childCount <= 0 || string.IsNullOrEmpty(childObjectName)) return;

        float angleStep = 360f / childCount;

        for (int i = 0; i < childCount; i++)
        {
            float targetAngle = currentSpreadRotation + i * angleStep;
            Quaternion childRotation = Quaternion.Euler(0f, targetAngle, 0f);

            // 모탄이 이동 중인 현재 위치에서 사방으로 자탄 생성
            GameObject childObj = ObjectPooler.SpawnFromPool(childObjectName, transform.position, childRotation);

            if (childObj != null && childObj.TryGetComponent<ISkillEffect>(out var childEffect))
            {
                // 캐싱된 데미지 데이터를 전달하므로 매 프레임 수십 개씩 쏴도 힙 메모리가 깨끗합니다.
                childEffect.SetDamageInfo(owner, cachedChildDamageData, isCrit, cachedMultiplier);
                childEffect.AddIgnore(ownerObject);

                if (childHomingEnabled && childObj.TryGetComponent<BaseProjectile>(out var childProjectile))
                {
                    if (!childObj.TryGetComponent<RuntimeHomingBehaviour>(out var homingBehaviour))
                        homingBehaviour = childObj.AddComponent<RuntimeHomingBehaviour>();

                    homingBehaviour.Setup(
                        childHomingSearchRadius,
                        childHomingTurnSpeed,
                        childHomingEnemyLayer,
                        childProjectile.Ignores,
                        childHomingTargetOrigin,
                        prioritizeHighestGrade: true);
                }
            }
        }

        // 다음 분사에서는 이전 전방위 패턴과 겹치지 않는 각도에서 발사합니다.
        currentSpreadRotation = Mathf.Repeat(
            currentSpreadRotation + spreadRotationPerShot,
            360f);
    }

    protected override void ProcessHit(Collider other)
    {
        // 벽에 부딪히면 파괴 (적은 관통)
        this.gameObject.SetActive(false);
    }
}
