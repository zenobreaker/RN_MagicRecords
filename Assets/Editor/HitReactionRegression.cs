#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class HitRegressionCharacter : Character
{
    public int Deaths;
    protected override void Start() { }
    protected override void OnDamageDeath() => Deaths++;
}

// Exercise the real Player damage acceptance rule without input/scene manager dependencies.
public sealed class HitRegressionPlayer : Player
{
    public int Deaths;
    protected override void Awake() { }
    protected override void Start() { }
    protected override void OnDamageDeath() => Deaths++;
}

public sealed class HitRegressionEnemy : Enemy
{
    public int Deaths;
    protected override void Awake() { }
    protected override void Start() { }
    protected override void OnDisable() { }
    protected override void OnDamageDeath() => Deaths++;
}

public sealed class HitRegressionProjectile : BaseProjectile
{
    public void Hit(GameObject target) => DealDamage(target, target.transform.position);
}

[InitializeOnLoad]
public static class HitReactionRegression
{
    private const string Pending = "HitReactionRegression.Pending";
    private const string ScenesKey = "HitReactionRegression.Scenes";
    private const string Report = "Library/HitReactionRegression-result.txt";
    private static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int checks;
    private static readonly List<string> errors = new();
    [Serializable] private class SceneBackup { public SceneSetup[] scenes; }

    static HitReactionRegression()
    {
        EditorApplication.playModeStateChanged += change =>
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                RunTests().Forget();
            }
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                string backup = SessionState.GetString(ScenesKey, "");
                if (string.IsNullOrEmpty(backup)) return;
                SessionState.EraseString(ScenesKey);
                EditorSceneManager.RestoreSceneManagerSetup(JsonUtility.FromJson<SceneBackup>(backup).scenes);
            }
        };
    }

    [MenuItem("Tools/Combat/Run Hit Reaction Regression (Play Mode)")]
    public static void RunBatch()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Run from Edit Mode.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new Exception("Save modified scenes before running the Hit Reaction regression.");
        var backup = new SceneBackup { scenes = EditorSceneManager.GetSceneManagerSetup() };
        SessionState.SetString(ScenesKey, JsonUtility.ToJson(backup));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    private static void Check(bool condition, string message)
    {
        File.AppendAllText(Report, (condition ? "PASS " : "FAIL ") + message + "\n");
        if (!condition) throw new Exception(message);
        checks++;
    }
    private static void Set(object target, string field, object value, Type type = null) =>
        (type ?? target.GetType()).GetField(field, Hidden).SetValue(target, value);
    private static async UniTask Wait(float seconds)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(seconds));
        // UniTask's Update queue runs before MonoBehaviour.Update.
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
    }
    private static DamageEvent Hit(HitReactionType reaction, float damage = 10f) =>
        new DamageEvent(damage, hitData: new HitData { Reaction = reaction, Distance = 4f });
    private static void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            errors.Add(message);
    }

    [Serializable] private sealed class HoldPhase : SkillModule
    {
        public override bool ControlsPhaseLifecycle() => true;
        public override void OnNotify(Character owner, ActiveSkill skill, PhaseSkill phaseSkill) { }
    }

    private sealed class Fixture : IDisposable
    {
        public readonly GameObject Root;
        public readonly Character Character;
        public readonly StateComponent State;
        public readonly HealthPointComponent Health;
        public readonly DamageHandleComponent Damage;
        public readonly LaunchComponent Launch;
        public readonly Rigidbody Body;
        public readonly MovementComponent Movement;
        public readonly EffectComponent Effects;
        public readonly SkillComponent Skills;
        private readonly SO_Movement movementData;
        private readonly List<Object> owned = new();

        public Fixture(Type characterType = null)
        {
            Root = new GameObject("Hit reaction fixture"); Root.SetActive(false);
            Character = (Character)Root.AddComponent(characterType ?? typeof(HitRegressionCharacter));
            Root.AddComponent<StatusEffectComponent>();
            State = Root.AddComponent<StateComponent>();
            Health = Root.AddComponent<HealthPointComponent>();
            var status = Root.AddComponent<StatusComponent>();
            Set(status, "status", new Status { statusEntries = new List<StatusEntry>() });
            Damage = Root.AddComponent<DamageHandleComponent>();
            Launch = Root.AddComponent<LaunchComponent>();
            Body = Root.GetComponent<Rigidbody>(); Body.useGravity = false;
            Body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            var visual = Root.AddComponent<CharacterVisual>(); visual.useAnimationEvents = false;
            Skills = Root.AddComponent<SkillComponent>();
            Effects = Root.AddComponent<EffectComponent>();
            movementData = ScriptableObject.CreateInstance<SO_Movement>();
            Movement = Root.AddComponent<MovementComponent>(); Set(Movement, "SO_Movement", movementData);
            // Player/Enemy probes intentionally omit only scene/input initialization.
            foreach (var pair in new (string, object)[] { ("state", State), ("healthPoint", Health),
                ("status", status), ("visual", visual), ("rigidbody", Body), ("damageHandler", Damage) })
                Set(Character, pair.Item1, pair.Item2, typeof(Character));
            Root.SetActive(true);
            Health.InitCurrentHealth();
        }

        public GenericActiveSkill StartSkill(params SkillModule[] extra)
        {
            var data = ScriptableObject.CreateInstance<SO_ActiveSkillData>(); owned.Add(data);
            data.skillName = "Hit reaction regression";
            data.levelDatas = new List<SkillLevelData> { new SkillLevelData {
                cooldown = 0, castingTime = -1, damageData = new DamageData() } };
            var modules = new List<SkillModule> { new HoldPhase() }; modules.AddRange(extra);
            data.phaseList = new List<PhaseSkill> { new PhaseSkill { modules = modules } };
            var skill = (GenericActiveSkill)data.CreateSkill();
            Skills.SetActiveSkill(SkillSlot.SLOT1, skill);
            Skills.UseSkill(SkillSlot.SLOT1);
            if (State.IdleMode) State.SetActionMode();
            Check(skill.IsActive, "fixture skill started");
            return skill;
        }

        public void Dispose()
        {
            Skills.CancelCurrentSkill();
            Object.DestroyImmediate(Root);
            Object.DestroyImmediate(movementData);
            foreach (var item in owned) Object.DestroyImmediate(item);
        }
    }

    private static void ValidateAssets()
    {
        foreach (var entry in new[] { ("MeleeAttack", HitReactionType.Light),
            ("iceburst", HitReactionType.Heavy), ("breakrush", HitReactionType.Knockback) })
        {
            var data = AssetDatabase.LoadAssetAtPath<SO_ActiveSkillData>(
                "Assets/10.ScriptableObjects/Skills/MonsterSkills/" + entry.Item1 + ".asset");
            var modules = data.phaseList.SelectMany(p => p.modules).ToArray();
            var attack = modules.OfType<Module_SetDamageData>().FirstOrDefault()?.DamageData;
            attack ??= modules.OfType<Module_SpawnObject>()
                .FirstOrDefault(m => m.damageApplyType == DamageApplyType.Override)?.damageData;
            Check(attack != null && attack.hitData.Reaction == entry.Item2, entry.Item1 + " actual SO reaction");
            if (entry.Item2 == HitReactionType.Knockback) Check(attack.hitData.Distance > 0, "BreakRush knockback power");
        }
    }

    private static async UniTaskVoid RunTests()
    {
        GameObject cameraObject = null;
        bool previousBackground = Application.runInBackground;
        float previousTimeScale = Time.timeScale;
        float previousMaximumDelta = Time.maximumDeltaTime;
        Application.runInBackground = true;
        Time.timeScale = 1f;
        Time.maximumDeltaTime = .05f;
        checks = 0; errors.Clear();
        File.WriteAllText(Report, "Unity " + Application.unityVersion + " Play Mode hit reaction regression\n");
        Application.logMessageReceived += Log;
        try
        {
            cameraObject = new GameObject("Hit regression camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            ValidateAssets();
            using (var f = new Fixture())
            using (var attacker = new Fixture())
            {
                await UniTask.Yield();
                attacker.Root.transform.position = new Vector3(-4, 0, 0);
                var data = new DamageData { hitData = new HitData { Reaction = HitReactionType.Heavy } };
                var a = data.GetMyDamageEvent(attacker.Character.Status);
                var b = data.GetMyDamageEvent(attacker.Character.Status);
                a.hitData.Reaction = HitReactionType.None;
                Check(b.Reaction == HitReactionType.Heavy && data.hitData.Reaction == HitReactionType.Heavy,
                    "DamageEvent snapshots are independent of SO and other targets");
                Check(new DamageData { hitData = null }.Clone().hitData != null, "legacy null HitData clones safely");
                f.Character.OnDamage(attacker.Root, null, Vector3.zero, new DamageEvent(10));
                Check(f.Health.GetCurrentHP == 90 && f.State.IdleMode && !f.Launch.IsLaunching,
                    "A/G unset None applies damage without state change or knockback");

                var skill = f.StartSkill();
                f.Character.OnDamage(attacker.Root, null, Vector3.zero, Hit(HitReactionType.Light));
                Check(f.Health.GetCurrentHP == 80 && skill.IsActive && f.State.ActionMode &&
                    f.State.CurrentHitReaction == HitReactionType.Light, "B Light preserves active skill and action");
                await Wait(.12f);
                Check(f.State.CurrentHitReaction == HitReactionType.None && skill.IsActive, "Light uses short receiver timer");

                var bullet = new GameObject("Already fired bullet");
                var projectile = bullet.AddComponent<HitRegressionProjectile>();
                projectile.SetDamageInfo(f.Character, new DamageData { hitData = new HitData { Reaction = HitReactionType.Heavy } });
                var attached = new GameObject("Skill-owned effect"); skill.AddTrackedEffect(attached, true);
                var token = skill.SkillToken;
                f.Character.OnDamage(attacker.Root, null, Vector3.zero, Hit(HitReactionType.Heavy));
                f.Character.End_Damaged();
                Check(f.Health.GetCurrentHP == 70 && f.State.DamagedMode && !skill.IsActive && token.IsCancellationRequested,
                    "C Heavy cancels skill and ignores early animation completion");
                Check(!attached.activeSelf && bullet.activeSelf, "skill-owned effects stop; fired projectile survives");
                f.Movement.SetDirection(Vector2.right);
                await UniTask.WaitForFixedUpdate();
                Check(Mathf.Abs(f.Body.linearVelocity.x) < .001f, "Heavy blocks normal movement");
                f.Character.OnDamage(attacker.Root, null, Vector3.zero, Hit(HitReactionType.Heavy));
                Check(f.Health.GetCurrentHP == 60 && f.State.CurrentHitReaction == HitReactionType.Heavy,
                    "E repeated damage applies while reaction is protected");
                await Wait(.23f);
                Check(f.State.IdleMode && f.State.IsReactionImmune, "Heavy timer ends before recovery immunity");
                f.Character.OnDamage(attacker.Root, null, Vector3.zero, Hit(HitReactionType.Knockback));
                Check(f.Health.GetCurrentHP == 50 && !f.Launch.IsLaunching && f.State.IdleMode,
                    "F recovery immunity blocks only reaction");
                var nextSkill = f.StartSkill(); f.Character.End_Damaged();
                Check(nextSkill.IsActive && f.State.ActionMode, "late animation event cannot cancel next skill");
                f.Skills.CancelCurrentSkill();
                await Wait(.18f);

                f.Movement.SetDirection(Vector2.left);
                var position = f.Body.position;
                f.Character.OnDamage(attacker.Root, null, Vector3.zero, Hit(HitReactionType.Knockback));
                await UniTask.WaitForFixedUpdate(); await UniTask.WaitForFixedUpdate();
                Check(f.Health.GetCurrentHP == 40 && f.Launch.IsLaunching && f.Body.position.x > position.x &&
                    f.Body.linearVelocity.x > 0, "D Knockback moves away from attacker despite opposite input");
                await Wait(.2f);
                Check(!f.Launch.IsLaunching && f.State.IdleMode, "knockback restores state and movement ownership");

                projectile.Hit(attacker.Root);
                Check(attacker.Health.GetCurrentHP == 90 && attacker.State.DamagedMode,
                    "H surviving projectile -> CombatHelper -> IDamagable -> shared reaction");
                Object.DestroyImmediate(attached); Object.DestroyImmediate(bullet);
                f.Root.SetActive(false); f.Root.SetActive(true);
                Check(!f.State.IsReactionImmune && f.State.IdleMode && !f.Launch.IsLaunching, "pool reuse clears reaction state");
                f.Character.OnDamage(attacker.Root, null, Vector3.zero, Hit(HitReactionType.Knockback, 200));
                await Wait(.4f);
                Check(f.State.DeadMode && !f.Launch.IsLaunching && ((HitRegressionCharacter)f.Character).Deaths == 1,
                    "lethal damage bypasses reaction and cannot be restored to Idle");
                f.Damage.OnDamage(attacker.Root, Hit(HitReactionType.Heavy, 200));
                Check(((HitRegressionCharacter)f.Character).Deaths == 1, "dead target ignores later damage");
            }

            using (var f = new Fixture())
            {
                await UniTask.Yield();
                f.State.BaseReactionResistance = HitReactionResistance.Light;
                var first = new HitReactionResistanceEffect("first", "test", 0, HitReactionResistance.Heavy);
                var second = new HitReactionResistanceEffect("second", "test", .1f, HitReactionResistance.Heavy);
                f.Effects.ApplyEffect(first, f.Root, f.Root); f.Effects.ApplyEffect(second, f.Root, f.Root);
                f.Effects.RemoveEffect(first);
                f.Character.OnDamage(null, null, Vector3.left, Hit(HitReactionType.Heavy));
                Check(f.Health.GetCurrentHP == 90 && f.State.IdleMode &&
                    f.State.EffectiveReactionResistance == (HitReactionResistance.Light | HitReactionResistance.Heavy),
                    "base and independent buff grants combine; removing one preserves another");
                await Wait(.15f);
                Check(f.State.EffectiveReactionResistance == HitReactionResistance.Light, "buff expiry removes only its resistance");
                var skill = f.StartSkill(new Module_HitReactionResistance { resistance = HitReactionResistance.Heavy });
                Check((f.State.EffectiveReactionResistance & HitReactionResistance.Heavy) != 0, "skill phase grants resistance");
                f.Skills.CancelCurrentSkill();
                Check(f.State.EffectiveReactionResistance == HitReactionResistance.Light, "skill cancellation removes phase resistance");
                f.Root.GetComponent<StatusEffectComponent>().AddStunEffect();
                Check(f.State.StopMode, "existing immobilizing status enters Stop");
                f.Character.OnDamage(null, null, Vector3.zero, Hit(HitReactionType.Heavy));
                Check(f.State.StopMode && f.Health.GetCurrentHP == 80, "reaction cannot release existing crowd control");
                f.Root.GetComponent<StatusEffectComponent>().RemoveStunEffect();
                Check(f.State.IdleMode, "status removal restores Stop to Idle");
            }

            using (var f = new Fixture(typeof(HitRegressionEnemy)))
            {
                await UniTask.Yield();
                var enemy = (HitRegressionEnemy)f.Character; enemy.SetGrade(MonsterGrade.BOSS);
                f.Damage.OnDamage(null, Hit(HitReactionType.Heavy));
                Check(f.Health.GetCurrentHP == 90 && f.State.IdleMode, "boss legacy resistance preserves damage");
                enemy.SetGrade(MonsterGrade.ELITE);
                Check(!enemy.Boss, "changing grade clears stale boss flag");
                new ApplyDamageAction(DamageType.DOT_BURN, 200).Execute(f.Root, null, 1);
                Check(f.State.DeadMode && enemy.Deaths == 1, "enemy DOT uses common death path");
            }

            foreach (var dot in new[] { DamageType.DOT_BLEED, DamageType.DOT_BURN, DamageType.DOT_POISON, DamageType.DOT_HATERD })
            using (var f = new Fixture(typeof(HitRegressionPlayer)))
            {
                await UniTask.Yield();
                f.State.SetEvadeMode();
                f.Character.OnDamage(null, null, Vector3.zero, Hit(HitReactionType.Heavy));
                Check(f.Health.GetCurrentHP == 100, "player direct hit evaded: " + dot);
                var tick = Hit(HitReactionType.Heavy);
                tick.hitData.DamageType = dot;
                f.Damage.OnDamage(null, tick);
                Check(f.Health.GetCurrentHP == 90 && f.State.EvadeMode &&
                    f.State.CurrentHitReaction == HitReactionType.None, "nonlethal DOT never reacts: " + dot);
                new ApplyDamageAction(dot, 200).Execute(f.Root, null, 1);
                Check(f.State.DeadMode && ((HitRegressionPlayer)f.Character).Deaths == 1,
                    "DOT bypasses evade, no reaction, player death: " + dot);
            }
            Check(errors.Count == 0, "no Unity errors: " + string.Join(" | ", errors));
            File.AppendAllText(Report, $"PASSED {checks} checks\n");
            Debug.Log($"HIT_REACTION_REGRESSION_PASSED: {checks} checks");
        }
        catch (Exception error)
        {
            File.AppendAllText(Report, "FAILED " + error + "\n");
            Debug.LogException(error);
        }
        finally
        {
            if (cameraObject != null) Object.DestroyImmediate(cameraObject);
            Application.runInBackground = previousBackground;
            Time.timeScale = previousTimeScale;
            Time.maximumDeltaTime = previousMaximumDelta;
            Application.logMessageReceived -= Log;
            EditorApplication.ExitPlaymode();
        }
    }
}
#endif
