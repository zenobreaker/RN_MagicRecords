using System;
using UnityEngine;

[ModuleCategory("Animation/Play Animation")]
[Serializable]
public class Module_PlayAnimation : SkillModule
{
    [Header("Skill Action")]
    public ActionData actionData;
    public bool restartFromBeginning;
    [Tooltip("MovementStart에서 기존 Dash/Evade 애니메이션을 이동 방향에 맞춰 재생합니다.")]
    public bool useMovementAnimation;
    [Tooltip("연사 간격에 맞춰 재생 속도를 설정합니다.")]
    public bool syncFireInterval;
    [Min(0.01f)] public float fireInterval = 0.1f;

    private WeaponController weaponController;

    public override void Init(Character owner)
    {
        actionData?.Initialize();

        weaponController = owner.GetComponent<IWeaponUser>()?.GetWeaponController();
    }

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (useMovementAnimation)
        {
            if (owner != null && skill != null)
                owner.Visual?.PlayDashAnimation(skill.Runtime.IsBackwardMovement);
            return;
        }
        if (owner == null || actionData == null) return;
        var playback = actionData;
        if (syncFireInterval)
        {
            playback = actionData.Clone();
            float interval = fireInterval * (skill?.Runtime.FireIntervalMultiplier ?? 1f);
            if (skill != null && phaseSkill?.modules != null)
                foreach (var module in phaseSkill.modules)
                    if (module is Module_PhaseLoop && module.triggerTime == SkillTriggerTime.OnPhaseTime)
                    {
                        interval = module.GetTriggerDelay(skill);
                        break;
                    }
            if (skill?.Runtime.ActivePhaseLoop != null)
                interval = skill.Runtime.ActivePhaseLoop.GetTriggerDelay(skill);
            playback.ActionSpeed = 1f / Mathf.Max(0.01f, interval);
            playback.Initialize();
        }
        if (restartFromBeginning && owner.Visual != null)
        {
            float speed = owner.Status != null ? owner.Status.GetStatusValue(StatusType.ATTACKSPEED) : 1f;
            owner.Visual.PlayActionAnimation(playback, 0, speed, true);
        }
        else owner.PlayAction(playback);
        if (weaponController != null) weaponController.DoAction(playback, restartFromBeginning);
        if (skill != null) skill.actionData = playback;
    }

    public override bool HasAnimationData()
    {
        if (useMovementAnimation) return true;
        // 모듈에 세팅된 actionData의 서브스테이트 이름이 비어있지 않으면 true 반환!
        return actionData != null && !string.IsNullOrEmpty(actionData.SubStateName);
    }
}
