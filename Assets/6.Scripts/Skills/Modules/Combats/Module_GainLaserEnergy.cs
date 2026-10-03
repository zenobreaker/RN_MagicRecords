using System;
using UnityEngine;

[Serializable]
[ModuleCategory("Combat/Magic Bullet/레이저 에너지 획득")]
public sealed class Module_GainLaserEnergy : SkillModule
{
    [Min(0)] public int baseGain = 1;
    [Min(0)] public int gainPerConsumedBullet = 1;
    public Sprite energyIcon;
    [Tooltip("이번 시전에서 에너지를 소비했다면 기본/탄환 보너스 에너지를 획득하지 않습니다.")]
    public bool skipIfEnergyConsumed = true;

    public Module_GainLaserEnergy()
    {
        triggerTime = SkillTriggerTime.OnCastingStart;
    }

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        var runtime = skill?.Runtime;
        if (owner == null || runtime?.Combat == null || !runtime.TryExecuteOnce(this)) return;
        if (skipIfEnergyConsumed && runtime.Combat.ConsumedLaserEnergyCount > 0) return;
        long gain = Math.Max(0, baseGain) +
            (long)runtime.Combat.ConsumedBulletCount * Math.Max(0, gainPerConsumedBullet);
        // Stack은 캐릭터의 EffectComponent가 소유합니다. 모듈에는 누적 상태를 두지 않습니다.
        var effects = EffectManager.Instance;
        if (effects == null || !owner.TryGetComponent<EffectComponent>(out _)) return;
        for (long i = 0; i < gain; i++)
        {
            effects.RegisterEffect(owner.gameObject, owner.gameObject, new LaserEnergyEffect(energyIcon));
        }
    }
}
