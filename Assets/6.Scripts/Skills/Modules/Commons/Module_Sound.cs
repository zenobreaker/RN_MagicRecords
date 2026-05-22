using System;
using UnityEngine;

[ModuleCategory("Common/Play Sound")]
[Serializable]
public class Module_Sound : SkillModule
{
    [Header("Skill Sounds")]
    public string soundName;

    public override void OnNotify(GameObject owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        phaseSkill?.actionData?.Play_Sound();
    }
}