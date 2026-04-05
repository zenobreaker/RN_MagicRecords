using System.Collections.Generic;
using UnityEngine;

public class BeamProjectile 
    : MonoBehaviour
    , ISkillEffect
{
    [Header("Beam Settings")]
    [SerializeField] private float beamRadius = 1.5f; // 레이저 두께 (반지름)
    [SerializeField] private float beamLength = 30f;  // 레이저 사거리
    [SerializeField] private LayerMask hitLayer;      // 타격할 레이어 (Enemy 전용으로 설정하여 벽 관통!)

    // 💡 추가된 부분: 파티클 연출과 데미지 타이밍을 맞추기 위한 딜레이!
    [Header("Timing Settings")]
    [SerializeField] private float hitDelay = 0.2f; //초 뒤에 실제 데미지 판정 시작
    [SerializeField] private float lifeTime = 0.2f; // 이펙트가 켜져 있는 총 시간 

    [Header("Hit Settings")]
    [SerializeField] private bool isMultiHit = false; // 체크 끄면 단발, 켜면 다단히트
    [SerializeField] private float tickRate = 0.2f;   // 다단히트 시 데미지 들어가는 간격(초)

    private float currentDelayTimer = 0f;
    private float currentLifeTimer = 0f;
    private float tickTimer = 0f;
    private bool isBeamActive = false;

    private GameObject ownerObject;
    private DamageEvent damageEvent;

    // 💡 단발 모드일 때 한 번 맞은 적이 또 맞는 걸 방지하는 리스트
    private HashSet<Collider> hitTargets = new HashSet<Collider>();
    private HashSet<GameObject> ignores = new();

    // 무기(Gun)에서 데미지 정보를 넘겨줄 때 호출하는 함수
    public void SetDamageInfo(GameObject attacker, DamageData damageData, bool bExtraCrit = false)
    {
        if (attacker == null || damageData == null) return;

        ownerObject = attacker;
        damageEvent = damageData.GetMyDamageEvent(attacker, false, bExtraCrit);
    }

    public void AddIgnore(GameObject ignore)
    {
        ignores.Add(ignore); 
    }

    private void OnEnable()
    {
        // 빔이 켜질 때마다 초기화
        hitTargets.Clear();
        tickTimer = 0f;

        // 타이머 초기화 
        currentDelayTimer = hitDelay;
        currentLifeTimer = lifeTime;

        // 단발 빔이라면 켜지자마자 즉시 1회 판정 쫙!
        if (hitDelay <= 0f)
        {
            ActivateBeam();
        }
    }

    void OnDisable()
    {
        ObjectPooler.ReturnToPool(gameObject);    // 한 객체에 한번만 
        CancelInvoke();    // Monobehaviour에 Invoke가 있다면 
    }

    private void Update()
    {
        // 1. 수명 관리
        if(currentLifeTimer > 0f)
        {
            currentLifeTimer -= Time.deltaTime; 
            if(currentLifeTimer <= 0f)
            {
                this.gameObject.SetActive(false);
                return; 
            }
        }

        // 2. 딜레이 관리 
        if(!isBeamActive)
        {
            currentDelayTimer -= Time.deltaTime;
            if (currentDelayTimer <= 0f)
            {
                ActivateBeam();
            }
        }
        // 3. 빔이 활성화된 상태 
        else if (isMultiHit )
        {
            tickTimer -= Time.deltaTime;
            if (tickTimer <= 0f)
            {
                FireBeamCast();
                tickTimer = tickRate; // 타이머 초기화 (예: 0.2초마다 타격)
            }
        }
    }

    private void ActivateBeam()
    {
        isBeamActive = true; 

        if(!isMultiHit)
        {
            FireBeamCast();
        }
        else
        {
            tickTimer = 0f; 
        }
    }

    // 💡 핵심 충돌 판정 로직
    private void FireBeamCast()
    {
        // 두꺼운 빔(SphereCast)을 쏴서 경로에 있는 모든 대상(All)을 긁어옵니다.
        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,    // 시작 위치 (총구)
            beamRadius,            // 두께
            transform.forward,     // 발사 방향 (앞)
            beamLength,            // 사거리
            hitLayer               // 적 레이어만 타격 (이걸로 벽 관통 구현!)
        );

        foreach (RaycastHit hit in hits)
        {
            // 무시할 대상은 무시 
            if (ignores.Contains(hit.collider.gameObject))
                continue;

            // 단발 모드일 때 이미 때린 적이면 무시 (다단히트는 무시 안 함)
            if (!isMultiHit && hitTargets.Contains(hit.collider))
                continue;

            // Damage 처리
            IDamagable damageTarget = hit.collider.GetComponent<IDamagable>();
            if (damageTarget != null)
            {
                // 정확한 피격 위치 계산
                Vector3 localHitPoint = hit.collider.transform.InverseTransformPoint(hit.point);

                // 데미지 전달
                damageTarget.OnDamage(ownerObject, null, localHitPoint, damageEvent);

                // 단발용: 맞은 적 기록
                if (!isMultiHit)
                {
                    hitTargets.Add(hit.collider);
                }
            }
        }
    }

    // 🎁 보너스: 유니티 에디터에서 빔의 두께와 길이를 빨간색 선으로 보여줍니다! (디버깅용)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 endPosition = transform.position + transform.forward * beamLength;
        Gizmos.DrawLine(transform.position, endPosition);
        Gizmos.DrawWireSphere(transform.position, beamRadius);
        Gizmos.DrawWireSphere(endPosition, beamRadius);
    }


}