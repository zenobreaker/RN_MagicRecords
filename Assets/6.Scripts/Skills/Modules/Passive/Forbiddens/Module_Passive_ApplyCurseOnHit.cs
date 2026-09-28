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
        // 전역 적중 이벤트 중 이 패시브 소유자가 다른 대상에게 가한 공격만 처리합니다.
        if (owner == null || attacker != owner || target == owner) return;
        if (target == null || UnityEngine.Random.value > chance) return;
        EffectManager.Instance.SafeInvoke(manager => manager.RegisterEffect_Curse(target, owner, duration));
    }
}
