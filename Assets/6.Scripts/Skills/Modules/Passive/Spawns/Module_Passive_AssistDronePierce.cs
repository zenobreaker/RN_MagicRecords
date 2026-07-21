using System;
using UnityEngine;

[ModuleCategory("Passive/Assist Weapon/일반탄 관통 증가")]
[Serializable]
public sealed class Module_Passive_AssistDronePierce : PassiveModule
{
    [Header("Pierce Settings")]
    [Tooltip("일반탄에 추가할 관통 횟수")]
    [Min(0)]
    public int bonusPierceCount = 1;

    public Module_Passive_AssistDronePierce()
    {
        triggerTime = PassiveTriggerTime.OnSpawnObject;
    }

    public override void OnAssistDroneNormalProjectile(ISkillEffect spawnedObject, Character owner)
    {
        if (spawnedObject is IProjectile projectile)
            projectile.PierceCount += bonusPierceCount;
    }
}
