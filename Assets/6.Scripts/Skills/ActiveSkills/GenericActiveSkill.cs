using System;
using System.Collections.Generic;
using UnityEngine;

public class GenericActiveSkill : ActiveSkill
{
    private readonly Dictionary<int, Dictionary<SkillTriggerTime, List<SkillModule>>> phaseModuleCache = new();
    private readonly Dictionary<int, List<SkillModule>> phaseModules = new();
    private readonly HashSet<SkillModule> executedModules = new();
    private readonly Dictionary<SkillModule, float> nextMovementTimes = new();

    public GenericActiveSkill(SO_SkillData skillData) : base(skillData) { }

    public override void SetOwner(GameObject gameObject)
    {
        base.SetOwner(gameObject);
        phaseModuleCache.Clear();
        phaseModules.Clear();
        if (phaseList == null) return;
        for (int i = 0; i < phaseList.Count; i++) CacheModules(i, phaseList[i]);
    }

    private void CacheModules(int index, PhaseSkill phase)
    {
        var timingCache = new Dictionary<SkillTriggerTime, List<SkillModule>>();
        var modules = new List<SkillModule>();
        foreach (SkillTriggerTime timing in Enum.GetValues(typeof(SkillTriggerTime)))
            timingCache[timing] = new List<SkillModule>();
        if (phase?.modules != null)
        {
            for (int i = 0; i < phase.modules.Count; i++)
            {
                var module = phase.modules[i]?.Clone();
                if (module == null) continue;
                module.RuntimeKey = $"{index}:{i}";
                module.Init(ownerCharacter);
                modules.Add(module);
                timingCache[module.triggerTime].Add(module);
            }
        }
        phaseModules[index] = modules;
        phaseModuleCache[index] = timingCache;
    }

    protected void NotifyModules(int index, SkillTriggerTime timing)
    {
        if (!IsActive || !phaseModuleCache.TryGetValue(index, out var cache) ||
            !cache.TryGetValue(timing, out var modules)) return;
        int version = PhaseVersion;
        foreach (var module in modules)
        {
            if (!IsActive || PhaseVersion != version || HasPendingPhaseChange) break;
            if (IsEnding && timing != SkillTriggerTime.OnEndDoAction) break;
            bool once = module.executeOncePerPhase || timing == SkillTriggerTime.OnPhaseTime ||
                timing == SkillTriggerTime.OnEndDoAction || module is Module_PhaseLoop;
            if (timing == SkillTriggerTime.OnPhaseTime && PhaseElapsedTime < module.GetTriggerDelay(this))
                continue;
            if (once && !executedModules.Add(module)) continue;
            module.OnNotify(ownerCharacter, this, phaseSkill);
        }
        var loop = Runtime.ActivePhaseLoop;
        if (loop != null && loop.triggerTime == timing && IsCurrentPhase(version) && !HasPendingPhaseChange &&
            Runtime.PhaseLoopTargetIndex == phaseIndex &&
            (timing != SkillTriggerTime.OnPhaseTime || PhaseElapsedTime >= loop.GetTriggerDelay(this)))
            loop.OnNotify(ownerCharacter, this, phaseSkill);
    }

    public override void NotifyMovement(SkillTriggerTime timing, float elapsed = 0f)
    {
        int version = PhaseVersion;
        if (!IsCurrentPhase(version) || HasPendingPhaseChange) return;
        if (timing == SkillTriggerTime.OnMovementStart || timing == SkillTriggerTime.OnMovementEnd)
        {
            NotifyModules(phaseIndex, timing);
            return;
        }
        if (timing != SkillTriggerTime.OnMovementProgress ||
            !phaseModuleCache.TryGetValue(phaseIndex, out var cache)) return;
        foreach (var module in cache[timing])
        {
            if (!IsCurrentPhase(version) || HasPendingPhaseChange) break;
            if (nextMovementTimes.TryGetValue(module, out float next) && elapsed < next) continue;
            if (module.executeOncePerPhase && !executedModules.Add(module)) continue;
            float interval = module.movementRepeatInterval;
            if (float.IsNaN(interval) || float.IsInfinity(interval)) interval = 0.1f;
            nextMovementTimes[module] = elapsed + Mathf.Max(0.01f, interval);
            module.OnNotify(ownerCharacter, this, phaseSkill);
        }
    }

    protected override void PrepareCasting()
    {
        executedModules.Clear();
        NotifyModules(phaseIndex, SkillTriggerTime.OnCastingStart);
    }

    protected override void ApplyEffects() { }

    protected override void OnPhaseEntered()
    {
        executedModules.Clear();
        nextMovementTimes.Clear();
        int version = PhaseVersion;
        if (phaseModules.TryGetValue(phaseIndex, out var modules))
            foreach (var module in modules)
            {
                if (!IsCurrentPhase(version) || HasPendingPhaseChange) break;
                module.OnPhaseEnter(ownerCharacter, this, phaseSkill);
            }
    }

    protected override void OnPhaseExited()
    {
        if (phaseModules.TryGetValue(phaseIndex, out var modules))
            foreach (var module in modules) module.OnPhaseExit(ownerCharacter, this, phaseSkill);
    }

    protected override void OnSkillEnding()
    {
        NotifyModules(phaseIndex, SkillTriggerTime.OnEndDoAction);
        executedModules.Clear();
        nextMovementTimes.Clear();
    }

    public override void Update(float deltaTime)
    {
        int version = PhaseVersion;
        base.Update(deltaTime);
        if (!IsCurrentPhase(version)) return;
        NotifyModules(phaseIndex, SkillTriggerTime.OnPhaseTime);
        if (!IsCurrentPhase(version) || !phaseModules.TryGetValue(phaseIndex, out var modules)) return;
        foreach (var module in modules)
        {
            if (!IsCurrentPhase(version)) break;
            module.Update(ownerCharacter, this, phaseSkill, deltaTime);
        }
    }

    public override void FixedUpdate(float fixedDeltaTime)
    {
        int version = PhaseVersion;
        if (!IsCurrentPhase(version) || !phaseModules.TryGetValue(phaseIndex, out var modules)) return;
        foreach (var module in modules)
        {
            if (!IsCurrentPhase(version)) break;
            module.FixedUpdate(ownerCharacter, this, phaseSkill, fixedDeltaTime);
        }
    }

    public override void Begin_JudgeAttack(AnimationEvent e)
    {
        if (!IsActive || !IsPhaseRunning || IsEnding) return;
        base.Begin_JudgeAttack(e);
        phaseSkill?.BeginJudgeAttack(ownerCharacter, this);
        NotifyModules(phaseIndex, SkillTriggerTime.OnJudgeAttack);
    }

    public override void End_JudgeAttack(AnimationEvent e)
    {
        if (!IsActive || !IsPhaseRunning || IsEnding) return;
        base.End_JudgeAttack(e);
        phaseSkill?.EndJudgeAttack(ownerCharacter, this);
        NotifyModules(phaseIndex, SkillTriggerTime.OnEndJudgeAttack);
    }

    public override void Begin_DoAction() => NotifyModules(phaseIndex, SkillTriggerTime.OnBeginDoAction);
    public override void Play_Sound() => NotifyModules(phaseIndex, SkillTriggerTime.OnSoundEvent);
    public override void Play_CameraShake() => NotifyModules(phaseIndex, SkillTriggerTime.OnCameraShake);

    public override void End_DoAction()
    {
        if (!IsActive || IsEnding || isCasting) return;
        int version = PhaseVersion;
        NotifyModules(phaseIndex, SkillTriggerTime.OnEndDoAction);
        if (PhaseVersion == version && !HasPendingPhaseChange) base.End_DoAction();
    }

    protected override void ExecutePhase(int index)
    {
        int version = PhaseVersion;
        NotifyModules(index, SkillTriggerTime.OnExecute);
        if (!IsCurrentPhase(version) || HasPendingPhaseChange) return;
        NotifyModules(index, SkillTriggerTime.OnPhaseTime);
        if (IsCurrentPhase(version) && !HasPendingPhaseChange && phaseSkill.isInstant)
            EndPhaseAndNext();
    }
}
