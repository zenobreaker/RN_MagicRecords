using System;
using UnityEngine;

[Serializable]
[ModuleCategory("Passive/Magic Bullet/자동 장전 시간 감소")]
public sealed class Module_Passive_AutoReload : PassiveContextModule
{
    [Range(0f, 0.95f)] public float reloadTimeReduction = 0.3f;
    public float Reduction => Mathf.Clamp(reloadTimeReduction, 0f, 0.95f);
    private SkillComponent registeredOwner;

    public Module_Passive_AutoReload() { triggerTime = PassiveTriggerTime.OnAcquire; }

    public override void OnAcquire(GameObject skillOwner, int level)
    {
        OnLose();
        base.OnAcquire(skillOwner, level);
        registeredOwner = skillOwner.SafeInvoke(value => value.GetComponent<SkillComponent>());
        registeredOwner.SafeInvoke(component => component.RegisterCapability(this));
    }

    public override void OnLose()
    {
        if (registeredOwner != null && ReferenceEquals(registeredOwner.GetCapability<Module_Passive_AutoReload>(), this))
            registeredOwner.UnregisterCapability<Module_Passive_AutoReload>();
        registeredOwner = null;
        base.OnLose();
    }
}
