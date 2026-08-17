using System;
using UnityEngine;

[ModuleCategory("Utility/Casting")]
[Serializable]
public sealed class Module_Casting : SkillModule
{
    [Tooltip("체크하면 SO_ActiveSkillData의 레벨별 Casting Time 대신 아래 값을 사용합니다.")]
    public bool overrideCastingTime;

    [Tooltip("캐스팅 시간입니다. 0 이하는 캐스팅 없이 즉시 다음 액션을 실행합니다.")]
    [Min(0f)]
    public float castingTime = 0.5f;

    public Module_Casting()
    {
        triggerTime = SkillTriggerTime.OnCastingStart;
    }

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (skill == null || !overrideCastingTime)
            return;

        skill.Runtime.Cast.CastingTime = castingTime;
    }
}
