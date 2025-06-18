using System;
using Unity.Behavior;
using UnityEngine;
using Modifier = Unity.Behavior.Modifier;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SkillCooldown", story: "Check Cooldown [SkillKey] from [SkillComponent]", category: "Flow", id: "1dead9026ddbedd43466cb237f5018b8")]
public partial class SkillCooldownModifier : Modifier
{
    [SerializeReference] public BlackboardVariable<SkillComponent> SkillComponent;
    [SerializeReference] public BlackboardVariable<string> SkillKey;

    protected override Status OnStart()
    {
        if (SkillComponent == null || SkillComponent.Value == null || SkillKey == null)
            return Status.Failure;

        bool bCanSkill = SkillComponent.Value.CanUseSkill(SkillKey.Value);
        if(bCanSkill)
            return Status.Success;
        else 
            return Status.Failure;
    }
}

