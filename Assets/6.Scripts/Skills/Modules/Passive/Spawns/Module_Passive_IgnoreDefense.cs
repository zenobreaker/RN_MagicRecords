using System;
using UnityEngine;

[ModuleCategory("Passive/Spawn Modifier/방어력 관통 (절단광선)")]
[Serializable]
public sealed class Module_Passive_IgnoreDefense : PassiveModule
{
    [Header("Target Filter")]
    [Tooltip("어떤 스킬(ID)에만 적용할 것인가? (예: 1014 = 절단광선)")]
    public int targetSkillID = 1014;

    [Header("Defense Ignore Settings")]
    [Tooltip("방어력 무시 비율 (1.0 = 100% 무시, 0.5 = 50% 무시)")]
    [Range(0f, 1f)]
    public float ignoreRate = 1.0f;

    public Module_Passive_IgnoreDefense()
    {
        triggerTime = PassiveTriggerTime.OnSpawnObject;
    }

    public override void OnSpawnObject(ISkillEffect spawnedObject, ActiveSkill casterSkill)
    {
        if (targetSkillID != 0 && casterSkill.SkillID != targetSkillID)
            return;

        if (spawnedObject is BaseProjectile proj)
        {
            // 💡 DamageData에 방어력 무시 비율을 적용 (중첩을 위해 덧셈 처리)
            if (proj.DamageData != null)
            {
                proj.DamageData.ignoreDefenseRate += ignoreRate;

                // (최대 100%를 넘지 않도록 안전망 추가)
                proj.DamageData.ignoreDefenseRate = Mathf.Clamp01(proj.DamageData.ignoreDefenseRate);
            }
        }
    }
}