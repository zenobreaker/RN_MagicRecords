using System;
using UnityEngine;

[Serializable]
[ModuleCategory("Passive/Curse/적중 시 저주 부여")]
public sealed class Module_Passive_ApplyCurseOnHit : PassiveContextModule
{
    [Range(0f, 1f)] public float chance = 1f;
    [Min(0f)] public float duration = 5f;

    public Module_Passive_ApplyCurseOnHit() => triggerTime = PassiveTriggerTime.OnHit;

    public override void OnAttackHit(GameObject attacker, GameObject target, DamageEvent damageEvent)
    {
        if (target == null || UnityEngine.Random.value > chance) return;
        // 원본과 동일하게 공격자 필터 없이, 패시브 소유자를 저주 시전자로 전달합니다.
        EffectManager.Instance?.RegisterEffect_Curse(target, owner, duration);
    }
}
