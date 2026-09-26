using System;
using UnityEngine;

[Serializable]
[ModuleCategory("Passive/Curse/잃은 체력 비례 추가 피해")]
public sealed class Module_Passive_MissingHealthDamage : PassiveContextModule
{
    [Min(0f)] public float missingHealthRatio = 0.02f;

    public Module_Passive_MissingHealthDamage() => triggerTime = PassiveTriggerTime.OnHit;

    public override void OnAttackHit(GameObject attacker, GameObject target, DamageEvent damageEvent)
    {
        // 직접 피해를 가하지 않고 기존 DamageCalculator가 적용할 비율을 설정합니다.
        if (damageEvent != null) damageEvent.MissingHPRatio = missingHealthRatio;
    }
}
