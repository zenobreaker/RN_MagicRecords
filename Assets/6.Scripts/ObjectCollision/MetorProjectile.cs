using System.Collections.Generic;
using UnityEngine;

public class MetorProjectile
    : BaseProjectile
    , ITargetableEffect
{
    [Header("Meteor Settings")]
    [SerializeField] private float fallSpeed = 20f;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private string explosionEffectName = "";
    [SerializeField] private string impactSoundName = "";

    [Header("Visuals (선택")]
    [SerializeField] private TrailRenderer trail;

    private Vector3 targetPosition;
    private DamageData damageData;
    private float cachedMultiplier = 1f;
    private bool isExploded = false;

    // 투사체가 스폰될 때 목표 지점을 설정해주는 함수
    public void SetTargetPosition(Vector3 targetPosition)
    {
        this.targetPosition = targetPosition;
        transform.LookAt(targetPosition);
    }

    private void OnEnable()
    {
        isExploded = false; 
        if(trail != null) trail.Clear(); 
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ObjectPooler.ReturnToPool(this.gameObject);
    }

    private void Update()
    {
        if (isExploded) return; 

        // 1. 목표 지점을 향해 사선으로 이동 
        transform.position = Vector3.MoveTowards(transform.position, 
            targetPosition, 
            fallSpeed *Time.deltaTime);

        // 2. 목표 지점에 도달했는지 체크 
        if (Vector3.Distance(transform.position, targetPosition) <= 0.1f)
            Explode();
    }

    private void Explode()
    {
        isExploded = true;

        // 1. 폭발 이펙트 스폰 
        GameObject explosion = ObjectPooler.DeferredSpawnFromPool(explosionEffectName, transform.position,
            Quaternion.identity);

        // 2. 광역 데미지 판정 
        if(explosion != null && explosion.TryGetComponent<ISkillEffect>(out var skillEffect))
        {
            skillEffect.SetDamageInfo(owner, damageData, false, cachedMultiplier);
            skillEffect.AddIgnore(ownerObject); 
        }

        // Play Sound
        {
            SoundManager.Instance.SafeInvoke(v => v.PlaySFX(impactSoundName));
        }

        ObjectPooler.FinishSpawn(explosion);

        // 3. 운석 역할 끝
        gameObject.SetActive(false); 
    }
}
   
