using System;
using UnityEngine;

[ModuleCategory("Passive/Skill Modifier/초집중 사격 (탄수 증가 & 집탄률 증가)")]
[Serializable]
public sealed class Module_Passive_FocusedFire : PassiveModule
{
    [Header("Target Filter")]
    [Tooltip("어떤 스킬(ID)에만 적용할 것인가? (예: 산탄 사격 스킬 ID)")]
    public int targetSkillID;

    [Header("Focused Fire Settings")]
    [Tooltip("추가로 발사할 산탄의 개수")]
    public int bonusPatternCount = 2;

    [Tooltip("감소시킬 퍼짐 각도 (예: -15.0 이면 15도만큼 좁아짐)")]
    public float bonusPatternAngle = -15.0f;

    public Module_Passive_FocusedFire()
    {
        // 💡 스킬이 시전되기 직전(런타임 컨텍스트가 계산될 때)에 개입합니다.
        triggerTime = PassiveTriggerTime.OnSkillCast;
    }

    public override void OnSkillCast(SkillUseEvent evt, SkillRuntimeContext context)
    {
        // 타겟 스킬 검사
        if (targetSkillID != 0 && evt.SkillID != targetSkillID)
            return;

        // 💡 CombatContext의 보너스 수치를 조작하여 런타임 프로퍼티에 반영되게 합니다.
        context.Combat.PatternCountBonus += bonusPatternCount;
        context.Combat.PatternAngleBonus += bonusPatternAngle;

        // (선택) 근접 화력이 강해지는 대신, 밸런스를 위해 전체 데미지 배율을 살짝 깎고 싶다면?
        // context.Combat.BonusMultipiler -= 0.1f; 
    }
}