using System;
using UnityEngine;

[ModuleCategory("Passive/Spawn Modifier/자탄 분열")]
[Serializable]
public sealed class Module_Passive_SplitProjectile : PassiveModule
{
    public int splitCount = 6;
    public GameObject splitPrefab;

    public Module_Passive_SplitProjectile()
    {
        triggerTime = PassiveTriggerTime.OnSpawnObject;
    }

    public override void OnSpawnObject(ISkillEffect spawnedObject)
    {
        if (spawnedObject is BaseProjectile proj)
        {
            proj.OnTargetHitEvent += (target, hitPoint) =>
            {
                // 분열 로직 (이전과 동일)
            };
        }
    }
}
