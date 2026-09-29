using System;
using UnityEngine;

[Serializable]
[ModuleCategory("Passive/Magic Bullet/마지막 평타 마탄 사용 허용")]
public sealed class Module_Passive_LastAttackMagicBullet : PassiveContextModule
{
    private SkillComponent registeredOwner;

    public Module_Passive_LastAttackMagicBullet() { triggerTime = PassiveTriggerTime.OnAcquire; }

    public override void OnAcquire(GameObject skillOwner, int level)
    {
        OnLose();
        base.OnAcquire(skillOwner, level);
        registeredOwner = skillOwner.SafeInvoke(value => value.GetComponent<SkillComponent>());
        registeredOwner.SafeInvoke(component => component.RegisterCapability(this));
    }

    public override void OnLose()
    {
        if (registeredOwner != null && ReferenceEquals(registeredOwner.GetCapability<Module_Passive_LastAttackMagicBullet>(), this))
            registeredOwner.UnregisterCapability<Module_Passive_LastAttackMagicBullet>();
        registeredOwner = null;
        base.OnLose();
    }
}
