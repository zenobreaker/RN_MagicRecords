using System;
using UnityEngine;

/// <summary>
/// 지정된 공격 페이즈에서 기존 마법탄 공급자를 통해 탄환을 장전합니다.
/// </summary>
[Serializable]
[ModuleCategory("Special/MagicBulletReload")]
public sealed class Module_ReloadMagicBullet : SkillModule
{
    [Min(0), Tooltip("이번 공격에서 장전할 마법탄 수")]
    public int reloadCount = 1;

    [Tooltip("피니시 드라이브를 보유했을 때만 장전합니다.")]
    public bool requireLastAttackPassive = true;

    /// <summary>실제 타격 시점에 페이즈당 한 번 장전하도록 설정합니다.</summary>
    public Module_ReloadMagicBullet()
    {
        triggerTime = SkillTriggerTime.OnJudgeAttack;
        executeOncePerPhase = true;
    }

    /// <summary>보유 패시브와 공급자를 확인하고 최대 장전 수 이내에서 보충합니다.</summary>
    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (owner == null || skill == null || !skill.IsActive || !skill.IsPhaseRunning || skill.IsEnding) return;
        var component = owner.GetComponent<SkillComponent>();
        if (component == null) return;
        if (requireLastAttackPassive && component.GetCapability<Module_Passive_LastAttackMagicBullet>() == null) return;

        var provider = component.GetCapability<IMagicBulletProvider>();
        if (provider != null) provider.Reload(Mathf.Max(0, reloadCount));
    }
}
