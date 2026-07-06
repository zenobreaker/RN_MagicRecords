using System;
using UnityEngine;

[ModuleCategory("Passive/Spawn Modifier/관통 증가")]
[Serializable]
public sealed class Module_Passive_BonusPierce : PassiveModule
{
    [Header("Target Filter")]
    [Tooltip("어떤 스킬(ID)에만 적용할 것인가? (0이면 조건 없이 발동)")]
    public int targetSkillID = 0;

    [Header("Pierce Settings")]
    [Tooltip("추가할 관통 횟수")]
    public int bonusPierceCount = 2;

    public Module_Passive_BonusPierce()
    {
        triggerTime = PassiveTriggerTime.OnSpawnObject;
    }

    public override void OnSpawnObject(ISkillEffect spawnedObject, ActiveSkill casterSkill)
    {
        // 필터링 검사
        if (targetSkillID != 0 && casterSkill.SkillID != targetSkillID)
            return;

        // IProjectile 인터페이스를 가진 투사체만 관통력 증가
        if (spawnedObject is IProjectile pierceProj)
        {
            pierceProj.PierceCount += bonusPierceCount;
        }
    }
}