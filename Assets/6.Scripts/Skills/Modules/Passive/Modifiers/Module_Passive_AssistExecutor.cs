using System;
using UnityEngine;

[Serializable]
[ModuleCategory("Passive/Assist Weapon/익스큐터")]
public sealed class Module_Passive_AssistExecutor : PassiveContextModule
{
    public int targetSkillID = 1010;
    public Module_AssistExecutor execution = new Module_AssistExecutor();
    [Range(0.01f, 1f)] public float cooldownMultiplier = 0.2f;

    public override int TargetSkillID => targetSkillID;

    public override void OnAcquire(GameObject skillOwner, int level)
    {
        OnLose();
        base.OnAcquire(skillOwner, level);
        owner?.GetComponent<SkillComponent>()?.RegisterCapability(this);
    }

    public override void OnLose()
    {
        var skills = owner != null ? owner.GetComponent<SkillComponent>() : null;
        if (skills != null && ReferenceEquals(skills.GetCapability<Module_Passive_AssistExecutor>(), this))
        {
            skills.UnregisterCapability<Module_Passive_AssistExecutor>();
        }
        base.OnLose();
    }
}
