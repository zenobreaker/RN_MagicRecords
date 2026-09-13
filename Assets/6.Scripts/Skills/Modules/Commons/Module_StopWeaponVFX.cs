using System;
using UnityEngine;

[ModuleCategory("Common/StopWeaponVFX")]
[Serializable]
public class Module_StopWeaponVFX : SkillModule
{
    [Tooltip("같은 스킬의 StartWeaponVFX에 설정한 ID입니다.")]
    public string effectId = "WeaponVFX";

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (owner != null && owner.TryGetComponent<SkillVFXComponent>(out var skillVFX))
            skillVFX.RemoveEffects(skill, effectId);
    }
}
