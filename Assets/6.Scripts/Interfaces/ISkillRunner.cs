

using UnityEngine;



public interface IProjectileOnSpawnRunner
{
    void OnSpawn(BaseProjectile projectile);
}

public interface IPorjectileOnHitRunner
{
    void OnHit(BaseProjectile projectile, GameObject target);
}

public interface IProjectileOnUpdateRunner
{
    void OnUpdate(BaseProjectile projectile, float dt);
}

public interface IProjectileOnDestroyRunner
{
    void OnDestroy(BaseProjectile projectile);
}