using System;
using UnityEngine;

[ModuleCategory("Passive/Skill Modifier/충전 삭제")]
[Serializable]
public sealed class Module_Passive_RemoveCharge : PassiveModule
{
    [Header("Target Filter")]
    public int targetSkillID;

    public Module_Passive_RemoveCharge()
    {
        // 💡 스킬 발동 직전(시전 시)에 개입합니다.
        triggerTime = PassiveTriggerTime.OnSkillCast;
    }

    public override void OnSkillCast(SkillUseEvent evt, SkillRuntimeContext context)
    {
        if (targetSkillID != 0 && evt.SkillID != targetSkillID)
            return;

        context.Cast.CastingTime = 0f;
        // 충전 계산 모듈에서 시간에 대해서 처리해서 Bonus값에 대입하므로 계산식 수정
        context.Cast.ChargedTime = context.Cast.MaxChargeTime * 0.5f;
        
        context.Cast.IsInstantCast = true; 
    }
}