#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Behavior;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Explicit Edit Mode checks; no scene, save data, or serialized assets are changed.
[InitializeOnLoad]
public static class AICombatRegression
{
    private const string Request = "Library/AICombatRegression.request";
    private const string Report = "Library/AICombatRegression-result.txt";
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int checks;

    static AICombatRegression() => EditorApplication.update += Tick;

    private static void Tick()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        File.Delete(Request);
        try { Run(); }
        catch (Exception error) { File.AppendAllText(Report, "FAIL " + error + "\n"); Debug.LogException(error); }
    }

    private static void Check(bool condition, string message)
    {
        File.AppendAllText(Report, (condition ? "PASS " : "FAIL ") + message + "\n");
        if (!condition) throw new InvalidOperationException(message);
        checks++;
    }

    private static void SetAI(AIController ai, string name, object value) =>
        typeof(AIController).GetField(name, Hidden).SetValue(ai, value);

    private static List<PatternEntry> Entries(AIController ai) =>
        (List<PatternEntry>)typeof(AIController).GetField("patternEntries", Hidden).GetValue(ai);

    private static PatternEntry Copy(PatternEntry source) => new PatternEntry
    {
        slotName = source.slotName,
        defaultIsUse = source.defaultIsUse,
        skill = source.skill,
        conditions = new List<PatternCondition>(source.conditions)
    };

    [MenuItem("Tools/Combat/Check AI Pattern Recovery (Edit Mode)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run AI pattern checks in Edit Mode.");
        File.WriteAllText(Report, "AI pattern recovery regression\n");
        checks = 0;
        var owner = new GameObject("AI regression fixture");
        owner.SetActive(false);
        var target = new GameObject("AI regression target");
        target.SetActive(false);
        try
        {
            var context = new AIContext(owner) { Target = target };
            foreach (string name in new[] { "Memosquito", "TriLegDrone", "SignalDrone", "TurtleShell", "Wheeler", "DefenseDrone", "IceSculpture", "IceBreaker" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/5.Prefabs/Characters/" + name + ".prefab");
                Check(prefab != null, name + " prefab loads");
                foreach (var source in Entries(prefab.GetComponent<AIController>()))
                {
                    var entry = Copy(source);
                    entry.InitiaizePattern();
                    entry.Update(1000f, context);
                    string label = name + "/" + entry.slotName;
                    Check(entry.conditions.Where(c => c.type == PatternConditionType.Distance)
                        .All(c => c.ctype == ComparisonType.LessThanOrEqual), label + " uses a maximum attack range");
                    target.transform.position = Vector3.forward * (entry.GetAttackRange() + 5f);
                    Check(entry.IsReadyToUse(context) && !entry.CheckUsePattern(context), label + " can approach a ready attack from outside range");
                    target.transform.position = Vector3.forward * (entry.GetAttackRange() * 0.5f);
                    Check(entry.CheckUsePattern(context), label + " can attack inside range");
                    entry.SetruntimeIsUse(false);
                    Check(!entry.IsReadyToUse(context) && !entry.CheckUsePattern(context), label + " respects disabled patterns");
                }
            }

            owner.AddComponent<Character>();
            var state = owner.AddComponent<StateComponent>();
            var skills = owner.AddComponent<SkillComponent>();
            var ai = owner.AddComponent<AIController>();
            var perception = owner.GetComponent<PerceptionComponent>();
            typeof(PerceptionComponent).GetMethod("Awake", Hidden).Invoke(perception, null);
            typeof(AIController).GetMethod("Awake", Hidden).Invoke(ai, null);
            var drone = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/5.Prefabs/Characters/TriLegDrone.prefab");
            var patterns = Entries(drone.GetComponent<AIController>()).Select(Copy).ToList();
            SetAI(ai, "patternEntries", patterns);
            SetAI(ai, "combatStyle", AICombatStyle.HYBRID);
            typeof(AIController).GetMethod("Start", Hidden).Invoke(ai, null);
            SetAI(ai, "context", context);
            target.transform.position = Vector3.forward * 30f;
            foreach (var entry in patterns) entry.Update(1000f, context);
            Check(Mathf.Approximately(ai.GetEngagementRange(), 12f), "Hybrid approaches ready SpikeShot from outside its range");

            patterns[0].UsePattern();
            SetAI(ai, "currentPattern", patterns[0]);
            patterns[0].Update(2f, context);
            skills.OnEndDoAction?.Invoke();
            patterns[0].Update(4.9f, context);
            Check(!patterns[0].IsReadyToUse(context), "SpikeShot cannot repeat before its seven-second cooldown");
            patterns[0].Update(0.2f, context);
            Check(patterns[0].IsReadyToUse(context), "Attack completion preserves the cooldown time spent attacking");
            patterns[0].ResetCondition();
            Check(!patterns[0].IsReadyToUse(context), "Explicit full reset still resets cooldown for reuse");
            Check(Mathf.Approximately(ai.GetEngagementRange(), 3f), "Hybrid selects ready melee range while SpikeShot cools down");

            patterns[0].Update(1000f, context);
            var table = (Dictionary<string, ActiveSkill>)typeof(SkillComponent).GetField("skillSlotTable", Hidden).GetValue(skills);
            typeof(ActiveSkill).GetField("currentCooldown", Hidden).SetValue(table["SLOT2"], 10f);
            Check(Mathf.Approximately(ai.GetEngagementRange(), 3f), "Native skill cooldown also excludes an unavailable ranged attack");
            typeof(ActiveSkill).GetField("currentCooldown", Hidden).SetValue(table["SLOT1"], 10f);
            Check(Mathf.Approximately(ai.GetEngagementRange(), 1.5f), "Hybrid uses the configured fallback if every skill is cooling down");

            var action = new WeaponAction { aiController = new BlackboardVariable<AIController>(ai) };
            var start = typeof(WeaponAction).GetMethod("OnStart", Hidden);
            Check((Node.Status)start.Invoke(action, null) == Node.Status.Success,
                "Unavailable attacks continue to the existing wait/strafe and state-reset sequence");
            state.SetStopMode();
            Check((Node.Status)start.Invoke(action, null) == Node.Status.Failure, "Stopped characters do not take the idle recovery path");
            state.SetDeadMode();
            Check((Node.Status)start.Invoke(action, null) == Node.Status.Failure, "Dead characters do not take the idle recovery path");

            var graph = File.ReadAllText("Assets/9.Behaviour/EnemyAI.asset").Replace("\r\n", "\n");
            Check(graph.Contains("rid: 9058063005651042546\n        LinkedVariable:\n          rid: 5085156134844105210"),
                "Authoring navigation threshold links to AttackDistance");
            Check(graph.Contains("DistanceThreshold:\n          rid: 455252085954576495"),
                "Runtime navigation threshold shares AttackDistance instead of fixed two units");
            File.AppendAllText(Report, $"COMPLETE {checks} checks passed\n");
            Debug.Log($"AI_COMBAT_REGRESSION_PASS: {checks} checks");
        }
        finally
        {
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(owner);
        }
    }
}
#endif
