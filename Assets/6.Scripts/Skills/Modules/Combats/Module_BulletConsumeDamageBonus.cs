using System;
using UnityEngine;

[Serializable]
[ModuleCategory("Combat/Magic Bullet/소비 탄환 피해 보너스")]
public sealed class Module_BulletConsumeDamageBonus : SkillModule
{
    [Min(0f), Tooltip("탄환 1개당 추가 피해 비율. 0.1 = 10%")]
    public float bonusPerBullet = 0.1f;

    public Module_BulletConsumeDamageBonus()
    {
        triggerTime = SkillTriggerTime.OnCastingStart;
    }

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        var runtime = skill?.Runtime;
        if (runtime?.Combat == null || !runtime.TryExecuteOnce(this)) return;
        runtime.Combat.BonusMultipiler *= 1f +
            runtime.Combat.ConsumedBulletCount * Mathf.Max(0f, bonusPerBullet);
    }
}
