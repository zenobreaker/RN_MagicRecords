using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SplitMotherProjectile : MonoBehaviour, ISkillEffect
{
    [Header("Mother Projectile Settings")]
    [SerializeField] private float force = 1200.0f;
    [SerializeField] private float life = 4.0f;
    [SerializeField] private LayerMask ignoreLayer;

    [Header("Split (Child) Settings")]
    [Tooltip("분열되어 퍼질 자탄의 프리팹 이름")]
    [SerializeField] private string childObjectName = "Projectile_Normal";
    [Tooltip("한 번 뿌릴 때 발사할 자탄의 개수")]
    [SerializeField] private int childCount = 6;
    [Tooltip("자탄을 몇 초 주기로 발사할 것인가?")]
    [SerializeField] private float fireInterval = 0.3f;
    [Tooltip("자탄이 가질 데미지 배율 (0.3 = 모탄 데미지의 30%)")]
    [SerializeField] private float childDamageMultiplier = 0.3f;

    private float curLife = 0f;
    private float fireTimer = 0f;

    private Rigidbody rigid;
    private new Collider collider;

    private GameObject ownerObject;
    private bool isCrit;

    // 💡 [최적화] Update문 안에서 'new'로 데미지 데이터를 계속 생성하면 렉 유발!
    // 최초 주입 시 자탄용 데미지 데이터를 딱 한 번만 만들어 두고 캐싱해서 씁니다.
    private DamageData cachedChildDamageData;
    private float cachedMultiplier = 1.0f;

    private HashSet<GameObject> ignores = new HashSet<GameObject>();

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        collider = GetComponent<Collider>();
    }

    // =======================================================
    // 💡 ISkillEffect 인터페이스 구현부
    // =======================================================
    public void SetDamageInfo(GameObject attacker, DamageData damageData
        , bool bExtraCrit = false, float mulitplier = 1.0f)
    {
        if (attacker == null || damageData == null) return;

        ownerObject = attacker;
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
            settings = damageData.settings,
            hitData = damageData.hitData
        };

        cachedMultiplier = mulitplier;
    }

    public void AddIgnore(GameObject ignore)
    {
        ignores.Add(ignore);
    }

    // =======================================================
    // 🔄 생명주기 및 주기적 발사 관리
    // =======================================================
    private void OnEnable()
    {
        if (rigid == null) return;

        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;
        rigid.AddForce(transform.forward * force);

        curLife = life;
        fireTimer = 0f; // 타이머 초기화
    }

    private void OnDisable()
    {
        ObjectPooler.ReturnToPool(this.gameObject);
        ignores.Clear();
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

        // 💡 2. [핵심] 날아가면서 주기적으로 자탄 살포!
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireInterval)
        {
            fireTimer = 0f;
            SpawnChildSpread();
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
                childEffect.SetDamageInfo(ownerObject, cachedChildDamageData, isCrit, cachedMultiplier);
                childEffect.AddIgnore(ownerObject);
            }
        }
    }

    // 벽이나 적에 부딪혔을 때의 처리 (관통하게 할 거라면 이 함수를 통째로 지우셔도 됩니다)
    private void OnTriggerEnter(Collider other)
    {
        if (ignores.Contains(other.gameObject)) return;
        if (ignoreLayer.Contains(other.gameObject)) return;

        // 예를 들어 벽(Environment) 레이어에 부딪히면 모탄 소멸
        // (적을 만났을 땐 뚫고 지나가면서 계속 뿌려야 제맛이므로 레이어 검사를 켜두는 게 좋습니다)
        this.gameObject.SetActive(false);
    }
}