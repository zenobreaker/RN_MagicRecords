using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SkillAction", story: "Try [SkillComponent] From [SkillKey]", category: "Action", id: "bdfdb7ed401e62941ac2ba8bc6fc1f5c")]
public partial class SkillAction : Action
{

    [SerializeReference] public BlackboardVariable<SkillComponent> SkillComponent;
    [SerializeReference] public BlackboardVariable<string> SkillKey;
    
    protected override Status OnStart()
    {
        if (SkillComponent == null || SkillComponent.Value == null) return Status.Failure;

        SkillComponent.Value.UseSkill(SkillKey.Value);

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

