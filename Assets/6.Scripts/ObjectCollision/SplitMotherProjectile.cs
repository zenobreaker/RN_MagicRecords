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

    private float fireTimer = 0f;
    private bool isCrit;

    // 💡 [최적화] Update문 안에서 'new'로 데미지 데이터를 계속 생성하면 렉 유발!
    // 최초 주입 시 자탄용 데미지 데이터를 딱 한 번만 만들어 두고 캐싱해서 씁니다.
    private DamageData cachedChildDamageData;
    private float cachedMultiplier = 1.0f;

    // =======================================================
    // 💡 ISkillEffect 인터페이스 구현부
    // =======================================================
    public override void SetDamageInfo(Character attacker, DamageData damageData
        , bool bExtraCrit = false, float mulitplier = 1.0f)
    {
        base.SetDamageInfo(attacker, damageData, bExtraCrit, mulitplier);
        if (attacker == null || damageData == null) return;
        isCrit = bExtraCrit;

        // 💡 여기서 자탄용 데미지 데이터를 미리 1번만 계산해서 생성해 둡니다. (GC 할당 제로화)
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

    protected override void OnProjectileSpawned()
    {
        fireTimer = 0f;
    }

    protected override void OnProjectileUpdate()
    {
        // 💡 잡다한 수명 계산은 부모가 알아서 하니, 나는 살포 타이머만 신경 쓴다!
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireInterval)
        {
            fireTimer = 0f;
            SpawnChildSpread(); // (자탄 360도 스폰 로직 동일)
        }
    }

    // =======================================================
    // ⚔️ 자탄 360도 발사 매커니즘
    // =======================================================
    private void SpawnChildSpread()
    {
        if (childCount <= 0 || string.IsNullOrEmpty(childObjectName)) return;

        float angleStep = 360f / childCount;

        for (int i = 0; i < childCount; i++)
        {
            float targetAngle = i * angleStep;
            Quaternion childRotation = Quaternion.Euler(0f, targetAngle, 0f);

            // 모탄이 이동 중인 현재 위치에서 사방으로 자탄 생성
            GameObject childObj = ObjectPooler.SpawnFromPool(childObjectName, transform.position, childRotation);

            if (childObj != null && childObj.TryGetComponent<ISkillEffect>(out var childEffect))
            {
                // 캐싱된 데미지 데이터를 전달하므로 매 프레임 수십 개씩 쏴도 힙 메모리가 깨끗합니다.
                childEffect.SetDamageInfo(owner, cachedChildDamageData, isCrit, cachedMultiplier);
                childEffect.AddIgnore(ownerObject);
            }
        }
    }

    protected override void ProcessHit(Collider other)
    {
        // 벽에 부딪히면 파괴 (적은 관통)
        this.gameObject.SetActive(false);
    }
}