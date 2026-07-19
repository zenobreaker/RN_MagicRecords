using System;
using UnityEngine;

[ModuleCategory("Passive/Spawn Modifier/분열탄 모탄 감속")]
[Serializable]
public sealed class Module_Passive_SplitMotherSlow : PassiveModule
{
    [Header("Target Filter")]
    [Tooltip("어떤 스킬(ID)에만 적용할 것인가? (0이면 조건 없이 발동)")]
    public int targetSkillID = 0;

    [Header("Mother Projectile Settings")]
    [Tooltip("모탄 속도 배율입니다. 0.1이면 기본 속도의 10%로 매우 느리게 이동합니다.")]
    [Range(0.01f, 1f)]
    public float speedMultiplier = 0.1f;

    public Module_Passive_SplitMotherSlow()
    {
        triggerTime = PassiveTriggerTime.OnSpawnObject;
    }

    public override void OnSpawnObject(ISkillEffect spawnedObject, ActiveSkill casterSkill)
    {
        if (targetSkillID != 0 && casterSkill.SkillID != targetSkillID)
            return;

        if (spawnedObject is SplitMotherProjectile motherProjectile)
            motherProjectile.SetMotherSpeedMultiplier(speedMultiplier);
    }
}
