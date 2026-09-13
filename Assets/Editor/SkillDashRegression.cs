#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Only the test character omits scene-wide hit-stop/slow manager registration.
public sealed class DashRegressionCharacter : Character
{
    protected override void Start() { }
}

[InitializeOnLoad]
public static class SkillDashRegression
{
    private const string Pending = "SkillDashRegression.Pending";
    private static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static SkillDashRegression()
    {
        EditorApplication.playModeStateChanged += change =>
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                RunTests().Forget();
            }
        };
    }
    [MenuItem("Tools/Skill/Run Dash Regression (Play Mode)")]
    public static void RunBatch()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Run from Edit Mode.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        ValidateAssets();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
    public static void ValidateAssets()
    {
        var dash = AssetDatabase.LoadAssetAtPath<SO_ActiveSkillData>("Assets/10.ScriptableObjects/Skills/Shooter/dash.asset");
        var rush = AssetDatabase.LoadAssetAtPath<SO_ActiveSkillData>("Assets/10.ScriptableObjects/Skills/MonsterSkills/breakrush.asset");
        Check(dash != null && dash.levelDatas.Count == 1 && dash.phaseList.Count == 1, "dash asset deserialization");
        Check(dash.phaseList[0].modules.Count == 4 && dash.phaseList[0].modules[0] is Module_Dash,
            "actual Dash asset uses existing modules");
        var move = (Module_Dash)dash.phaseList[0].modules[0];
        Check(move.directionType == Module_Dash.DashDirectionType.InputDirection && move.advancePhaseOnFinish &&
            move.distance == 5 && move.duration == 0.2f, "actual player movement configuration");
        Check(rush != null && rush.phaseList[1].modules[0] is Module_Dash legacy &&
            legacy.useTargetPosition && legacy.directionType == Module_Dash.DashDirectionType.Legacy &&
            legacy.bIsMoveOverTime && legacy.bIsGhostMode && legacy.duration == 2 && legacy.advancePhaseOnFinish,
            "BreakRush preserves legacy movement and waits for completion");
        Debug.Log("DASH_ASSETS_PASS: player and BreakRush deserialize with preserved module types and settings");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Dash regression: " + message);
    }
    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, Hidden).SetValue(target, value);
    private static async UniTask Frames(int count)
    {
        for (int i = 0; i < count; i++) await UniTask.WaitForFixedUpdate();
    }
    private static void InputSubscriptions()
    {
        var root = new GameObject("Input subscription regression"); root.SetActive(false);
        var player = root.AddComponent<Player>();
        var map = new InputActionMap("Player");
        map.AddAction("Dash", InputActionType.Button, "<Keyboard>/f12");
        var originalSettings = InputSystem.settings;
        var originalFocus = originalSettings.editorInputBehaviorInPlayMode;
        var originalBackground = originalSettings.backgroundBehavior;
        originalSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        originalSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            int requests = 0;
            Set(player, "playerActionMap", map);
            Set(player, "onDash", new Action<InputAction.CallbackContext>(_ => requests++));
            var bind = typeof(Player).GetMethod("SetInputSubscriptions", Hidden);
            void Subscribe(bool enabled) => bind.Invoke(player, new object[] { enabled });
            void Press()
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F12)); InputSystem.Update();
            }
            map.Enable();
            Subscribe(true); Subscribe(true); Press();
            Check(requests == 1, $"input subscription avoids duplicate callbacks: {requests}");
            Subscribe(false); Press(); Check(requests == 1, "disabled input unsubscribes");
            Subscribe(true); Press(); Check(requests == 2, "input reconnects on enable");
            Subscribe(false);
        }
        finally
        {
            map.Dispose(); InputSystem.RemoveDevice(keyboard); UnityEngine.Object.DestroyImmediate(root);
            originalSettings.editorInputBehaviorInPlayMode = originalFocus;
            originalSettings.backgroundBehavior = originalBackground;
        }
    }
    [Serializable]
    private sealed class Probe : SkillModule
    {
        public Action action;
        public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phase) => action();
    }
    private sealed class Fixture : IDisposable
    {
        public GameObject Owner;
        public Character Character;
        public MovementComponent Movement;
        public StateComponent State;
        public SkillComponent Skills;
        public ComboComponent Combo;
        public Rigidbody Body;
        public Collider Collider;
        public SO_ActiveSkillData Data;
        public SO_Movement MovementData;
        public GenericActiveSkill Skill;
        public Module_Dash Dash;
        public Animator Animator;
        public AnimatorController Controller;
        public GameObject Effect;
        public int Starts, Ends, Progress;
        public Fixture()
        {
            Owner = new GameObject("Dash regression owner"); Owner.SetActive(false);
            Character = Owner.AddComponent<DashRegressionCharacter>();
            Owner.AddComponent<StatusEffectComponent>();
            State = Owner.AddComponent<StateComponent>();
            Body = Owner.GetComponent<Rigidbody>(); Body.useGravity = false;
            Body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            Body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Collider = Owner.AddComponent<CapsuleCollider>();
            var visual = Owner.AddComponent<CharacterVisual>();
            Animator = Owner.GetComponent<Animator>();
            Controller = new AnimatorController(); Controller.AddLayer("Base Layer");
            Controller.AddParameter("Dash", AnimatorControllerParameterType.Trigger);
            Controller.AddParameter("Evade", AnimatorControllerParameterType.Trigger);
            Controller.AddParameter("SpeedY", AnimatorControllerParameterType.Float);
            var sm = Controller.layers[0].stateMachine;
            sm.defaultState = sm.AddState("Idle");
            foreach (string name in new[] { "Dash", "Evade" })
            {
                var transition = sm.AddAnyStateTransition(sm.AddState(name));
                transition.hasExitTime = false; transition.duration = 0;
                transition.AddCondition(AnimatorConditionMode.If, 0, name);
            }
            Animator.runtimeAnimatorController = Controller;
            MovementData = ScriptableObject.CreateInstance<SO_Movement>();
            Movement = Owner.AddComponent<MovementComponent>();
            Set(Movement, "SO_Movement", MovementData); Set(Movement, "characterLayer", (LayerMask)(1 << 0));
            Skills = Owner.AddComponent<SkillComponent>();
            Combo = Owner.AddComponent<ComboComponent>();
            Owner.SetActive(true);
            Effect = new GameObject("Dash regression effect"); Effect.SetActive(false);
            Dash = new Module_Dash { directionType = Module_Dash.DashDirectionType.InputDirection,
                distance = 5f, duration = 0.2f, speedCurve = null, advancePhaseOnFinish = true };
            Data = ScriptableObject.CreateInstance<SO_ActiveSkillData>();
            Data.skillName = "Dash regression";
            Data.levelDatas = new List<SkillLevelData> { new SkillLevelData {
                cooldown = 0.5f, castingTime = -1f, damageData = new DamageData { hitData = new HitData() } } };
            Data.phaseList = new List<PhaseSkill> { new PhaseSkill { modules = new List<SkillModule> {
                Dash,
                new Module_Sound { triggerTime = SkillTriggerTime.OnMovementStart, soundName = "DashRegressionSound" },
                new Module_PlayAnimation { triggerTime = SkillTriggerTime.OnMovementStart, useMovementAnimation = true },
                new Module_StartWeaponVFX { triggerTime = SkillTriggerTime.OnMovementStart, spawnAtOwner = true, effectPrefab = Effect },
                new Probe { triggerTime = SkillTriggerTime.OnMovementStart, action = () => Starts++ },
                new Probe { triggerTime = SkillTriggerTime.OnMovementProgress, movementRepeatInterval = 0.05f, action = () => Progress++ },
                new Probe { triggerTime = SkillTriggerTime.OnMovementEnd, action = () => Ends++ }
            } } };
            Skill = (GenericActiveSkill)Data.CreateSkill();
            Skills.SetActiveSkill(SkillSlot.SubAction, Skill);
        }
        public void Ready()
        {
            Skill.EndSkill(); Skill.Update_Cooldown(100f); State.SetIdleMode();
            Movement.SetDirection(Vector2.zero); Owner.transform.position = Vector3.zero;
            Body.position = Vector3.zero;
            Body.linearVelocity = Vector3.zero; Owner.transform.rotation = Quaternion.identity;
        }
        public void Dispose()
        {
            if (Owner != null) { Skill.EndSkill(); UnityEngine.Object.DestroyImmediate(Owner); }
            UnityEngine.Object.DestroyImmediate(Effect);
            UnityEngine.Object.DestroyImmediate(Data);
            UnityEngine.Object.DestroyImmediate(MovementData);
            UnityEngine.Object.DestroyImmediate(Controller);
        }
    }
    private static async UniTaskVoid RunTests()
    {
        GameObject soundObject = null;
        AudioClip soundClip = null;
        try
        {
            soundObject = new GameObject("Dash regression sound"); soundObject.SetActive(false);
            soundObject.AddComponent<AudioListener>();
            var audio = soundObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            var sounds = soundObject.AddComponent<SoundManager>();
            soundClip = AudioClip.Create("Dash regression clip", 44100, 1, 44100, false);
            Set(sounds, "sfxSounds", new[] { new Sound { soundName = "DashRegressionSound", clip = soundClip } });
            Set(sounds, "bgmSounds", Array.Empty<Sound>());
            sounds.sfxPlayers = new[] { audio };
            soundObject.SetActive(true);
            await UniTask.Yield();
            Check(SoundManager.Instance == sounds, "test SoundManager initialized");
            audio.Stop();
            InputSubscriptions();
            using (var f = new Fixture())
            {
                Check((int)SkillSlot.SubAction == 77 && (int)SkillSlot.SLOT1 == 100 &&
                    (int)SkillSlot.SLOT4 == 103 && (int)SkillSlot.MAX == 104, "serialized slot values");
                Check(f.Skills.TryGetRegisteredActiveSkill(SkillSlot.SubAction, out var registered) &&
                    ReferenceEquals(registered, f.Skill), "SubAction registration/query");
                f.Movement.SetDirection(Vector2.right);
                f.Combo.InputQueue(InputCommandType.DASH);
                Check(f.Skill.IsActive && f.Skills.InAction && f.State.EvadeMode && f.Starts == 1, "Combo -> skill -> movement");
                Check(audio.clip == soundClip, "sound module reaches AudioSource");
                f.Animator.Update(0);
                Check(f.Animator.GetCurrentAnimatorStateInfo(0).IsName("Dash"), "forward animation module");
                Check(f.Owner.transform.childCount == 1, "VFX module creates following effect");
                f.Combo.InputQueue(InputCommandType.DASH);
                f.Movement.Dash(Vector3.forward, 5, 0.2f);
                Check(f.Starts == 1, "duplicate requests rejected");
                await Frames(12);
                Check(!f.Skill.IsActive && !f.Skills.InAction && f.State.IdleMode && f.Ends == 1, "physical completion ends skill");
                Check(f.Body.position.x >= 4.9f && Math.Abs(f.Body.position.z) < 0.01f, "input direction distance");
                Check(f.Progress >= 2 && f.Progress <= 5, "movement progress interval");
                Check(f.Skill.IsOnCooldown, "cooldown remains after movement");
                f.Combo.InputQueue(InputCommandType.DASH); Check(f.Starts == 1, "cooldown rejects input");
                await UniTask.Yield(); Check(f.Owner.transform.childCount == 0, "VFX removed on skill end");
                f.Ready(); f.Combo.InputQueue(InputCommandType.DASH); f.Animator.Update(0);
                Check(f.Animator.GetCurrentAnimatorStateInfo(0).IsName("Evade"), "no-input backward animation");
                await Frames(12); Check(f.Body.position.z <= -4.9f, "no-input backward movement");
                f.Ready();
                f.Skill.Runtime.Spawn.TargetPosition = Vector3.right * 10;
                var legacy = new Module_Dash { useTargetPosition = true };
                Check(legacy.ResolveDirection(f.Character, f.Skill, f.Movement) == Vector3.right, "legacy target direction");
                legacy.useTargetPosition = false;
                Check(legacy.ResolveDirection(f.Character, f.Skill, f.Movement) == Vector3.forward, "legacy forward direction");
                f.Dash.directionType = Module_Dash.DashDirectionType.Backward;
                Check(f.Dash.ResolveDirection(f.Character, f.Skill, f.Movement) == Vector3.back, "explicit backward direction");
                f.Skill.Runtime.DashDistanceMultiplier = 2;
                f.Skill.EndSkill(); f.Skill.Update_Cooldown(100); f.Skill.Cast();
                Check(f.Skill.Runtime.DashDistanceMultiplier == 1, "new cast resets movement modifiers");
                f.Ready();
                // A character along the path is ignored, while environment collision remains enabled.
                using (var other = new Fixture())
                {
                    other.Owner.transform.position = new Vector3(0, 0, 2);
                    other.Body.isKinematic = true;
                    Physics.SyncTransforms();
                    f.Movement.SetDirection(Vector2.up); f.Combo.InputQueue(InputCommandType.DASH);
                    Check(Physics.GetIgnoreCollision(f.Collider, other.Collider), "character collision ignored");
                    var oldToken = f.Skill.PhaseToken;
                    f.Skill.RestartCurrentPhase();
                    Check(oldToken.IsCancellationRequested && f.Skill.IsActive && f.State.EvadeMode, "phase restart cancels old task before new movement");
                    f.Skill.EndSkill();
                    Check(!Physics.GetIgnoreCollision(f.Collider, other.Collider) && f.State.IdleMode, "immediate cancellation restores collision/state");
                    f.Ready(); f.Movement.SetDirection(Vector2.up); f.Combo.InputQueue(InputCommandType.DASH);
                    f.State.SetDamagedMode();
                    Check(!f.Skill.IsActive && f.State.DamagedMode && !Physics.GetIgnoreCollision(f.Collider, other.Collider), "damage cancels without restoring Idle");
                    foreach (bool dead in new[] { false, true })
                    {
                        f.Ready(); f.Movement.SetDirection(Vector2.up); f.Combo.InputQueue(InputCommandType.DASH);
                        if (dead) f.State.SetDeadMode(); else f.State.SetStopMode();
                        Check(!f.Skill.IsActive && !Physics.GetIgnoreCollision(f.Collider, other.Collider) &&
                            (dead ? f.State.DeadMode : f.State.StopMode), "stop/death cancels without overwriting state");
                    }
                    f.Ready(); f.Movement.SetDirection(Vector2.up); f.Combo.InputQueue(InputCommandType.DASH);
                    f.Owner.SetActive(false);
                    Check(!f.Skill.IsActive && !Physics.GetIgnoreCollision(f.Collider, other.Collider) && !f.State.EvadeMode, "disable cleanup");
                    f.Owner.SetActive(true); f.Ready();
                    f.Movement.SetDirection(Vector2.up); f.Combo.InputQueue(InputCommandType.DASH);
                    var collider = f.Collider;
                    UnityEngine.Object.Destroy(f.Owner); await UniTask.Yield(); await UniTask.Yield();
                    Check(!f.Skill.IsActive && f.Owner == null, "destroy cancels skill and movement");
                }
            }
            using (var f = new Fixture())
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = new Vector3(0, 0, 2);
                wall.transform.localScale = new Vector3(10, 5, 0.5f);
                Physics.SyncTransforms();
                f.Movement.SetDirection(Vector2.up); f.Combo.InputQueue(InputCommandType.DASH);
                Check(!Physics.GetIgnoreCollision(f.Collider, wall.GetComponent<Collider>()), "wall never ignored");
                await Frames(12); f.Movement.SetDirection(Vector2.zero);
                Check(f.Body.position.z < 1.5f, "wall blocks physical dash");
                UnityEngine.Object.DestroyImmediate(wall); f.Ready();
                bool completed = false;
                f.Movement.MoveOverTime(Vector3.right, 2f, 0.1f, onFinished: done => completed = done);
                await Frames(7);
                Check(completed && f.Body.position.x >= 1.99f, $"MoveOverTime completes: done={completed}, x={f.Body.position.x}");
                f.Ready(); f.Movement.SetDirection(Vector2.right);
                await Frames(5); Check(f.Body.position.x > 0, "walk resumes after external movement");
                float walkSpeed = f.Body.linearVelocity.x;
                f.Movement.SetDirection(Vector2.right, true);
                await Frames(5); Check(f.Body.linearVelocity.x > walkSpeed, "run resumes after external movement");
                f.Ready();
                bool rejected = false;
                f.Movement.Dash(Vector3.right, 5, float.NaN, onFinished: done => rejected = !done);
                Check(rejected && f.State.IdleMode, "invalid duration rejects without leaving Evade");
                var clone = f.MovementData.GetMovement();
                Check(clone.WalkSpeed == f.MovementData.WalkSpeed && clone.RunSpeed == f.MovementData.RunSpeed &&
                    clone.SprintSpeed == f.MovementData.SprintSpeed && clone.Ratio == f.MovementData.Ratio, "movement clone copies all values");
                UnityEngine.Object.DestroyImmediate(clone);
                Set(f.Combo, "skill", null); f.Combo.InputQueue(InputCommandType.DASH);
                Check(!f.Skill.IsActive, "missing SkillComponent is safe");
            }
            Debug.Log("DASH REGRESSION PASSED: slots, input, direction, animation, VFX, cooldown, physics, phase restart, cancellation, legacy and locomotion");
            if (Application.isBatchMode) EditorApplication.Exit(0); else EditorApplication.ExitPlaymode();
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            if (Application.isBatchMode) EditorApplication.Exit(1); else EditorApplication.ExitPlaymode();
        }
        finally
        {
            if (soundObject != null) UnityEngine.Object.DestroyImmediate(soundObject);
            if (soundClip != null) UnityEngine.Object.DestroyImmediate(soundClip);
        }
    }
}
#endif
