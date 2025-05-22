using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Weapon", story: "Try Attack with [WeaponComponent]", category: "Action", id: "80427657f30cee1fb3681ada612a869e")]
public partial class WeaponAction : Action
{
    [SerializeReference] public BlackboardVariable<WeaponComponent> WeaponComponent;

    protected override Status OnUpdate()
    {
        if (WeaponComponent == null || WeaponComponent.Value == null)
            return Status.Failure;

        WeaponComponent.Value.DoAction();

        return Status.Success;
    }
}

