using System;
using UnityEngine;

public enum PhaseLoopTarget { CurrentPhase, SpecificPhase }
public enum PhaseLoopCompleteAction { NextPhase, SpecificPhase, EndSkill }

[Serializable]
[ModuleCategory("Utility/Phase Loop")]
public class Module_PhaseLoop : SkillModule
{
    public PhaseLoopTarget loopTarget = PhaseLoopTarget.CurrentPhase;
    [Min(1), Tooltip("첫 실행을 포함한 총 실행 횟수입니다. 10이면 총 10회 실행합니다.")]
    public int repeatCount = 10;
    public int targetPhaseIndex;
    public PhaseLoopCompleteAction completeAction = PhaseLoopCompleteAction.NextPhase;
    public int completePhaseIndex;
    [Tooltip("연사 패시브의 TotalShots 및 FireInterval 설정과 함께 사용할 때 켭니다.")]
    public bool useRuntimeTotalShots;
    public bool debugLog;

    public override bool ControlsPhaseLifecycle() => true;

    public override void OnPhaseEnter(Character owner, ActiveSkill skill, PhaseSkill phase)
    {
        if (skill == null || !skill.IsActive || skill.IsEnding) return;
        int source = skill.PhaseIndex;
        int target = loopTarget == PhaseLoopTarget.CurrentPhase ? source : targetPhaseIndex;
        int completeTarget = completeAction == PhaseLoopCompleteAction.NextPhase ? source + 1 : completePhaseIndex;
        if (!Validate(skill, source, target, completeTarget)) skill.EndSkill();
        else if (useRuntimeTotalShots && skill.Runtime.Base.TotalShots <= 0) skill.Runtime.Base.TotalShots = repeatCount;
    }

    public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill)
    {
        if (skill == null || !skill.IsActive || skill.IsEnding || !skill.IsPhaseRunning) return;
        var runtime = skill.Runtime;
        bool delegated = ReferenceEquals(runtime.ActivePhaseLoop, this);
        int source = delegated ? runtime.PhaseLoopSourceIndex : skill.PhaseIndex;
        int target = loopTarget == PhaseLoopTarget.CurrentPhase ? source : targetPhaseIndex;
        int completeTarget = completeAction == PhaseLoopCompleteAction.NextPhase ? source + 1 : completePhaseIndex;
        if (!Validate(skill, source, target, completeTarget))
        {
            skill.EndSkill();
            return;
        }

        if (loopTarget == PhaseLoopTarget.SpecificPhase && target != source && !delegated)
        {
            if (runtime.ActivePhaseLoop != null || skill.DoesPhaseControlItself(target))
            {
                Debug.LogError($"[{skill.Name}] Specific PhaseLoop target already has a lifecycle controller.");
                skill.EndSkill();
                return;
            }
            runtime.ActivePhaseLoop = this;
            runtime.PhaseLoopSourceIndex = source;
            runtime.PhaseLoopTargetIndex = target;
            runtime.PhaseLoopLastVersion = -1;
            skill.ChangePhase(target);
            return;
        }

        if (delegated)
        {
            if (runtime.PhaseLoopLastVersion == skill.PhaseVersion) return;
            runtime.PhaseLoopLastVersion = skill.PhaseVersion;
        }
        string key = RuntimeKey ?? $"phase:{source}:loop";
        if (useRuntimeTotalShots && runtime.Base.TotalShots <= 0) runtime.Base.TotalShots = repeatCount;
        int total = useRuntimeTotalShots ? Mathf.Max(1, runtime.TotalShots) : repeatCount;
        int count = runtime.IncrementPhaseLoopCount(key);
        if (debugLog)
            Debug.Log($"[PhaseLoop] Skill={skill.Name}, Current Phase={skill.PhaseIndex}, Loop Count={count}/{total}, Target Phase={target}, Complete Action={completeAction} ({completeTarget})");

        if (count < total)
        {
            if (target == skill.PhaseIndex) skill.RestartCurrentPhase();
            else skill.ChangePhase(target);
            return;
        }

        runtime.ResetPhaseLoopCount(key);
        if (delegated) runtime.ActivePhaseLoop = null;
        if (completeAction == PhaseLoopCompleteAction.EndSkill ||
            (completeAction == PhaseLoopCompleteAction.NextPhase && completeTarget == skill.MaxPhaseCount))
            skill.EndSkill();
        else
            skill.ChangePhase(completeTarget);
    }

    private bool Validate(ActiveSkill skill, int source, int target, int completeTarget)
    {
        bool valid = repeatCount > 0 && skill.IsValidPhaseIndex(target) &&
            Enum.IsDefined(typeof(PhaseLoopTarget), loopTarget) &&
            Enum.IsDefined(typeof(PhaseLoopCompleteAction), completeAction) &&
            (completeAction == PhaseLoopCompleteAction.EndSkill ||
             skill.IsValidPhaseIndex(completeTarget) ||
             (completeAction == PhaseLoopCompleteAction.NextPhase && completeTarget == skill.MaxPhaseCount));
        // Completing into the loop itself starts an unbounded cycle after the counter resets.
        valid &= completeAction == PhaseLoopCompleteAction.EndSkill ||
            (completeTarget != target && completeTarget != source);
        valid &= !skill.IsInstantPhase(target) && triggerTime != SkillTriggerTime.OnCastingStart;
        valid &= !(repeatCount > 1 && (triggerTime == SkillTriggerTime.OnExecute ||
            (triggerTime == SkillTriggerTime.OnPhaseTime &&
             (triggerDelay <= 0f || float.IsNaN(triggerDelay) || float.IsInfinity(triggerDelay))) ||
            skill.IsInstantPhase(target)));
        if (!valid) Debug.LogError($"[{skill.Name}] Invalid PhaseLoop: count={repeatCount}, target={target}, completion={completeTarget}. Use a timed or animation trigger and an exit outside the loop.");
        return valid;
    }
}
