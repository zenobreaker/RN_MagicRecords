using System;
using UnityEngine;

[ModuleCategory("Passive/Spawn Modifier/분열탄 자탄 유도")]
[Serializable]
public sealed class Module_Passive_SplitChildHoming : PassiveModule
{
    [Header("Target Filter")]
    [Tooltip("어떤 스킬(ID)에만 적용할 것인가? (0이면 조건 없이 발동)")]
    public int targetSkillID = 0;

    [Header("Homing Settings")]
    [Tooltip("플레이어 기준으로 우선 대상(보스 → 엘리트 → 노멀)을 탐색할 반경")]
    public float searchRadius = 12f;
    [Tooltip("자탄의 유도 회전 속도")]
    public float turnSpeed = 12f;
    [Tooltip("탐색할 적 레이어")]
    public LayerMask enemyLayer;

    public Module_Passive_SplitChildHoming()
    {
        triggerTime = PassiveTriggerTime.OnSpawnObject;
    }

    public override void OnSpawnObject(ISkillEffect spawnedObject, ActiveSkill casterSkill)
    {
        if (targetSkillID != 0 && casterSkill.SkillID != targetSkillID)
            return;

        if (spawnedObject is SplitMotherProjectile motherProjectile)
        {
            motherProjectile.EnableChildHoming(
                searchRadius,
                turnSpeed,
                enemyLayer,
                casterSkill.Owner != null ? casterSkill.Owner.transform : null);
        }
    }
}
