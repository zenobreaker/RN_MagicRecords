#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Run from Tools/Skill/Run Phase Loop Regression, or Unity -executeMethod SkillPhaseLoopRegression.RunBatch.
public static class SkillPhaseLoopRegression
{
    private sealed class ProbeState
    {
        public int Shots, Events, Updates, Exits;
        public readonly List<CancellationToken> Tokens = new();
        public readonly List<float> Times = new();
        public SkillRuntimeContext FirstRuntime;
        public bool SameRuntime = true;
    }

    [Serializable]
    private sealed class Probe : SkillModule
    {
        public ProbeState state;
        public bool attackEvent;
        private bool initialized;
        public override void Init(Character owner) => initialized = true;
        public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phase)
        {
            if (attackEvent) { state.Events++; return; }
            state.Shots++;
            state.Tokens.Add(skill.PhaseToken);
            state.Times.Add(skill.PhaseElapsedTime);
            state.FirstRuntime ??= skill.Runtime;
            state.SameRuntime &= ReferenceEquals(state.FirstRuntime, skill.Runtime);
        }
        public override void Update(Character owner, ActiveSkill skill, PhaseSkill phase, float dt)
        {
            Check(initialized, "Update uses initialized runtime clone, not asset prototype");
            state.Updates++;
        }
        public override void OnPhaseExit(Character owner, ActiveSkill skill, PhaseSkill phase) => state.Exits++;
    }

    private sealed class Fixture : IDisposable
    {
        public readonly GameObject Owner;
        public readonly SO_ActiveSkillData Data;
        public readonly GenericActiveSkill Skill;
        public Fixture(params PhaseSkill[] phases)
        {
            Owner = new GameObject("PhaseLoop regression owner");
            Owner.SetActive(false);
            Owner.AddComponent<Character>();
            Data = ScriptableObject.CreateInstance<SO_ActiveSkillData>();
            Data.skillName = "phase_loop_regression";
            Data.levelDatas = new List<SkillLevelData> { new SkillLevelData { damageData = new DamageData { hitData = new HitData() }, cooldown = 0, castingTime = 0, spawnCount = 1 } };
            Data.phaseList = new List<PhaseSkill>(phases);
            Skill = new GenericActiveSkill(Data);
            Skill.SetOwner(Owner);
            Skill.Cast();
        }
        public void Dispose()
        {
            Skill.EndSkill(false);
            UnityEngine.Object.DestroyImmediate(Owner);
            UnityEngine.Object.DestroyImmediate(Data);
        }
    }

    private static PhaseSkill Phase(params SkillModule[] modules) => new PhaseSkill { modules = new List<SkillModule>(modules) };
    private static Probe Shot(ProbeState state) => new Probe { state = state, triggerTime = SkillTriggerTime.OnExecute };
    private static Module_PhaseLoop Loop(int count = 10) => new Module_PhaseLoop
    {
        triggerTime = SkillTriggerTime.OnPhaseTime, triggerDelay = 0.1f,
        repeatCount = count, completeAction = PhaseLoopCompleteAction.EndSkill
    };
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("PhaseLoop regression: " + message);
    }

    [MenuItem("Tools/Skill/Run Phase Loop Regression")]
    public static void Run()
    {
        CurrentPhaseAndRecast();
        SpecificPhase();
        IsolationAndCancellation();
        CompletionActions();
        InvalidSettings();
        RuntimeModifiers();
        DelayedTransitionCancellation();
        AnimationReplay();
        CharacterEndBridge();
        Debug.Log("PHASE_LOOP_REGRESSION_PASS: 9 groups (counts, recast, runtime preservation, trigger reset, isolation, phase cancellation, completion, validation, modifiers, delayed transition, character/weapon animation replay).");
    }

    public static void RunBatch()
    {
        try { Run(); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    private static void CurrentPhaseAndRecast()
    {
        var state = new ProbeState();
        var eventProbe = new Probe { state = state, attackEvent = true, triggerTime = SkillTriggerTime.OnJudgeAttack, executeOncePerPhase = true };
        using var f = new Fixture(Phase(Shot(state), eventProbe, Loop()));
        var runtime = f.Skill.Runtime;
        runtime.Combat.BonusMultipiler = 2.5f;
        f.Skill.Update(0.01f);
        Check(state.Updates > 0, "runtime module Update");
        for (int i = 0; i < 10; i++)
        {
            f.Skill.Begin_JudgeAttack(null);
            f.Skill.Begin_JudgeAttack(null);
            f.Skill.Update(0.1f);
        }
        Check(state.Shots == 10 && state.Events == 10, "exactly 10 shots/events, once per generation");
        Check(state.SameRuntime && runtime.Combat.BonusMultipiler == 2.5f, "Runtime survives restart");
        Check(state.Times.TrueForAll(t => t == 0), "phase timer resets before OnExecute");
        Check(state.Tokens.TrueForAll(t => t.IsCancellationRequested), "all old phase tasks canceled");
        Check(!f.Skill.IsActive && runtime.GetPhaseLoopCount("0:2") == 0, "End clears counters");
        f.Skill.Update_Cooldown(100);
        f.Skill.Cast();
        Check(f.Skill.PhaseIndex == 0 && state.Shots == 11 && !ReferenceEquals(runtime, f.Skill.Runtime), "recast starts fresh");
        f.Skill.End_DoAction();
        Check(f.Skill.IsActive, "animation end does not terminate loop");
    }

    private static void SpecificPhase()
    {
        var state = new ProbeState();
        var end = new ProbeState();
        var loop = Loop(3);
        loop.loopTarget = PhaseLoopTarget.SpecificPhase;
        loop.targetPhaseIndex = 1;
        loop.completeAction = PhaseLoopCompleteAction.SpecificPhase;
        loop.completePhaseIndex = 2;
        using var f = new Fixture(Phase(loop), Phase(Shot(state)), Phase(Shot(end)));
        for (int i = 0; i < 4; i++) f.Skill.Update(0.1f);
        Check(state.Shots == 3 && end.Shots == 1 && f.Skill.PhaseIndex == 2, "specific target repeats without a second loop module");
    }

    private static void IsolationAndCancellation()
    {
        var a = new ProbeState(); var b = new ProbeState();
        using var first = new Fixture(Phase(Shot(a), Loop()));
        using var second = new Fixture(Phase(Shot(b), Loop()));
        first.Skill.Update(0.1f);
        Check(first.Skill.Runtime.GetPhaseLoopCount("0:1") == 1 && second.Skill.Runtime.GetPhaseLoopCount("0:1") == 0, "owner isolation");
        var old = first.Skill.PhaseToken;
        var lifetime = first.Skill.SkillToken;
        first.Skill.RestartCurrentPhase();
        Check(old.IsCancellationRequested && !lifetime.IsCancellationRequested, "phase and skill tokens differ");
        first.Skill.EndSkill(false);
        int shots = a.Shots;
        first.Skill.Update(1f);
        Check(!first.Skill.RestartCurrentPhase() && !first.Skill.ChangePhase(0) && a.Shots == shots, "no transitions after end");
        Check(lifetime.IsCancellationRequested, "skill token canceled on end");
    }

    private static void CompletionActions()
    {
        var state = new ProbeState(); var end = new ProbeState();
        var loop = Loop(1); loop.completeAction = PhaseLoopCompleteAction.NextPhase;
        using var f = new Fixture(Phase(Shot(state), loop), Phase(Shot(end)));
        f.Skill.Update(0.1f);
        Check(state.Shots == 1 && end.Shots == 1 && f.Skill.PhaseIndex == 1, "repeatCount 1 and NextPhase");
        f.Skill.Runtime.IncrementPhaseLoopCount("independent");
        f.Skill.Runtime.ResetPhaseLoopCount("independent");
        Check(f.Skill.Runtime.GetPhaseLoopCount("independent") == 0, "key reset");
    }

    private static void InvalidSettings()
    {
        Loop().OnNotify(null, null, null);
        foreach (int count in new[] { 0, -1 })
        {
            using var f = new Fixture(Phase(Loop(count)));
            f.Skill.Update(0.1f);
            Check(!f.Skill.IsActive, "non-positive repeat rejected");
        }
        var invalid = Loop(); invalid.loopTarget = PhaseLoopTarget.SpecificPhase; invalid.targetPhaseIndex = 99;
        using (var f = new Fixture(Phase(invalid))) { f.Skill.Update(0.1f); Check(!f.Skill.IsActive, "invalid phase rejected"); }
        var cycle = Loop(); cycle.completeAction = PhaseLoopCompleteAction.SpecificPhase; cycle.completePhaseIndex = 0;
        using (var f = new Fixture(Phase(cycle))) { f.Skill.Update(0.1f); Check(!f.Skill.IsActive, "self completion rejected"); }
        var immediate = Loop(); immediate.triggerDelay = 0;
        using (var f = new Fixture(Phase(immediate))) Check(!f.Skill.IsActive, "zero-delay loop rejected");
    }

    private static void RuntimeModifiers()
    {
        var state = new ProbeState(); var loop = Loop(5);
        loop.useRuntimeTotalShots = true; loop.useFireIntervalMultiplier = true;
        using var f = new Fixture(Phase(Shot(state), loop));
        f.Skill.Runtime.Combat.TotalShotsBonus = -2;
        f.Skill.Runtime.Combat.FireIntervalMultiplier = 0.5f;
        for (int i = 0; i < 3; i++) f.Skill.Update(0.05f);
        Check(state.Shots == 3 && !f.Skill.IsActive, "count/interval passives survive loop");
    }

    private static void DelayedTransitionCancellation()
    {
        var transition = new Module_PhaseTransition { triggerTime = SkillTriggerTime.OnExecute, delayTime = 100f };
        using var f = new Fixture(Phase(transition), Phase());
        var token = f.Skill.PhaseToken;
        f.Skill.RestartCurrentPhase();
        Check(token.IsCancellationRequested && f.Skill.PhaseIndex == 0, "old transition delay canceled on restart");
        Check(!f.Skill.ChangePhase(-1) && !f.Skill.ChangePhase(99), "index boundaries");
    }

    private static void CharacterEndBridge()
    {
        var shots = new ProbeState();
        using var f = new Fixture(Phase(Shot(shots), Loop(2)));
        f.Skill.EndSkill(false);
        var component = f.Owner.AddComponent<SkillComponent>();
        typeof(SkillComponent).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
        component.useAnimationEvents = true;
        component.SetActiveSkill("SLOT1", f.Skill);
        var character = f.Owner.GetComponent<Character>();
        int endEvents = 0;
        character.OnEndDoAction += () => endEvents++;
        component.UseSkill("SLOT1");
        character.End_DoAction();
        Check(component.InAction && f.Skill.IsActive && endEvents == 0, "animation end keeps input lock during loop");
        f.Skill.Update(0.1f); f.Skill.Update(0.1f);
        Check(!component.InAction && !f.Skill.IsActive && endEvents == 1, "completion releases action and notifies character once");
        component.UseSkill("SLOT1");
        var oldToken = f.Skill.PhaseToken;
        component.CancelCurrentSkill();
        Check(!component.InAction && !f.Skill.IsActive && endEvents == 2 && oldToken.IsCancellationRequested, "explicit cancellation completes character once");
    }

    private static void AnimationReplay()
    {
        var go = new GameObject("PhaseLoop animation regression");
        var controller = new AnimatorController();
        var clip = new AnimationClip();
        try
        {
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 1));
            controller.AddLayer("Base Layer");
            controller.AddParameter("ActionSpeed", AnimatorControllerParameterType.Float);
            var state = controller.layers[0].stateMachine.AddState("Shot");
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var visual = go.AddComponent<CharacterVisual>();
            typeof(CharacterVisual).GetField("<Animator>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(visual, animator);
            var action = new ActionData();
            typeof(ActionData).GetField("stateName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(action, "Base Layer");
            typeof(ActionData).GetField("subStateName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(action, "Shot");
            typeof(ActionData).GetField("weaponActionName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(action, "Base Layer.Shot");
            action.Initialize();
            animator.Rebind(); animator.Update(0f);
            animator.Play(action.StateName, 0, 0.5f); animator.Update(0f);
            Check(animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.1f, "animation starts at a nonzero offset");
            visual.PlayActionAnimation(action, 0, 1f, true); animator.Update(0);
            Check(animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.01f, "character restarts at zero");
            animator.Play(action.StateName, 0, 0.5f); animator.Update(0f);
            var weapon = go.AddComponent<WeaponController>();
            typeof(WeaponController).GetField("weaponAnimator", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(weapon, animator);
            weapon.DoAction(action, true); animator.Update(0);
            Check(animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.01f, "weapon restarts at zero");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(controller);
            UnityEngine.Object.DestroyImmediate(clip);
        }
    }
}
#endif
