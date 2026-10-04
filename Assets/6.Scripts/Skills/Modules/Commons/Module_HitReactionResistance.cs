using System;
using UnityEngine;

[Serializable]
[ModuleCategory("Common/Hit Reaction Resistance")]
public class Module_HitReactionResistance : SkillModule
{
    public HitReactionResistance resistance = HitReactionResistance.All;

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (owner != null) owner.GetComponent<StateComponent>()?.SetReactionResistance(this, resistance);
    }

    public override void OnPhaseExit(Character owner, ActiveSkill skill, PhaseSkill phase)
    {
        if (owner != null) owner.GetComponent<StateComponent>()?.RemoveReactionResistance(this);
    }
}
