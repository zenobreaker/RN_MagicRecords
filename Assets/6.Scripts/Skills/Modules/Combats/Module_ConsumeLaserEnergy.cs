using System;
using UnityEngine;

[Serializable]
[ModuleCategory("Combat/Magic Bullet/레이저 에너지 소비 보너스")]
public sealed class Module_ConsumeLaserEnergy : SkillModule
{
    [Min(1)] public int energyCost = 10;
    [Range(0f, 1f)] public float ignoreDefenseBonus;
    [Range(0.01f, 1f)] public float chargeTimeMultiplier = 1f;

    public Module_ConsumeLaserEnergy()
    {
        triggerTime = SkillTriggerTime.OnCastingStart;
    }

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        var runtime = skill?.Runtime;
        if (owner == null || runtime?.Combat == null || runtime.Cast == null ||
            energyCost <= 0 || !runtime.TryExecuteOnce(this) ||
            !owner.TryGetComponent<EffectComponent>(out var effects)) return;

        if (!effects.TryConsumeStacks(LaserEnergyEffect.EffectID, energyCost)) return;

        runtime.Combat.ConsumedLaserEnergyCount += energyCost;
        runtime.Combat.IgnoreDefenseBonus += Mathf.Clamp01(ignoreDefenseBonus);
        runtime.Cast.ChargeSpeedMultiplier /= Mathf.Clamp(chargeTimeMultiplier, 0.01f, 1f);
    }
}
