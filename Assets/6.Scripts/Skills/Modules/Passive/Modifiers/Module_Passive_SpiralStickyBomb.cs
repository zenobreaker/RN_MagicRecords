using System;
using UnityEngine;

[ModuleCategory("Passive/Skill Modifier/스파이럴 끈적이 폭탄 (부착형)")]
[Serializable]
public sealed class Module_Passive_SpiralStickyBomb : PassiveModule
{
    [Header("타겟 스킬 설정")]
    [Tooltip("이 효과를 적용할 특정 스킬의 ID (0이면 모든 스킬에 적용)")]
    public int targetSkillID;

    [Header("끈적이 폭탄 설정")]
    //public float tickInterval = 0.2f;    // 다단 히트 간격
    //public float tickDamage = 10f;       // 틱당 데미지
    public float explosionRadius = 3.0f; // 막타 폭발 반경
    public float explosionDamage = 100f; // 막타 폭발 데미지
    public LayerMask enemyLayer;         // 적 레이어

    private DamageEvent damageEvent; 

    public Module_Passive_SpiralStickyBomb()
    {
        // 투사체(스킬 이펙트)가 스폰될 때 이 모듈이 작동하도록 트리거 타이밍 설정
        triggerTime = PassiveTriggerTime.OnSpawnObject;
    }

    public override void OnSpawnObject(ISkillEffect spawnedObject, ActiveSkill casterSkill)
    {
        // 1. 타겟 스킬 필터링 (지정된 스킬이 아니면 패스)
        if (targetSkillID != 0 && casterSkill.SkillID != targetSkillID)
            return;


        damageEvent = casterSkill.damageData.GetMyDamageEvent(
            casterSkill.Status);

        // 2. '부착형 폭탄 러너(행동)' 객체를 기획 데이터와 함께 생성
        var bombRunner = new SpiralBomb(
            enemyLayer,
            casterSkill.Owner,
            explosionRadius,
            damageEvent
        );

        // 3. 투사체 본체에 러너 주입! 
        if(spawnedObject is PiercingDrillProjectile pdp)
        {
            pdp.AddSpawnRunner(bombRunner);
            pdp.AddHitRunner(bombRunner);
            pdp.AddDestroyRunner(bombRunner);
            pdp.AddUpdateRunner(bombRunner);
        }
    }
}