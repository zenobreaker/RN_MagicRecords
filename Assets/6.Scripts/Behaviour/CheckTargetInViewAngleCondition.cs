using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CheckTargetInViewAngle", story: "Check Look At [Target] in [ViewAngle] form [Perception]", category: "Conditions", id: "34490bf57175c2e976999ad7157becd2")]
public partial class CheckTargetInViewAngleCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> ViewAngle;
    [SerializeReference] public BlackboardVariable<PerceptionComponent> Perception;

    public override bool IsTrue()
    {
        if(Target == null || Target.Value == null) return false;
        if (Perception == null || Perception.Value == null) return false;

        return Perception.Value.GetLooAtTarget(ViewAngle.Value, Target.Value.transform.position); ;
    }
}
