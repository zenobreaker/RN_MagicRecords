using System;
using UnityEngine;

/// <summary>
/// 소유자/레벨 또는 피해 계산 전 DamageEvent가 필요한 패시브 모듈의 계약.
/// 호스트가 OnAcquire, OnChangedLevel, OnAttackHit, OnLose를 전달해야 합니다.
/// 기존 PassiveModule.OnHit의 DamageData는 DamageEvent를 대체할 수 없습니다.
/// </summary>
[Serializable]
public abstract class PassiveContextModule : PassiveModule
{
    [NonSerialized] protected GameObject owner;
    [NonSerialized] protected int skillLevel = 1;

    public virtual void OnAcquire(GameObject skillOwner, int level)
    {
        owner = skillOwner;
        OnChangedLevel(level);
    }

    public virtual void OnChangedLevel(int level) => skillLevel = level;

    public virtual void OnAttackHit(GameObject attacker, GameObject target, DamageEvent damageEvent) { }

    public override void OnLose() => owner = null;

    protected float GetLevelValue(float[] values, float fallback)
    {
        int index = skillLevel - 1;
        return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
    }
}
