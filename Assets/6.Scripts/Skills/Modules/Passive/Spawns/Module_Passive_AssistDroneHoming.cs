using System;
using UnityEngine;

[ModuleCategory("Passive/Assist Weapon/일반탄 유도")]
[Serializable]
public sealed class Module_Passive_AssistDroneHoming : PassiveModule
{
    [Header("Homing Settings")]
    [Tooltip("일반탄이 적을 탐색할 반경")]
    public float searchRadius = 7f;
    [Tooltip("유도 회전 속도")]
    public float turnSpeed = 10f;
    [Tooltip("탐색할 적 레이어")]
    public LayerMask enemyLayer;

    public Module_Passive_AssistDroneHoming()
    {
        triggerTime = PassiveTriggerTime.OnSpawnObject;
    }

    public override void OnAssistDroneNormalProjectile(ISkillEffect spawnedObject, Character owner)
    {
        if (!(spawnedObject is BaseProjectile projectile))
            return;

        if (!projectile.gameObject.TryGetComponent<RuntimeHomingBehaviour>(out var homingBehaviour))
            homingBehaviour = projectile.gameObject.AddComponent<RuntimeHomingBehaviour>();

        homingBehaviour.Setup(searchRadius, turnSpeed, enemyLayer, projectile.Ignores);
    }
}
