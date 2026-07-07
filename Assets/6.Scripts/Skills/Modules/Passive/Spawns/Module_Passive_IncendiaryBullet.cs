using System;
using UnityEngine;

[ModuleCategory("Passive/Spawn Modifier/상태이상 탄환 변환")]
[Serializable]
public sealed class Module_Passive_StatusEffectBullet : PassiveModule
{
    [Header("Target Filter")]
    [Tooltip("어떤 스킬(ID)에만 적용할 것인가? (0이면 조건 없이 발동)")]
    public int targetSkillID = 0;

    [Header("Status Effect Settings")]
    [Tooltip("부여할 상태이상 ID (예: Burn, Bleed, Poison)")]
    public string effectID = "Burn";

    [Tooltip("지속 시간 (초)")]
    public float duration = 3.0f;
    [Tooltip("틱당 데미지")]
    public float power = 5.0f;

    public Module_Passive_StatusEffectBullet()
    {
        triggerTime = PassiveTriggerTime.OnSpawnObject;
    }

    public override void OnSpawnObject(ISkillEffect spawnedObject, ActiveSkill casterSkill)
    {
        if (targetSkillID != 0 && casterSkill.SkillID != targetSkillID)
            return;

        if (spawnedObject is AbstractProjectile proj)
        {
            // 런타임 상태이상 부여기 컴포넌트를 가져오거나 붙여줍니다.
            if (!proj.gameObject.TryGetComponent<RuntimeStatusEffectApplier>(out var effectApplier))
            {
                effectApplier = proj.gameObject.AddComponent<RuntimeStatusEffectApplier>();
            }

            // 💡 인스펙터에서 설정한 문자열(effectID)을 전달!
            effectApplier.Setup(effectID, duration, power);
        }
    }
}