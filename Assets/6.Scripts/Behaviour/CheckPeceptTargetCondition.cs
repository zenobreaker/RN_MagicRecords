using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CheckPeceptTarget", story: "Check [Percept] the [Target]", category: "Conditions", id: "11189f9928510619097d9be703d6bf2b")]
public partial class CheckPeceptTargetCondition : Condition
{
    [SerializeReference] public BlackboardVariable<PerceptionComponent> Percept;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    public override bool IsTrue()
    {
        if (Percept == null || Percept.Value == null)
            return false; 
        
        return Percept.Value.GetTarget() == Target.Value;
    }
}
