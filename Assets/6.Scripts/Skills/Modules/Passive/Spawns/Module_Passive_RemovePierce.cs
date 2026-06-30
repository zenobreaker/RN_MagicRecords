using System;

[ModuleCategory("Passive/Spawn Modifier/관통 제거")]
[Serializable]
public sealed class Module_Passive_RemovePierce : PassiveModule
{
    // 필터: 특정 스킬(예: 매그넘샷)에만 적용하고 싶을 때
    public int targetSkillID = 1002;

    public Module_Passive_RemovePierce()
    {
        triggerTime = PassiveTriggerTime.OnSpawnObject;
    }

    public override void OnSpawnObject(ISkillEffect spawnedObject)
    {
        if (spawnedObject is IProjectile proj)
        {
            // 이 투사체가 내가 개조하려는 그 스킬의 투사체인지 확인하는 로직 필요
            proj.PierceCount = 0;
        }
    }
}

