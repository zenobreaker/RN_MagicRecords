using System;
using UnityEngine;

[ModuleCategory("Combat/Set DamageData")]
[Serializable]
public class Module_SetDamageData : SkillModule
{
    [Header("Damage Data")]
    public DamageData DamageData;

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        skill.damageData = DamageData; 
    }
}
