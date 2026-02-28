using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CheckTargetIsNull", story: "Check the [Target] is Null", category: "Conditions", id: "4398df3b15ca827a0351e73a7fce8f80")]
public partial class CheckTargetIsNullCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    public override bool IsTrue()
    {
        if (Target == null || Target.Value == null)
            return true;

        return false;
    }
}
