using UnityEngine;

public sealed class SpiralBomb
    : IPorjectileOnHitRunner
    , IProjectileOnSpawnRunner
    , IProjectileOnUpdateRunner
    , IProjectileOnDestroyRunner
{
    private LayerMask enemyLayer;
    private Character owner;
    private bool hasTriggered = false;


    private float explosionRadius;
    private DamageEvent explosionEvent;

    private Transform attachedTarget;
    private Vector3 localOffset;

    public SpiralBomb(LayerMask enemyLayer
        , Character owner
        , float explosionRadius
        , DamageEvent explosionEvent)
    {
        this.enemyLayer = enemyLayer;
        this.owner = owner;
        this.explosionRadius = explosionRadius;
        this.explosionEvent = explosionEvent;
    }

    public void OnSpawn(BaseProjectile projectile)
    {
        if (projectile != null)
        {
            owner = projectile.Owner;
            hasTriggered = false;
        }

        if (projectile is PiercingDrillProjectile pdp)
            pdp.SetDrilingSpeedRatio(1.0f);
    }

    public void OnHit(BaseProjectile projectile, GameObject target)
    {
        if (hasTriggered || projectile == null || target == null) return;
        hasTriggered = true;


        attachedTarget = target.transform;
        localOffset = attachedTarget.SafeInvoke(
            v => v.InverseTransformPoint(projectile.transform.position));

        if (projectile.TryGetComponent<Rigidbody>(out var rigid))
            rigid.linearVelocity = Vector3.zero;
    }

    public void OnDestroy(BaseProjectile projectile)
    {
        if (projectile == null) return;

        if (hasTriggered)
        {
            //TODO: 이펙트 추가하기
            ApplyAoEDamage(projectile.gameObject.transform.position, explosionRadius);
        }
    }

    private void ApplyAoEDamage(Vector3 center, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius, enemyLayer);
        foreach (var hit in hits)
        {
            if (hit == null || !hit.gameObject.activeInHierarchy) continue;

            if (hit.TryGetComponent<IDamagable>(out var IDamagable))
            {
                IDamagable.OnDamage(owner, null, center, damageEvent:explosionEvent); 
            }
        }
    }

    public void OnUpdate(BaseProjectile projectile, float dt)
    {
        if (!hasTriggered) return;

        if (attachedTarget != null && attachedTarget.gameObject.activeInHierarchy)
            projectile.transform.position = attachedTarget.TransformPoint(localOffset);
    }
}
