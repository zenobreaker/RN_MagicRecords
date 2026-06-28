using System;
using UnityEngine;

[ModuleCategory("Combat/Set Pattern Data")]
[Serializable]
public class Module_SetPatternData : SkillModule
{
    [Header("Pattern Settings")]
    [Tooltip("발사할 투사체/장판의 개수")]
    public int spawnCount = 1;

    [Tooltip("투사체 사이의 각도")]
    public float angleBetween = 0f;

    // 필요하다면 타겟팅 관련 값도 여기서 한 번에 세팅 가능
    // public TargetPositionType targetType = TargetPositionType.CasterForward;

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        // 아주 심플하게 블랙보드에 값만 올려두고 끝납니다.
        skill.Runtime.Base.PatternCount = spawnCount;
        skill.Runtime.Base.PatternAngle = angleBetween;
    }
}