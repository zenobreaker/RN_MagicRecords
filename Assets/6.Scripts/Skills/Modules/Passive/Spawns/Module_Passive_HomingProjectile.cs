using System;
using UnityEngine;

[ModuleCategory("Passive/Spawn Modifier/유도탄 부여")]
[Serializable]
public sealed class Module_Passive_HomingProjectile : PassiveModule
{
    [Header("Target Filter")]
    [Tooltip("어떤 스킬(ID)에만 적용할 것인가? (0이면 조건 없이 발동)")]
    public int targetSkillID = 0;

    [Header("Homing Settings")]
    [Tooltip("적을 탐지할 반경")]
    public float searchRadius = 7.0f;
    [Tooltip("유도 회전 속도 (높을수록 급격하게 꺾임)")]
    public float turnSpeed = 10.0f;
    [Tooltip("적을 찾아갈 타겟 레이어")]
    public LayerMask enemyLayer;

    public Module_Passive_HomingProjectile()
    {
        triggerTime = PassiveTriggerTime.OnSpawnObject;
    }

    public override void OnSpawnObject(ISkillEffect spawnedObject, ActiveSkill casterSkill)
    {
        // 필터링 검사
        if (targetSkillID != 0 && casterSkill.SkillID != targetSkillID)
            return;

        if (spawnedObject is BaseProjectile proj)
        {
            // 💡 1. 이미 컴포넌트가 붙어있는지 확인 (TryGetComponent는 성능이 매우 빠름)
            if (!proj.gameObject.TryGetComponent<RuntimeHomingBehaviour>(out var homingScript))
            {
                // 없으면 새로 달아줍니다. (최초 1회만 실행됨)
                homingScript = proj.gameObject.AddComponent<RuntimeHomingBehaviour>();
            }

            // 💡 2. 있든 없든, 꺼내온 컴포넌트의 Setup을 다시 호출해서 '새것처럼' 덮어씌웁니다!
            homingScript.Setup(searchRadius, turnSpeed, enemyLayer, proj.Ignores);
        }
    }
}