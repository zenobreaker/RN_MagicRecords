#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Edit-mode checks never touch player saves, managers, or the open scene.
public static class SkillEventRegression
{
    [MenuItem("Tools/Event/Run Skill Event Regression")]
    public static void Run()
    {
        var assets = new List<Object>();
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("Skill event regression: " + message);
            checks++;
        }
        SkillRuntimeData Skill(int id, int level = 1)
        {
            var so = ScriptableObject.CreateInstance<SO_ActiveSkillData>(); assets.Add(so);
            so.id = id; so.maxLevel = 3;
            return new SkillRuntimeData { template = so, currentLevel = level, isUnlocked = level > 0 };
        }
        GameObject root = null;
        try
        {
            var a = Skill(1); var b = Skill(2, 0); var c = Skill(3);
            var slots = new List<SkillRuntimeData> { a, c, null, null };
            int balance = 500, spendCalls = 0, changes = 0;
            a.OnDataChanged += _ => changes++;
            SkillEventSession New(int cost = 50, int perLevel = 25) => new(
                new[] { a, b, c }, slots, cost, perLevel, () => balance,
                amount => { if (balance < amount) return false; balance -= amount; spendCalls++; return true; },
                (slot, data) => slots[slot] = data);
            var edit = New();
            Check(edit.TryUpgrade(1) && edit.UpgradeCost(1) == 75 && edit.TryUpgrade(1), "escalating upgrade costs");
            Check(edit.PendingCost == 125 && balance == 500 && a.currentLevel == 1 && changes == 0, "draft must not charge or mutate canonical data");
            Check(!edit.TryUpgrade(1), "max level guard");
            Check(!edit.TryReplace(0, 3) && !edit.TryReplace(-1, 2, true), "duplicate and invalid slot guards");
            Check(!edit.TryReplace(0, 2) && edit.TryReplace(0, 2, true), "draft candidate unlock");
            Check(slots[0] == a && b.currentLevel == 0 && !b.isUnlocked, "replacement stays provisional");
            Check(edit.Commit() && balance == 375 && a.currentLevel == 3 && b.currentLevel == 1 && slots[0] == b, "atomic upgrade/replacement application");
            Check(changes == 1 && spendCalls == 1 && !edit.Commit() && !edit.TryUpgrade(2), "single commit and notification");

            balance = 0;
            Check(!New().TryUpgrade(2), "insufficient currency");
            var free = New(0, 0);
            Check(free.TryUpgrade(2) && free.HasChanges && free.Commit() && b.currentLevel == 2 && spendCalls == 1, "free upgrade without a currency debit");
            balance = 100;
            var stale = New(); Check(stale.TryUpgrade(2), "stale balance setup"); balance = 0;
            Check(!stale.Commit() && b.currentLevel == 2 && spendCalls == 1, "balance changed before confirmation");
            balance = 100;
            stale = New(); Check(stale.TryUpgrade(2), "stale skill setup"); b.currentLevel = 3;
            Check(!stale.Commit() && balance == 100, "canonical skill changed before confirmation"); b.currentLevel = 2;
            stale = New(); Check(stale.TryUpgrade(2), "stale slot setup"); slots[1] = null;
            Check(!stale.Commit() && balance == 100, "equipment changed before confirmation"); slots[1] = c;
            var cancelled = New(); Check(cancelled.TryUpgrade(2), "cancel setup"); cancelled = null;
            Check(b.currentLevel == 2 && balance == 100, "discarding a draft leaves live data intact");

            b.currentLevel = 0; b.isUnlocked = false;
            slots[0] = a; // The earlier replacement test equipped b; this fixture needs an unowned candidate.
            balance = 500;
            var preview = New(); preview.PrepareCandidate(2);
            Check(preview.GetSkill(2).currentLevel == 1 && !preview.HasChanges && b.currentLevel == 0,
                "Lv.1 candidate preview is local and not a pending purchase");
            Check(preview.Commit() && b.currentLevel == 0 && !b.isUnlocked && balance == 500,
                "viewing a candidate cannot grant it on commit");
            preview = New(); preview.PrepareCandidate(2); preview.SetReplacementCost(50);
            Check(preview.TryUpgrade(2) && preview.GetSkill(2).currentLevel == 2 && preview.PendingCost == 50 &&
                preview.TryReplace(2, 2, true) && preview.PendingCost == 100, "candidate upgrade then empty-slot equip totals existing costs");
            Check(preview.Commit() && slots[2] == b && b.currentLevel == 2 && balance == 400,
                "candidate upgrade/equip applies level and charges one aggregate payment");

            var layout = New(); layout.SetReplacementCost(50);
            Check(layout.TryEquipOwned(0, 2) && layout.Slots[2] == 0 && layout.Slots[0] == 2 &&
                layout.PendingCost == 0 && layout.ReplacementCount == 0, "move occupied skill clears old slot without buying another candidate");
            Check(layout.TryEquipOwned(2, 2) && layout.TryEquipOwned(0, 1) && !layout.HasChanges,
                "displaced skill can be restored and a round-trip layout is unchanged");
            Check(!layout.TryUnequip(-1) && !layout.TryUnequip(3) && !layout.TryEquipOwned(0, 999),
                "invalid and empty unequip requests do not mutate draft");
            Check(layout.TryUnequip(1) && layout.HasChanges && slots[1] == c,
                "unequip stays provisional without modifying skill ownership");
            Check(layout.Commit() && slots[1] == null && balance == 400 && c.isUnlocked && c.currentLevel == 1 &&
                !layout.TryUnequip(0), "unequip commit preserves money and skill level; committed session is immutable");

            layout = New(); layout.SetReplacementCost(50);
            Check(layout.TryReplace(1, 3) && layout.TryEquipOwned(3, 3) && layout.TryUnequip(3) && layout.TryEquipOwned(1, 3) &&
                layout.PendingCost == 50 && layout.ReplacementCount == 1, "new purchase can move and unequip without a second charge");

            root = new GameObject("Skill event regression (temporary)"); root.SetActive(false);
            var records = root.AddComponent<RecordManager>();
            var related = new RecordData { Skills = new List<RecordSkillData> { new() { SkillID = 1 } } };
            Check(records.IsStartingRecordEligible(related, new HashSet<int> { 1 }), "equipped skill record admitted");
            Check(!records.IsStartingRecordEligible(related, new HashSet<int> { 2 }) &&
                !records.IsStartingRecordEligible(null, new HashSet<int>()), "unrelated and null record rejected");
            Check(records.IsStartingRecordEligible(new RecordData { Stats = new List<RecordStatData> { new() } }, new HashSet<int>()),
                "general stat record remains eligible even without an equipped skill");
            Check(records.IsStartingRecordEligible(new RecordData(), new HashSet<int> { 1 }),
                "general records without a specific skill target remain eligible");

            var db = root.AddComponent<EventDataBase>();
            var json = new TextAsset(File.ReadAllText("Assets/98.Datas/eventList.json")); assets.Add(json);
            typeof(DataBase).GetField("jsonAsset", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(db, json);
            db.Initialize();
            var draft = db.GetEventInfo(1012)?.eventChoices.FirstOrDefault(x => x.ChoiceIndex == 1);
            Check(draft != null && draft.ChoiceIsActive && draft.ActionType == EventActionType.RECORD_SKILL_UP &&
                draft.ActionParam == EventActionParam.DRAFT_3 && draft.ActionValue == 3, "actual draft event JSON parsing");
            Check(db.GetEventInfo(1010)?.eventChoices.Any(x => x.ChoiceIsActive && x.ActionType == EventActionType.RECORD_SKILL_UP) == true,
                "upgrade event is reachable");
            Check(draft.ResultButtonTextKey == "ui_btn_leave", "missing result button text defaults to leave");
            var strings = JsonUtility.FromJson<StringDataAllData>(File.ReadAllText("Assets/98.Datas/stringData.json"));
            Check(strings.stringData.Count(x => x.key == "ui_skill_event_upgrade_confirm" && !string.IsNullOrEmpty(x.kr) && !string.IsNullOrEmpty(x.en)) == 1,
                "confirmation localization exists exactly once");
            Debug.Log($"SKILL_EVENT_REGRESSION_PASS: {checks} checks (draft, commit, cost, cancellation, record eligibility, real JSON)");
        }
        finally
        {
            if (root != null) Object.DestroyImmediate(root);
            foreach (var asset in assets) Object.DestroyImmediate(asset);
        }
    }
}
#endif
