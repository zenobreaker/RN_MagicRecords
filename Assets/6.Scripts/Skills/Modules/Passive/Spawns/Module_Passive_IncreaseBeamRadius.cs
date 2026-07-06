using System;
using UnityEngine;

[ModuleCategory("Passive/Spawn Modifier/빔 두께 증가 (과충전)")]
[Serializable]
public sealed class Module_Passive_IncreaseBeamRadius : PassiveModule
{
    [Header("Target Filter")]
    [Tooltip("어떤 스킬(ID)에만 적용할 것인가? (예: 1013 = 과충전)")]
    public int targetSkillID = 1013;

    [Header("Beam Settings")]
    [Tooltip("증가시킬 빔의 두께 (반지름)")]
    public float bonusRadius = 1.0f;

    public Module_Passive_IncreaseBeamRadius()
    {
        triggerTime = PassiveTriggerTime.OnSpawnObject;
    }

    public override void OnSpawnObject(ISkillEffect spawnedObject, ActiveSkill casterSkill)
    {
        if (targetSkillID != 0 && casterSkill.SkillID != targetSkillID)
            return;

        // 💡 생성된 오브젝트가 빔 프로젝타일이라면 두께를 늘려줍니다!
        if (spawnedObject is BeamProjectile beam)
        {
            beam.BeamRadius += bonusRadius;
        }
    }
}