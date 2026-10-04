#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class OverchargeRecordRegression
{
    private sealed class Bullets : IMagicBulletProvider
    {
        public int CurrentBulletCount { get; private set; }
        public Bullets(int count) { CurrentBulletCount = count; }
        public void Reload(int amount) => CurrentBulletCount += amount;
        public bool TryConsumBullet(out bool isCrit)
        {
            isCrit = false;
            if (CurrentBulletCount <= 0) return false;
            CurrentBulletCount--;
            return true;
        }
    }

    [MenuItem("Tools/Record/Check Plasma Overcharge (Sandbox Play Mode)")]
    public static void Run()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("ShopRegression.SaveDirectory", "")))
            throw new InvalidOperationException("Requires isolated-save ShopRegression Play Mode.");
        const string report = "Library/OverchargeRecordRegression-result.txt";
        File.WriteAllText(report, "Plasma Overcharge regression\n");
        int checks = 0;
        void Check(bool value, string message)
        {
            File.AppendAllText(report, (value ? "PASS " : "FAIL ") + message + "\n");
            if (!value) throw new InvalidOperationException(message);
            checks++;
        }
        var recordManager = AppManager.Instance.GetRecordManager();
        Check(!recordManager.GetPossesRecord().Any(record => record.id == 10021), "Sandbox starts without Overcharge");
        var template = AssetDatabase.LoadAssetAtPath<SO_PassiveSkillData>(
            "Assets/10.ScriptableObjects/Skills/Shooter/Passive/Modifiers/plasmaray-overcharge.asset");
        Check(template.Modules.Single() is Module_Passive_LaserEnergyGain bonus && bonus.bonusGain == 5 && bonus.targetSkillID == 1002,
            "Overcharge asset replaces beam radius with energy gain +5 for Plasma Ray");
        var plasma = AssetDatabase.LoadAssetAtPath<SO_ActiveSkillData>("Assets/10.ScriptableObjects/Skills/Shooter/plasmaray.asset");
        var hyper = AssetDatabase.LoadAssetAtPath<SO_ActiveSkillData>("Assets/10.ScriptableObjects/Skills/Shooter/hyperbeam.asset");
        var fixture = new GameObject("Overcharge regression fixture");
        fixture.SetActive(false);
        var owner = fixture.AddComponent<Character>();
        var effects = fixture.AddComponent<EffectComponent>();
        RecordData granted = null;
        try
        {
            int Gain(SO_ActiveSkillData skillData, int bulletCount, int startingEnergy = 0)
            {
                effects.ClearEffects();
                for (int index = 0; index < startingEnergy; index++)
                    EffectManager.Instance.RegisterEffect(fixture, fixture, new LaserEnergyEffect());
                var skill = new GenericActiveSkill(skillData);
                AppManager.Instance.GetPassiveSystem().BroadcastOnSkillCast(
                    new SkillUseEvent { SkillID = skill.SkillID, Owner = owner }, skill.Runtime);
                skill.Runtime.Combat.ConsumeMagicBullets(new Bullets(bulletCount), 3, BulletEffectApplyMode.AllAttack);
                var modules = skillData.phaseList.SelectMany(phase => phase.modules).ToArray();
                var consume = modules.OfType<Module_ConsumeLaserEnergy>().Single().Clone();
                var gain = modules.OfType<Module_GainLaserEnergy>().Single().Clone();
                consume.OnNotify(owner, skill, null);
                gain.OnNotify(owner, skill, null);
                int count = effects.HasEffect(LaserEnergyEffect.EffectID)?.StackCount ?? 0;
                gain.OnNotify(owner, skill, null);
                Check((effects.HasEffect(LaserEnergyEffect.EffectID)?.StackCount ?? 0) == count,
                    "Repeated notification does not grant energy twice");
                return count;
            }

            Check(Gain(plasma, 0) == 1, "Without record: zero bullets grants 1");
            Check(Gain(plasma, 3) == 4, "Without record: three bullets grants 4");
            int otherSkillGain = Gain(hyper, 0);
            var recordAsset = AssetDatabase.LoadAssetAtPath<SO_RecordData>("Assets/10.ScriptableObjects/Records/10021_name_overcharge.asset");
            granted = recordManager.GrantRecord(recordAsset.GetRecordData());
            Check(granted != null && granted.id == 10021, "Actual record acquisition registers the passive");
            Check(granted.description == "플라즈마 레이의 레이저 에너지 획득량 +5", "Record UI description uses the new effect");
            Check(Gain(plasma, 0) == 6, "With record: zero bullets grants 6");
            Check(Gain(plasma, 3) == 9, "With record: three bullets grants 9");
            Check(Gain(plasma, 0) == 6, "Next cast resets the per-cast bonus instead of accumulating it");
            Check(Gain(hyper, 0) == otherSkillGain, "Other laser skill receives no Overcharge bonus");
            Check(Gain(plasma, 3, 10) == 0, "Energy-consuming empowered cast retains the no-gain rule");
            Check(recordManager.RemoveOwnedRecord(granted), "Record removal succeeds");
            granted = null;
            Check(Gain(plasma, 3) == 4, "After removal: energy gain returns to 4");
            Debug.Log($"Overcharge record regression passed: {checks} checks; energy 1 -> 6 and 4 -> 9.");
        }
        finally
        {
            if (granted != null) recordManager.RemoveOwnedRecord(granted);
            EffectManager.Instance.UnregisterAllEffects(owner);
            Object.DestroyImmediate(fixture);
        }
    }
}
#endif
