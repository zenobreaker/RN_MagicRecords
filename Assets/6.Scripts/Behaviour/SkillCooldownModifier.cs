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

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        bool bCanSkill = SkillComponent.Value.CanUseSkill(SkillKey.Value);
        if (bCanSkill == false)
            return Status.Failure;

        return Child.CurrentStatus switch
        {
            Status.Success => Status.Success,
            Status.Failure => Status.Failure,
            _ => Status.Running
        };
    }
}

