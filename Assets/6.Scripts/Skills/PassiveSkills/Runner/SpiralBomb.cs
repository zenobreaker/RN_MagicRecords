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
    private readonly float attachedLifetime;
    private float remainingAttachedLife;

    public SpiralBomb(LayerMask enemyLayer
        , Character owner
        , float explosionRadius
        , DamageEvent explosionEvent
        , float attachedLifetime = 3f)
    {
        this.enemyLayer = enemyLayer;
        this.owner = owner;
        this.explosionRadius = explosionRadius;
        this.explosionEvent = explosionEvent;
        this.attachedLifetime = Mathf.Max(0.01f, attachedLifetime);
    }

    public void OnSpawn(BaseProjectile projectile)
    {
        hasTriggered = false;
        remainingAttachedLife = attachedLifetime;
        attachedTarget = null;
        localOffset = Vector3.zero;
        if (projectile != null) owner = projectile.Owner;
        // Keep the projectile's normal launch velocity until a valid enemy hit.
    }

    public void OnHit(BaseProjectile projectile, GameObject target)
    {
        if (hasTriggered || projectile == null || target == null || !target.activeInHierarchy) return;
        if (!enemyLayer.Contains(target) || !target.TryGetComponent<IDamagable>(out _) ||
            CombatHelper.IsFriendly(owner, target, projectile.Ignores)) return;
        hasTriggered = true;
        remainingAttachedLife = attachedLifetime;


        attachedTarget = target.transform;
        localOffset = attachedTarget.SafeInvoke(
            v => v.InverseTransformPoint(projectile.transform.position));

        if (projectile is PiercingDrillProjectile drill)
            drill.AttachToTarget(target);
        else if (projectile.TryGetComponent<Rigidbody>(out var rigid))
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
        if (!hasTriggered || projectile == null) return;
        if (attachedTarget == null || !attachedTarget.gameObject.activeInHierarchy)
        {
            projectile.gameObject.SetActive(false);
            return;
        }

        if (attachedTarget != null && attachedTarget.gameObject.activeInHierarchy)
            projectile.transform.position = attachedTarget.TransformPoint(localOffset);

        remainingAttachedLife -= dt;
        if (remainingAttachedLife <= 0f)
            projectile.gameObject.SetActive(false);
    }
}
