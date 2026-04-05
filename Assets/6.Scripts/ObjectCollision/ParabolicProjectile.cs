using UnityEngine;

public class ParabolicProjectile
        : MonoBehaviour
    , ISkillEffect
    , ITargetableEffect
{
    [Header("Parabola Settings")]
    [Tooltip("목표 지점까지 날아가는 데 걸리는 시간")]
    [SerializeField] private float flightTime = 1.0f;

    [Tooltip("궤적의 최고점 높이")]
    [SerializeField] private float arcHeight = 5.0f;

    [Tooltip("바닥에 닿았을 때 터질 이펙트(이전에 만든 AoEExplosion 프리팹)")]
    [SerializeField] private string explosionEffectName = "VFX_Explosion";

    private Vector3 startPos;
    private Vector3 targetPos;
    private float progress = 0f;
    private bool isFired = false;

    // 데미지 정보를 다음 폭발(AoE)로 토스하기 위한 캐싱 변수들
    private GameObject ownerObject;
    private DamageData cachedDamageData;
    private bool cachedCrit;

    public void AddIgnore(GameObject ignore)
    {
        
    }

    public void SetDamageInfo(GameObject attacker, DamageData damageData, bool bExtraCrit = false)
    {
        ownerObject = attacker;
        cachedDamageData = damageData;
        cachedCrit = bExtraCrit;
    }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        startPos = transform.position;
        targetPos = targetPosition;
        progress = 0f;
        isFired = true; // 세팅 완료! 발사!
    }

    private void OnEnable()
    {
        isFired = false;
        progress = 0f;
    }

    private void OnDisable()
    {
        ObjectPooler.ReturnToPool(gameObject); 
    }

    private void Update()
    {
        if (!isFired) return;

        // 1. 진행도 (t) 계산 : 0에서 시작해서 도착하면 1이됨.
        progress += Time.deltaTime / flightTime; 

        // 2. 바닥 기준 직선 이동 ( 시작점 -> 끝 점)
        Vector3 currentPos = Vector3.Lerp(startPos, targetPos, progress);

        // 3. 포물선 정리 : sin 함수를 이용한 높이 추가
        float heightOffset = Mathf.Sin(progress * Mathf.PI) * arcHeight; 
        currentPos.y += heightOffset;

        // 4. 투사체 머리 부분이 날아가는 궤적 방향을 바라보도록 
        Vector3 moveDirection = currentPos - transform.position;
        if(moveDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveDirection);

        // 5. 실제 위치 이동
        transform.position = currentPos;

        // 6. 목표 도달 체크 
        if (progress >= 1f)
            Exlpode(); 
    }

    private void Exlpode()
    {
        isFired = false;

        GameObject explosion = ObjectPooler.DeferredSpawnFromPool(explosionEffectName, transform);
        if (explosion != null && explosion.TryGetComponent<ISkillEffect>(out var effect))
        {
            effect.SetDamageInfo(ownerObject, cachedDamageData, cachedCrit);
            effect.AddIgnore(ownerObject); 
        }

        ObjectPooler.FinishSpawn(explosion);
        gameObject.SetActive(false); 
    }
}
