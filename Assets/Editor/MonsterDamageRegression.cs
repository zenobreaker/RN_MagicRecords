#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MonsterDamageRegression
{
    private const string Skills = "Assets/10.ScriptableObjects/Skills/MonsterSkills/";
    private const string Report = "Library/MonsterDamageRegression-result.txt";
    private static int checks;

    private static void Check(bool condition, string message)
    {
        File.AppendAllText(Report, (condition ? "PASS " : "FAIL ") + message + "\n");
        if (!condition) throw new InvalidOperationException(message);
        checks++;
    }

    private static DamageData Resolve(string name, out int count)
    {
        var asset = AssetDatabase.LoadAssetAtPath<SO_ActiveSkillData>(Skills + name + ".asset");
        var runtime = new GenericActiveSkill(asset);
        var modules = asset.phaseList.SelectMany(phase => phase.modules).ToList();
        for (int index = 0; index < modules.Count; index++)
            if (modules[index] is Module_SpawnWarningSign warning && warning.onSignEndModules != null)
                modules.AddRange(warning.onSignEndModules);
        foreach (var setter in modules.OfType<Module_SetDamageData>())
            setter.OnNotify(null, runtime, null);
        var spawn = modules.OfType<Module_SpawnObject>().SingleOrDefault();
        count = spawn?.baseSpawnCount ?? 1;
        return spawn == null ? runtime.damageData : (DamageData)typeof(Module_SpawnObject)
            .GetMethod("GetEffectiveDamageData", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(spawn, new object[] { runtime });
    }

    [MenuItem("Tools/Combat/Check Monster Damage (Sandbox Play Mode)")]
    public static void Run()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("ShopRegression.SaveDirectory", "")))
            throw new InvalidOperationException("Requires isolated-save ShopRegression Play Mode.");
        File.WriteAllText(Report, "Monster damage regression\n");
        checks = 0;
        var randomState = UnityEngine.Random.state;
        var fixture = new GameObject("Monster damage regression fixture");
        var attacker = fixture.AddComponent<StatusComponent>();
        var target = new GameObject("Defense fixture");
        target.transform.SetParent(fixture.transform);
        var defender = target.AddComponent<StatusComponent>();
        try
        {
            var data = new DamageData { baseDamage = 12 };
            attacker.SetStatusValue(StatusType.CRIT_RATIO, 10);
            foreach (float multiplier in new[] { 0f, -1f, 1f })
            {
                attacker.SetStatusValue(StatusType.CRIT_DMG, multiplier);
                var hit = data.GetMyDamageEvent(attacker, extraCrit: true);
                Check(hit.BaseDamage == 12 && !hit.isCrit, $"Invalid/non-bonus critical multiplier {multiplier} preserves ordinary damage");
            }
            attacker.SetStatusValue(StatusType.CRIT_DMG, 1.5f);
            var crit = data.GetMyDamageEvent(attacker, extraCrit: true);
            Check(crit.isCrit && crit.BaseDamage == 18, "Valid critical still multiplies damage");
            attacker.SetStatusValue(StatusType.CRIT_RATIO, 0);
            Check(!data.GetMyDamageEvent(attacker).isCrit, "Zero critical chance does not crit");
            Check(!new DamageData { baseDamage = 0 }.GetMyDamageEvent(attacker, extraCrit: true).isCrit,
                "Zero-power attack is not classified as critical");

            var textObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/5.Prefabs/UI/DamageText.prefab"), fixture.transform);
            var display = textObject.GetComponent<DamageText>();
            var label = textObject.GetComponent<TMP_Text>();
            foreach (float value in new[] { 0f, 0.1f, 0.49f })
            {
                display.DrawDamage(Vector3.zero, value, true);
                Check(label.text == "<color=#FFFFFF>0</color>", $"Numeric overload: rounded {value} is ordinary white zero");
                display.DrawDamage(Vector3.zero, value, new DamageEvent(value, true));
                Check(label.text == "<color=#FFFFFF>0</color>", $"Event overload: rounded {value} is ordinary white zero");
            }
            display.DrawDamage(Vector3.zero, crit);
            Check(label.text.Contains(">18</color>") && !label.text.Contains("#FFFFFF"), "Positive critical retains critical color");

            var turtle = AssetDatabase.LoadAssetAtPath<CharStatusData>("Assets/10.ScriptableObjects/CharacterInfo/TurtleStatus.asset");
            float defense = turtle.GetStatusValue(StatusType.DEFENSE);
            defender.SetStatusValue(StatusType.DEFENSE, defense);
            var enemies = new[] {
                ("Slime", 101, new[] { "MeleeAttack" }),
                ("TurtleShell", 102, new[] { "MeleeAttack", "SpikeShot" }),
                ("Memosquito", 103, new[] { "MeleeAttack", "SpikeShot" }),
                ("SignalDrone", 104, new[] { "SpikeShot", "TripleShot" }),
                ("TriLegDrone", 201, new[] { "MeleeAttack", "SpikeShot" }),
                ("Wheeler", 901, new[] { "SpikeShot", "TripleShot" }),
                ("DefenseDrone", 902, new[] { "TripleShot", "parabolicShell", "chargebeam" }),
                ("IceBreaker", 903, new[] { "iceburst", "icecrush", "icewave", "breakrush" })
            };
            var damageBySkill = new Dictionary<string, float>();
            foreach (var enemy in enemies)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/5.Prefabs/Characters/" + enemy.Item1 + ".prefab");
                var entries = new SerializedObject(prefab.GetComponent<StatusComponent>()).FindProperty("status").FindPropertyRelative("statusEntries");
                attacker.SetStatusValue(StatusType.CRIT_RATIO, 0);
                attacker.SetStatusValue(StatusType.CRIT_DMG, 0);
                for (int index = 0; index < entries.arraySize; index++)
                {
                    var entry = entries.GetArrayElementAtIndex(index);
                    attacker.SetStatusValue((StatusType)entry.FindPropertyRelative("type").intValue,
                        entry.FindPropertyRelative("value").FindPropertyRelative("baseValue").floatValue);
                }
                attacker.SetStatusValue(StatusType.ATTACK, AppManager.Instance.GetDataBaseManager().GetMonsterStatData(enemy.Item2).attack);
                foreach (var skill in enemy.Item3)
                {
                    var power = Resolve(skill, out int count);
                    var hit = power.GetMyDamageEvent(attacker);
                    float damage = DamageCalculator.CalcDamage(defender, hit);
                    damageBySkill[skill] = damage;
                    Check(!hit.isCrit && damage >= 1 && damage * count < 50,
                        $"{enemy.Item1}/{skill}: defense {defense}, damage {damage:F2}, all projectiles {damage * count:F2}");
                    if (skill == "MeleeAttack" || skill == "SpikeShot" || skill == "TripleShot")
                    {
                        Check(power.baseDamage == 12, skill + " resolves the increased skill power");
                        defender.SetStatusValue(StatusType.DEFENSE, 100);
                        Check(DamageCalculator.CalcDamage(defender, hit) >= 1, skill + " remains positive against defense 100");
                        defender.SetStatusValue(StatusType.DEFENSE, defense);
                    }
                }
            }
            Check(damageBySkill["iceburst"] + damageBySkill["breakrush"] < 70,
                "Ice burst and rush combined leave over 30 HP from 100 HP at base defense");
            Debug.Log($"Monster damage regression passed: {checks} checks. IceBurst={damageBySkill["iceburst"]:F2}, BreakRush={damageBySkill["breakrush"]:F2}");
        }
        finally
        {
            Object.DestroyImmediate(fixture);
            UnityEngine.Random.state = randomState;
        }
    }
}
#endif
