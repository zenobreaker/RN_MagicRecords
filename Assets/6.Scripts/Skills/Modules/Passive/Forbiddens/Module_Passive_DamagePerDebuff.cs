using System;
using UnityEngine;

[Serializable]
[ModuleCategory("Passive/Curse/디버프 수 비례 피해 증폭")]
public sealed class Module_Passive_DamagePerDebuff : PassiveContextModule
{
    [Min(0f)] public float damageAmpPerEffect = 0.1f;
    [Min(0f)] public float damageAmpPerLevel;

    public Module_Passive_DamagePerDebuff() => triggerTime = PassiveTriggerTime.OnHit;

    public override void OnAttackHit(GameObject attacker, GameObject target, DamageEvent damageEvent)
    {
        if (attacker == null || target == null || damageEvent == null) return;
        if (target.TryGetComponent(out EffectComponent effects))
            damageEvent.DamageAmp = 1f + effects.DebuffCount *
                (damageAmpPerEffect + Mathf.Max(0, skillLevel - 1) * damageAmpPerLevel);
    }
}
