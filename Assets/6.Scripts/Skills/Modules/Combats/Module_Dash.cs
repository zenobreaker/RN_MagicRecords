using System;
using UnityEngine;

[ModuleCategory("Movement/Dash")]
[Serializable]
public class Module_Dash : SkillModule
{
    public enum DashDirectionType
    {
        Legacy = 0, InputDirection = 1, Forward = 2, Backward = 3, TargetDirection = 4
    }

    [Header("Dash Settings")]
    public float distance = 5f;
    public float duration = 0.3f;
    [Tooltip("Legacy 방향 모드에서 기존 에셋 설정을 유지합니다.")]
    public bool useTargetPosition = true;
    public DashDirectionType directionType;
    public bool backwardWhenNoInput = true;
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("이동 완료 시 다음 Phase로 진행합니다. 마지막 Phase이면 Skill을 종료합니다.")]
    public bool advancePhaseOnFinish;

    [Header("MoveOverTime Settings")]
    public bool bIsMoveOverTime;
    public bool bIsGhostMode;

    public Vector3 ResolveDirection(Character owner, ActiveSkill skill, MovementComponent movement)
    {
        if (owner == null) return Vector3.zero;
        Vector3 direction;
        switch (directionType)
        {
            case DashDirectionType.InputDirection:
                Vector2 input = movement != null ? movement.TargetDirection : Vector2.zero;
                direction = input.sqrMagnitude > 0.001f
                    ? new Vector3(input.x, 0f, input.y)
                    : owner.transform.forward * (backwardWhenNoInput ? -1f : 1f);
                break;
            case DashDirectionType.Backward:
                direction = -owner.transform.forward;
                break;
            case DashDirectionType.Forward:
                direction = owner.transform.forward;
                break;
            default:
                direction = skill != null &&
                    (directionType == DashDirectionType.TargetDirection || useTargetPosition)
                    ? skill.Runtime.Spawn.TargetPosition - owner.transform.position
                    : owner.transform.forward;
                break;
        }
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f) direction = owner.transform.forward;
        direction.y = 0f;
        return direction.normalized;
    }

    public override bool ControlsPhaseLifecycle() => advancePhaseOnFinish;

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (skill == null || !skill.IsActive || skill.IsEnding) return;
        if (owner == null || !owner.isActiveAndEnabled ||
            !owner.TryGetComponent<MovementComponent>(out var movement))
        {
            if (advancePhaseOnFinish) skill.EndSkill();
            return;
        }

        Vector3 direction = ResolveDirection(owner, skill, movement);
        if (directionType == DashDirectionType.InputDirection && movement.TargetDirection.sqrMagnitude > 0.001f)
            owner.transform.rotation = Quaternion.LookRotation(direction);
        skill.Runtime.MovementDirection = direction;
        skill.Runtime.IsBackwardMovement = Vector3.Dot(owner.transform.forward, direction) < -0.001f;
        // Legacy monster skills used this direction for their subsequent locomotion.
        // InputDirection leaves the player's actual input untouched.
        if (directionType == DashDirectionType.Legacy)
            movement.SetDirection(new Vector2(direction.x, direction.z));
        float actualDistance = distance * skill.Runtime.DashDistanceMultiplier;
        float actualDuration = duration * skill.Runtime.DashDurationMultiplier;
        int version = skill.PhaseVersion;
        var lifetime = skill.PhaseToken;
        void Started()
        {
            if (!skill.IsCurrentPhase(version)) return;
            skill.NotifyMovement(SkillTriggerTime.OnMovementStart);
            if (skill.IsCurrentPhase(version))
                skill.NotifyMovement(SkillTriggerTime.OnMovementProgress);
        }
        void Progress(float elapsed)
        {
            if (skill.IsCurrentPhase(version))
                skill.NotifyMovement(SkillTriggerTime.OnMovementProgress, elapsed);
        }
        void Finished(bool completed)
        {
            if (!skill.IsCurrentPhase(version) || lifetime.IsCancellationRequested) return;
            if (completed) skill.NotifyMovement(SkillTriggerTime.OnMovementEnd);
            if (!advancePhaseOnFinish || !skill.IsCurrentPhase(version)) return;
            if (completed) skill.EndPhaseAndNext();
            else skill.EndSkill();
        }
        if (bIsMoveOverTime)
            movement.MoveOverTime(direction, actualDistance, actualDuration, bIsGhostMode,
                Started, Progress, Finished, skill.PhaseToken);
        else
            movement.Dash(direction, actualDistance, actualDuration, speedCurve,
                Started, Progress, Finished, skill.PhaseToken);
    }

    public override SkillModule Clone() => (Module_Dash)base.Clone();
}
