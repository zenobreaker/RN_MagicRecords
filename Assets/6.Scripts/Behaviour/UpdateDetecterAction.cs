using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "UpdateDetecter", story: "Update [Detecter] and assign [Target]", category: "Action", id: "46090536025dae91e51dc3cd55e784c0")]
public partial class UpdateDetecterAction : Action
{
    [SerializeReference] public BlackboardVariable<PerceptionComponent> Detecter;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    protected override Status OnUpdate()
    {
        Target.Value = Detecter.Value.GetTarget();
        return Target.Value == null? Status.Failure : Status.Success;
    }
}

