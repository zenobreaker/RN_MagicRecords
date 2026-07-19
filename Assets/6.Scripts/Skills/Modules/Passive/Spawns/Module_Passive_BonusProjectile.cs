using System;
using UnityEngine;


[ModuleCategory("Passive/Spawn Modifier/추가 탄환")]
[Serializable]
public class Module_Passive_BonusProjectile : PassiveModule
{
    [Header("Target Filter")]
    [Tooltip("어떤 스킬(ID)에만 적용할 것인가? (0이면 조건 없이 발동)")]
    public int targetSkillID = 0;

    [Header("Bonus Projectile")]
    [Tooltip("한 번의 발사에 추가할 프로젝타일 개수 (기본 1발에 이 값만큼 더해집니다)")]
    public int bonusCount = 1;

    [Tooltip("추가할 프로젝타일의 오브젝트 정보")]
    public string bonusPrefabName;
    public GameObject bonusPrefab;

    [Tooltip("감소시킬 퍼짐 각도 (예: -15.0 이면 15도만큼 좁아짐)")]
    public float bonusAngle;

    [Header("Spawn Origin Bonus")]
    public Vector3 bonusPos;

    public Module_Passive_BonusProjectile()
    {
        triggerTime = PassiveTriggerTime.OnSkillCast;
    }

    public override void OnSkillCast(SkillUseEvent evt, SkillRuntimeContext context)
    {
        // 타겟 스킬 검사
        if (targetSkillID != 0 && evt.SkillID != targetSkillID)
            return;

        //CombatContext의 보너스 수치를 조작하여 런타임 프로퍼티에 반영되게 합니다.
        context.Combat.PatternCountBonus += bonusCount;
        context.Combat.PatternAngleBonus += bonusAngle;
        
        if(!string.IsNullOrEmpty(bonusPrefabName))
        context.Spawn.OverridePrefabName = bonusPrefabName;
    }
}
