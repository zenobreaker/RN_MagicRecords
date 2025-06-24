using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CheckPatternCooldown", story: "Check [PatternName] of [Pattern] Complete Cooldown", category: "Conditions", id: "68b2fcf006203da842289e4e9324dca6")]
public partial class CheckPatternCooldownCondition : Condition
{
    [SerializeReference] public BlackboardVariable<SkillComponent> Pattern;
    [SerializeReference] public BlackboardVariable<string> PatternName;

    public override bool IsTrue()
    {
        if (Pattern == null || Pattern.Value == null) return false;

        return Pattern.Value.CanUseSkill(PatternName.Value);
    }

}
