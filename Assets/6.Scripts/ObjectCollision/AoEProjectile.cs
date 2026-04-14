using System.Collections.Generic;
using UnityEngine;

public class AoEProjectile 
    : MonoBehaviour
    , ISkillEffect
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float lifeTime = 1f; // 폭발/장판이 유지되는 시간
    [SerializeField] private LayerMask hitLayer;

    [Header("Hit Settings")]
    [SerializeField] private bool isMultiHit = false; // 단타 폭발 vs 지속 장판
    [SerializeField] private float tickRate = 0.5f;   // 다단히트 간격

    private float lifeTimer;
    private float tickTimer;

    private GameObject ownerObject;
    private DamageEvent damageEvent;

    // 단타일 때 중복 피격 방지용
    private HashSet<Collider> hitTargets = new HashSet<Collider>();
    private HashSet<GameObject> ignores = new HashSet<GameObject>();

    public void SetDamageInfo(GameObject attacker, DamageData damageData, bool bExtraCrit = false)
    {
        ownerObject = attacker;
        damageEvent = damageData.GetMyDamageEvent(attacker, false, bExtraCrit);
    }

    public void AddIgnore(GameObject ignore)
    {
        ignores.Add(ignore);
    }

    private void OnEnable()
    {
        hitTargets.Clear();
        lifeTimer = lifeTime;
        tickTimer = 0f; // 켜지자마자 바로 1회 타격 들어가도록 0으로 셋팅

        // 단발 폭발이면 켜지자마자 쾅!
        if (!isMultiHit)
        {
            ExplodeHit();
        }
    }

    private void OnDisable()
    {
        ignores.Clear();
        ObjectPooler.ReturnToPool(this.gameObject);
    }

    private void Update()
    {
        // 1. 수명 관리 (시간 다 되면 꺼짐)
        if (lifeTimer > 0f)
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                this.gameObject.SetActive(false);
                return;
            }
        }

        // 2. 다단히트(장판) 틱 관리
        if (isMultiHit)
        {
            tickTimer -= Time.deltaTime;
            if (tickTimer <= 0f)
            {
                ExplodeHit();
                tickTimer = tickRate;
            }
        }
    }

    private void ExplodeHit()
    {
        // SphereCast 대신 둥근 범위를 덮치는 OverlapSphere 사용!
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, hitLayer);

        foreach (Collider hitCollider in hits)
        {
            if (ignores.Contains(hitCollider.gameObject)) continue;
            if (ownerObject != null && hitCollider.gameObject == ownerObject) continue;

            if (!isMultiHit && hitTargets.Contains(hitCollider)) continue;

            IDamagable damageTarget = hitCollider.GetComponent<IDamagable>();
            if (damageTarget != null)
            {
                damageTarget.OnDamage(ownerObject, null, hitCollider.transform.position, damageEvent);

                if (!isMultiHit) hitTargets.Add(hitCollider);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
