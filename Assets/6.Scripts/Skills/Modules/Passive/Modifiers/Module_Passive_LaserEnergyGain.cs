using System;
using UnityEngine;

[Serializable]
[ModuleCategory("Passive/Skill Modifier/레이저 에너지 획득량 증가")]
public sealed class Module_Passive_LaserEnergyGain : PassiveModule
{
    public int targetSkillID;
    public override int TargetSkillID => targetSkillID;

    [Min(0)] public int bonusGain = 5;

    public Module_Passive_LaserEnergyGain()
    {
        triggerTime = PassiveTriggerTime.OnSkillCast;
    }

    public override void OnSkillCast(SkillUseEvent evt, SkillRuntimeContext context)
    {
        if (evt == null || context?.Combat == null ||
            (targetSkillID != 0 && evt.SkillID != targetSkillID)) return;

        context.Combat.LaserEnergyGainBonus += Math.Max(0, bonusGain);
    }
}
