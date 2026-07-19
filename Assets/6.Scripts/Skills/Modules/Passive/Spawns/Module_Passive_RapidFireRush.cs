using System;
using UnityEngine;

[ModuleCategory("Passive/Skill Modifier/속사 (발사 속도 증가 & 연사 횟수 감소)")]
[Serializable]
public sealed class Module_Passive_RapidFireRush : PassiveModule
{
    [Header("Target Filter")]
    [Tooltip("어떤 스킬(ID)에만 적용할 것인가? (0이면 조건 없이 발동)")]
    public int targetSkillID = 0;

    [Header("Rapid Fire Settings")]
    [Tooltip("발사 간격 배율입니다. 0.7이면 기존 간격의 70%가 되어 더 빠르게 발사합니다.")]
    [Range(0.01f, 1f)]
    public float fireIntervalMultiplier = 0.7f;

    [Tooltip("감소시킬 연사 횟수입니다. 최종 연사 횟수는 최소 1발로 제한됩니다.")]
    [Min(0)]
    public int totalShotsReduction = 2;

    public Module_Passive_RapidFireRush()
    {
        triggerTime = PassiveTriggerTime.OnSkillCast;
    }

    public override void OnSkillCast(SkillUseEvent evt, SkillRuntimeContext context)
    {
        if (targetSkillID != 0 && evt.SkillID != targetSkillID)
            return;

        context.Combat.FireIntervalMultiplier *= fireIntervalMultiplier;
        context.Combat.TotalShotsBonus -= totalShotsReduction;
    }
}
