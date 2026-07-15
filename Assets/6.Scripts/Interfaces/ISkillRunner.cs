

using UnityEngine;

public interface ISkillEffectBehavior
{
    void OnSpawn(ISkillEffect projectile);    
    void OnHit(ISkillEffect effect, GameObject target, Vector3 hitPos);
    void OnUpdate(ISkillEffect effect, float dt);
    void OnDespawn(ISkillEffect projectile);
}


public interface IOnSpawnRunner
{
    void OnSpawn(ISkillEffect effect);
}

public interface IOnHitRunner
{
    void OnHit(ISkillEffect effect, GameObject target);
}

public interface IOnUpdateRunner
{
    void OnUpdate(ISkillEffect effect, float dt);
}

public interface IOnDestroyRunner
{
    void OnDestroy(ISkillEffect effect);
}