#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Explicit, isolated-save test spanning real Play Mode shutdown/restart cycles.
[InitializeOnLoad]
public static class RunPersistenceRegression
{
    const string Request="Library/RunPersistence.request", Report="Library/RunPersistence-result.txt";
    const string Phase="RunPersistence.Phase", DirectoryKey="RunPersistence.Directory", Expected="RunPersistence.Records";
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static double deadline;
    static RunPersistenceRegression()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetString(Phase,"").StartsWith("restart-"))
                EditorApplication.delayCall+=()=>
                {
                    SessionState.SetString("ShopRegression.SaveDirectory",SessionState.GetString(DirectoryKey,""));
                    SessionState.SetString(Phase,SessionState.GetString(Phase,"").Substring(8));
                    EditorApplication.EnterPlaymode();
                };
        };
    }
    static T Field<T>(object owner,string field)=>(T)owner.GetType().GetField(field,Hidden).GetValue(owner);
    static void Check(bool valid,string message)
    {
        File.AppendAllText(Report,(valid?"PASS ":"FAIL ")+message+"\n");
        if(!valid) throw new Exception(message);
    }
    static void SetPhase(string phase) { SessionState.SetString(Phase,phase); deadline=EditorApplication.timeSinceStartup+45; }
    static void Restart(string next)
    {
        SessionState.SetString(DirectoryKey,SessionState.GetString("ShopRegression.SaveDirectory",""));
        SetPhase("restart-"+next); EditorApplication.ExitPlaymode();
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try
        {
            if(File.Exists(Request))
            {
                string action;
                try { action=File.ReadAllText(Request).Trim(); File.Delete(Request); } catch(IOException) { return; }
                if(action=="start")
                {
                    if(!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("ShopRegression.SaveDirectory","")))
                        throw new Exception("Requires sandbox Lobby Play Mode.");
                    File.WriteAllText(Report,"Unity "+Application.unityVersion+" actual Play Mode with two cold restarts\n");
                    Seed(); return;
                }
            }
            string phase=SessionState.GetString(Phase,"");
            if(phase=="" || phase.StartsWith("restart-") || !Application.isPlaying) return;
            if(deadline==0) deadline=EditorApplication.timeSinceStartup+45;
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Timed out in "+phase);
            var app=AppManager.Instance;
            if(app==null || !AppManager.IsInitialized || UIManager.Instance==null) return;
            var explore=app.GetExploreManager(); var records=app.GetRecordManager();
            if(phase=="resume-mid" || phase=="resume-final")
            {
                var expected=JsonUtility.FromJson<RecordSaveListData>(SessionState.GetString(Expected,""));
                Check(Same(expected.recordIDs,records.GetPossesRecord()) && Same(expected.transferedrecordIDs,records.GetTransferedRecordIDs()),
                    phase+": real startup restores owned/archive records and unique identities");
                int normal=records.GetPossesRecord().Count(r=>r.id!=RecordDataBase.EmptyRecordId);
                var passives=Field<Dictionary<int,List<PassiveSkill>>>(app.GetPassiveSystem(),"passiveSkillList");
                Check(passives[Constants.GLOBAL_RECORD_JOB_ID].Count==normal,
                    phase+": real startup rebuilds owned record effects without empty/archive effects");
                Check(passives[Constants.GLOBAL_RECORD_JOB_ID].All(p=>p is GenericPassiveSkill),
                    phase+": saved skill modifier records restore their original passive modules");
                SetPhase(phase=="resume-mid"?"map-mid":"map-final");
                app.ContinueExplorationProcess(); return;
            }
            if(SceneManager.GetActiveScene().name=="StageSelectScene" && phase=="map-mid")
            {
                if(explore.RunStatus!=RunStatus.MidRun || explore.StageReplacer==null) return;
                Check(explore.MapNodeID==SessionState.GetInt("RunPersistence.Node",-1) && !explore.InitialRecordRewardPending,
                    "continue preserves current node and does not grant another starting draft");
                Check(Object.FindFirstObjectByType<UITotalResultPopUp>()==null,"mid-run continue does not open completion UI");
                typeof(ExploreManager).GetField("maxChapter",Hidden).SetValue(explore,explore.Chapter);
                int boss=explore.StageReplacer.GetLevels().Last()[0].id;
                typeof(ExploreManager).GetField("<MapNodeID>k__BackingField",Hidden).SetValue(explore,boss);
                explore.ChangeState(ExploreState.IN_STAGE); explore.ClearStage(true);
                Check(SaveManager.LoadExploreRun().runStatus==RunStatus.FinalRunCleared && records.GetPossesRecord().Count>0,
                    "final boss clear retains result data until confirmation");
                Restart("resume-final"); return;
            }
            if(SceneManager.GetActiveScene().name=="StageSelectScene" && phase=="map-final")
            {
                var popup=Object.FindFirstObjectByType<UITotalResultPopUp>();
                if(popup==null) return;
                Check(explore.RunStatus==RunStatus.FinalRunCleared,"unconfirmed final result survives restart and opens result UI");
                SetPhase("lobby"); popup.OnClickedConfirmButton(); return;
            }
            if(phase=="lobby" && SceneManager.GetActiveScene().name=="Lobby")
            {
                Check(!SaveManager.HasSavedExploreRun() && SaveManager.LoadExploreRun()==null && !app.HasSavedExploration(),
                    "actual result confirm removes exploration save and continue eligibility");
                Check(explore.RunStatus==RunStatus.NoSave && explore.CurrentState==ExploreState.NONE &&
                    explore.StageReplacer==null && !explore.AllStageClear && explore.CurrentSetupData.SelectedCharacterId==-1,
                    "result confirm clears in-memory completion/setup/map state");
                Check(records.GetPossesRecord().Count==0 && records.GetTransferedRecordIDs().Count==1 &&
                    SaveManager.LoadRecordData().recordIDs.Count==0 && SaveManager.LoadRecordData().transferedrecordIDs.Count==1,
                    "result confirm clears run records while retaining saved archive");
                app.SaveIfDirty();
                Check(!SaveManager.HasSavedExploreRun(),"late scene/quit save cannot recreate the completed run");
                var main=UIManager.Instance.OpenUI<Exploration_Main_UI>();
                if(main==null) throw new Exception("Lobby exploration menu unavailable");
                main.EnterTheExploration();
                Check(explore.RunStatus==RunStatus.SetupIncomplete && explore.Chapter==1 && explore.MapNodeID==0 &&
                    Object.FindFirstObjectByType<UIExplorationSetup>()!=null && Object.FindFirstObjectByType<UITotalResultPopUp>()==null,
                    "actual lobby exploration button opens fresh setup, not the old completion popup");
                Check(records.GetPossesRecord().Count==0 && records.GetTransferedRecordIDs().Count==1,
                    "fresh setup preserves archive and carries no previous-run records");
                SessionState.EraseString(Phase); File.AppendAllText(Report,"RUN_PERSISTENCE_REGRESSION_PASS\n");
            }
        }
        catch(Exception e)
        {
            SessionState.EraseString(Phase); File.AppendAllText(Report,e+"\n"); Debug.LogException(e);
        }
    }
    static bool Same(List<RecordSaveData> saved,List<RecordData> records) =>
        saved.Select(r=>r.recordID+":"+r.uniqueID).OrderBy(s=>s).SequenceEqual(records.Select(r=>r.id+":"+r.uniqueID).OrderBy(s=>s));
    static void Seed()
    {
        var app=AppManager.Instance; var records=app.GetRecordManager(); var explore=app.GetExploreManager();
        app.EnterTheExplorationProcess();
        explore.FinallizeSetupAndGenerateMap(new ExplorationSetupData {SelectedCharacterId=1,SelectedClassId=1});
        UIManager.Instance.CloseAllOpenedUI();
        int node=explore.StageReplacer.GetLevels()[1][0].id;
        typeof(ExploreManager).GetField("<MapNodeID>k__BackingField",Hidden).SetValue(explore,node);
        SessionState.SetInt("RunPersistence.Node",node); explore.SaveExploreMap();
        var options=records.GetShopCandidates().Where(r=>r.type==RecordType.MODIFY).Take(3).ToArray();
        foreach(var record in options.Take(2)) { records.SelectedRecords.Add(record); records.OnCompleteSelctRecords(); }
        records.GrantRecord(records.GetEmptyRecord());
        Check(SaveManager.LoadRecordData().recordIDs.Count==3,"acquisition immediately writes normal and empty records before quitting");
        var archived=records.GrantRecord(options[2]); records.SetTranferRecord(archived);
        Check(SaveManager.LoadRecordData().recordIDs.Count==3 && SaveManager.LoadRecordData().transferedrecordIDs.Count==1,
            "archive transfer immediately persists both inventory changes");
        var spent=records.GrantRecord(records.GetEmptyRecord()); records.SelectedRecords.Add(spent); records.OnCompleteCostDiscard();
        Check(SaveManager.LoadRecordData().recordIDs.Count==3,"record cost is persisted immediately without resurrecting spent items");
        var saved=SaveManager.LoadRecordData(); SessionState.SetString(Expected,JsonUtility.ToJson(saved));
        SaveManager.DeleteExploreRun();
        Check(!SaveManager.HasSavedExploreRun() && Same(saved.recordIDs,records.GetPossesRecord()) &&
            JsonUtility.ToJson(SaveManager.LoadRecordData())==JsonUtility.ToJson(saved),
            "deleting exploration save targets runSaveData, never record.json");
        explore.SaveExploreMap();
        Restart("resume-mid");
    }
}
#endif
