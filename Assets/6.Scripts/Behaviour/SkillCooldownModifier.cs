using System;
using Unity.Behavior;
using UnityEngine;
using Modifier = Unity.Behavior.Modifier;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SkillCooldown", story: "Check Cooldown [SkillKey]", category: "Flow", id: "1dead9026ddbedd43466cb237f5018b8")]
public partial class SkillCooldownModifier : Modifier
{
    [SerializeReference] public BlackboardVariable<string> SkillKey;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

