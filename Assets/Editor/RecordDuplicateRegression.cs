#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class RecordDuplicateRegression
{
    const string Request = "Library/RecordDuplicate.request";
    const string Report = "Library/RecordDuplicate-result.txt";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static RecordDuplicateRegression() => EditorApplication.update += Tick;
    static void Tick()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try { File.ReadAllText(Request); File.Delete(Request); }
        catch (IOException) { return; }
        try { Run(); }
        catch (Exception e) { File.AppendAllText(Report, e + "\n"); Debug.LogException(e); }
    }
    static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Hidden).GetValue(owner);
    static void Check(bool valid, string message)
    {
        File.AppendAllText(Report, (valid ? "PASS " : "FAIL ") + message + "\n");
        if (!valid) throw new Exception(message);
    }
    [MenuItem("Tools/Record/Check Duplicate Grants (Sandbox Play Mode)")]
    public static void Run()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("ShopRegression.SaveDirectory", "")))
            throw new Exception("Requires isolated-save ShopRegression Play Mode.");
        File.WriteAllText(Report, "Unity " + Application.unityVersion + " actual Play Mode\n");
        var app = AppManager.Instance;
        var rm = app.GetRecordManager();
        rm.ResetRecordFlowData(); rm.SelectedRecords.Clear();
        var inventory = Field<RecordInventory>(rm, "recordInventory");
        var archive = Field<RecordInventory>(rm, "transferInventory");
        archive.ClearAll();
        var ps = app.GetPassiveSystem(); ps.ResetExplorePassives();
        var passiveGroups = Field<Dictionary<int, List<PassiveSkill>>>(ps, "passiveSkillList");
        int PassiveCount() => passiveGroups.TryGetValue(Constants.GLOBAL_RECORD_JOB_ID, out var list) ? list.Count : 0;
        var original = rm.GetShopCandidates().First();
        var blank1 = rm.GetEmptyRecord(); var blank2 = rm.GetEmptyRecord();
        Check(blank1.id == RecordDataBase.EmptyRecordId && blank1.type == RecordType.EMPTY && blank1.uniqueID != blank2.uniqueID,
            "existing empty template creates distinct item identities");
        Check(blank1.recordName == LocalizationManager.Instance.GetText("name_emptymemory") && blank1.recordName != "name_emptymemory",
            "empty record has a localized display name");
        rm.SelectedRecords.Add(original);
        Check(rm.OnCompleteSelctRecords() && rm.GetPossesRecord().Single().id == original.id && PassiveCount() == 1,
            "first draft grant retains the original record and registers its passive");
        rm.SelectedRecords.Add(rm.GetShopRecord(original.id));
        Check(rm.OnCompleteSelctRecords() && rm.GetPossesRecord().Count(r=>r.id==original.id) == 1 &&
            rm.GetPossesRecord().Count(r=>r.id==RecordDataBase.EmptyRecordId) == 1, "duplicate draft grants one empty record instead of another original");
        Check(PassiveCount() == 1, "duplicate draft does not register the original passive again");
        var duplicate = rm.GrantRecord(original);
        Check(duplicate.id == RecordDataBase.EmptyRecordId && inventory.GetRecord(duplicate.uniqueID) == duplicate,
            "duplicate result is returned and indexed for lookup");
        var directBlank = rm.GrantRecord(blank1);
        var repeatedBlank = rm.GrantRecord(blank1);
        Check(directBlank.uniqueID != repeatedBlank.uniqueID && inventory.GetRecord(directBlank.uniqueID) == directBlank &&
            inventory.GetRecord(repeatedBlank.uniqueID) == repeatedBlank, "multiple empty rewards and repeated input objects remain individually indexed");
        rm.SelectedRecords.Add(duplicate);
        Check(rm.OnCompleteCostDiscard() && inventory.GetRecord(duplicate.uniqueID) == null &&
            inventory.GetRecord(directBlank.uniqueID) != null, "empty record can be spent without removing a different empty item");
        rm.SetTranferRecord(directBlank);
        Check(inventory.GetRecord(directBlank.uniqueID) == null && archive.GetRecord(directBlank.uniqueID) != null,
            "empty record can be archived using its unique identity");
        rm.SetTranferRecord(original);
        var reacquired = rm.GrantRecord(rm.GetShopRecord(original.id));
        Check(reacquired.id == original.id, "a record no longer in current inventory can be acquired normally");
        int emptyBefore = inventory.Records.Count(r=>r.id==RecordDataBase.EmptyRecordId);
        rm.SelectedRecords.Add(original);
        Check(rm.OnCompleteInheritReward() && archive.GetRecord(original.uniqueID) == null &&
            inventory.Records.Count(r=>r.id==original.id) == 1 && inventory.Records.Count(r=>r.id==RecordDataBase.EmptyRecordId) == emptyBefore + 1,
            "inheriting an already-owned record grants an empty and consumes the archived source");
        Check(PassiveCount() == 1, "duplicate inheritance does not add a passive");
        rm.SaveIfDirty();
        var saved = SaveManager.LoadRecordData();
        var beforeOwned = inventory.Records.Select(r=>r.id+":"+r.uniqueID).OrderBy(s=>s).ToArray();
        var beforeArchive = archive.Records.Select(r=>r.id+":"+r.uniqueID).OrderBy(s=>s).ToArray();
        typeof(RecordManager).GetMethod("ApplySavedRecords", Hidden).Invoke(rm, new object[] { saved });
        Check(beforeOwned.SequenceEqual(inventory.Records.Select(r=>r.id+":"+r.uniqueID).OrderBy(s=>s)) &&
            beforeArchive.SequenceEqual(archive.Records.Select(r=>r.id+":"+r.uniqueID).OrderBy(s=>s)),
            "existing save/load preserves empty and normal records, counts and unique identities in both inventories");
        rm.SelectedRecords.Add(archive.Records.Single());
        Check(rm.OnCompleteInheritReward() && archive.Records.Count == 0 && PassiveCount() == 1,
            "restored empty archive reward can be inherited without a passive");
        var restoredEmpty = inventory.Records.First(r=>r.id==RecordDataBase.EmptyRecordId);
        inventory.RemoveRecord(restoredEmpty);
        Check(inventory.GetRecord(restoredEmpty.uniqueID) == null, "restored empty can be individually removed");
        int before = inventory.Records.Count;
        new RecordReward(RecordRewardMode.FixedRecord, original.id).Receive();
        Check(inventory.Records.Count == before + 1 && inventory.Records.Last().id == RecordDataBase.EmptyRecordId,
            "fixed duplicate reward grants an empty via the common acquisition path");
        foreach (var record in app.GetAllRecordData()) rm.AddRecord(record);
        foreach (var mode in new[] { RecordRewardMode.RandomAll, RecordRewardMode.RandomByRarity })
        {
            UIManager.Instance.CloseAllOpenedUI();
            before = inventory.Records.Count;
            new RecordReward(mode, rarity: original.rarity).Receive();
            var ui = Object.FindFirstObjectByType<RecordUI>();
            var cards = Field<List<RecordCard>>(ui, "cards");
            Check(inventory.Records.Count == before + 1 && inventory.Records.Last().id == RecordDataBase.EmptyRecordId,
                mode + " duplicate reward grants exactly one empty");
            Check(cards.Count == 1 && cards[0].myData == inventory.Records.Last() && cards[0].myData.id == RecordDataBase.EmptyRecordId,
                mode + " result popup displays the actual empty grant, not the original record");
        }
        Check(inventory.Records.Select(r=>r.uniqueID).Distinct().Count() == inventory.Records.Count &&
            inventory.Records.All(r=>inventory.GetRecord(r.uniqueID) == r), "all grants keep list and unique lookup consistent");
        var legacy = new RecordSaveListData();
        legacy.recordIDs.Add(new RecordSaveData { recordID=RecordDataBase.EmptyRecordId, uniqueID="old-shared-template-id" });
        legacy.recordIDs.Add(new RecordSaveData { recordID=RecordDataBase.EmptyRecordId, uniqueID="old-shared-template-id" });
        typeof(RecordManager).GetMethod("ApplySavedRecords", Hidden).Invoke(rm, new object[] { legacy });
        Check(inventory.Records.Count == 2 && inventory.Records.Select(r=>r.uniqueID).Distinct().Count() == 2,
            "legacy empty items with a shared template identity load as separately usable items");
        Debug.Log("RECORD_DUPLICATE_REGRESSION_PASS");
    }
}
#endif
