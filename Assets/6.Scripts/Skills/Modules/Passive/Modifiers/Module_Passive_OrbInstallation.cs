using System;
using UnityEngine;

[ModuleCategory("Passive/Skill Modifier/설치형 구체 변환")]
[Serializable]
public sealed class Module_Passive_OrbInstallation : PassiveModule
{
    [Header("Target Filter")]
    public int targetSkillID;

    [Header("Orb Settings")]
    [Tooltip("기존 광선 대신 소환할 구체의 프리팹 이름")]
    public string orbPrefabName = "Orb_DestructionRay";

    public Module_Passive_OrbInstallation()
    {
        triggerTime = PassiveTriggerTime.OnSkillCast;
    }

    public override void OnSkillCast(SkillUseEvent evt, SkillRuntimeContext context)
    {
        if (targetSkillID != 0 && evt.SkillID != targetSkillID)
            return;

        context.Cast.IsInstantCast = true;
        context.Cast.AutoFireOnMaxCharge = true;
        // 충전 계산 모듈에서 시간에 대해서 처리해서 Bonus값에 대입하므로 계산식 수정
        context.Cast.ChargedTime = context.Cast.MaxChargeTime;

        context.Spawn.OverridePrefabName = orbPrefabName;
    }
}