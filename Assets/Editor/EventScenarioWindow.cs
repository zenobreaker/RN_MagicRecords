#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

// A transient inspector model: edits never dirty the event JSON or a prefab.
public sealed class EventScenarioDraft : ScriptableObject
{
    public EventInfo data;
}

public sealed class EventScenarioBuildGuard : IProcessSceneWithReport
{
    public int callbackOrder => 0;
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report != null && scene.path == EventScenarioWindow.ScenePath)
            throw new BuildFailedException("EventTest is editor-only. Remove it from the build scene list.");
    }
}

[InitializeOnLoad]
public sealed class EventScenarioWindow : EditorWindow
{
    public const string ScenePath = "Assets/8.Scenes/EventTest.unity";
    private const string SandboxKey = "EventScenario.UseSandbox";
    [SerializeField] private int characterId = 1, classId = 1, coins = 1000, nodeId = 1;
    [SerializeField] private int[] skillIds = new int[4];
    [SerializeField] private int selectedEventId = 1012;
    [SerializeField] private bool logState = true;
    private EventScenarioDraft draft;
    private SerializedObject draftInspector;
    private Vector2 scroll;
    private string status;
    private double prepareDeadline;
    private bool preparing;
    private string lastSnapshot;

    static EventScenarioWindow()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                SessionState.SetBool(SandboxKey, SceneManager.GetActiveScene().path == ScenePath);
            // Keep the sandbox enabled through OnDisable/OnApplicationQuit saves.
            if (state == PlayModeStateChange.EnteredEditMode)
                SessionState.SetBool(SandboxKey, false);
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(SandboxKey, false))
            {
                var window = GetWindow<EventScenarioWindow>("Event Scenario");
                window.preparing = true;
                window.prepareDeadline = EditorApplication.timeSinceStartup + 30;
            }
        };
    }

    [MenuItem("Tools/Event/Scenario Tester")]
    public static void Open() => GetWindow<EventScenarioWindow>("Event Scenario").Show();

    private void OnEnable()
    {
        minSize = new Vector2(590, 640);
        EditorApplication.update += Tick;
    }
    private void OnDisable()
    {
        EditorApplication.update -= Tick;
        if (draft != null) DestroyImmediate(draft);
    }
    private void Tick()
    {
        if (preparing && Application.isPlaying)
        {
            var app = AppManager.Instance;
            if (app != null && SkillTreeManager.IsInitialized && CurrencyManager.IsInitialized &&
                UIRegistry.Get<IUIContainer>() != null && app.GetDataBaseManager()?.GetAllRecordData()?.Count > 0)
            {
                preparing = false;
                Execute(PrepareNewRun);
                Execute(LoadDraft);
            }
            else if (EditorApplication.timeSinceStartup > prepareDeadline)
            {
                preparing = false;
                status = "초기화 시간 초과. Console을 확인한 뒤 새 탐사 준비를 다시 실행하세요.";
            }
        }
        if (CanRun && AppManager.Instance?.GetExploreManager() != null)
        {
            string snapshot = Snapshot();
            if (snapshot != lastSnapshot)
            {
                lastSnapshot = snapshot;
                if (logState) Debug.Log("[EventScenario] " + snapshot);
            }
        }
        if (Application.isPlaying) Repaint();
    }

    public static bool CanRun => Application.isPlaying && SessionState.GetBool(SandboxKey, false) &&
        (SceneManager.GetActiveScene().path == ScenePath || SceneManager.GetActiveScene().name == "StageSelectScene");

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("이벤트 시나리오 테스트", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("EventTest 전용 씬에서 실제 이벤트 선택지와 비용/보상 처리를 실행합니다. " +
            "저장은 persistentDataPath/EventScenario로 분리되며, 이벤트 편집은 메모리에만 적용됩니다.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("EventTest 씬 열고 실행", GUILayout.Height(28)))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(ScenePath);
                    EditorApplication.EnterPlaymode();
                }
            }
        }
        characterId = EditorGUILayout.IntField("캐릭터 ID", characterId);
        classId = EditorGUILayout.IntField("직업 ID", classId);
        coins = Mathf.Max(0, EditorGUILayout.IntField("탐사 재화", coins));
        EditorGUILayout.LabelField("장착 스킬 ID (0 = 직업 스킬 자동 선택)");
        for (int i = 0; i < 4; i++) skillIds[i] = EditorGUILayout.IntField("슬롯 " + (i + 1), skillIds[i]);
        using (new EditorGUI.DisabledScope(!CanRun))
        {
            if (GUILayout.Button("새 탐사 준비 / 초기화")) Execute(PrepareNewRun);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("첫 보상 3장")) Execute(() => AppManager.Instance.GetRecordManager().GenerateChapterStartRecords());
            if (GUILayout.Button("이후 보상 3장")) Execute(() => AppManager.Instance.GetRecordManager().GenerateRewardRecords());
            if (GUILayout.Button("MidRun 복구 검증")) Execute(RestoreMidRun);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("탐사 재화 설정")) Execute(SetCoins);

            EditorGUILayout.Space();
            var events = FindDatabase()?.GetTestEvents().OrderBy(e => e.id).ToArray() ?? Array.Empty<EventInfo>();
            if (events.Length > 0)
            {
                int index = Math.Max(0, Array.FindIndex(events, e => e.id == selectedEventId));
                int next = EditorGUILayout.Popup("원본 이벤트", index, events.Select(e => $"{e.id} - {Localize(e.nameKey)}").ToArray());
                selectedEventId = events[next].id;
            }
            if (GUILayout.Button("선택 이벤트 복사 (편집 초기화)")) Execute(LoadDraft);
            if (draft != null && draftInspector != null)
            {
                draftInspector.Update();
                EditorGUILayout.PropertyField(draftInspector.FindProperty("data"), new GUIContent("테스트 이벤트 / 선택지"), true);
                draftInspector.ApplyModifiedProperties();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("선택지 전부 성공 (100%)")) SetProbability(100);
                if (GUILayout.Button("선택지 전부 실패 (0%)")) SetProbability(0);
                EditorGUILayout.EndHorizontal();
            }
            nodeId = Mathf.Max(1, EditorGUILayout.IntField("대상 노드 ID", nodeId));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("이 노드에 이벤트 적용")) Execute(() => Apply(false));
            if (GUILayout.Button("전체 일반 노드에 적용")) Execute(() => Apply(true));
            if (GUILayout.Button("대상 노드 진입 / 재실행")) Execute(EnterNode);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("JSON 원본 이벤트 다시 로드")) Execute(() => { FindDatabase().Initialize(); LoadDraft(); });
            logState = EditorGUILayout.Toggle("상태 로그 출력", logState);
            DrawState();
        }
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.None);
        EditorGUILayout.EndScrollView();
    }

    private static string Localize(string key) => LocalizationManager.Instance?.GetText(key) ?? key;
    private static EventDataBase FindDatabase() => UnityEngine.Object.FindFirstObjectByType<EventDataBase>();
    private void Execute(Action action)
    {
        try
        {
            if (!CanRun) throw new InvalidOperationException("EventTest 씬에서 테스트를 실행하세요.");
            action();
            if (logState) Debug.Log("[EventScenario] " + status);
        }
        catch (Exception e) { status = e.Message; Debug.LogException(e); }
    }

    public void PrepareNewRun()
    {
        if (!CanRun) throw new InvalidOperationException("EventTest Play Mode required.");
        var app = AppManager.Instance;
        var available = SkillTreeManager.Instance.GetAvailableSkills(classId)
            .Where(s => s.template is SO_ActiveSkillData && s.GetMaxSkillLevel() > 0).OrderBy(s => s.GetSkillID()).ToList();
        if (available.Count == 0 || app.GetEquippedActiveSkillListByCharID(characterId) == null)
            throw new InvalidOperationException("유효한 캐릭터/직업/스킬을 지정하세요.");
        var selected = new List<SkillRuntimeData>();
        for (int i = 0; i < 4; i++)
        {
            var data = skillIds[i] == 0 ? available.FirstOrDefault(s => !selected.Contains(s)) : available.FirstOrDefault(s => s.GetSkillID() == skillIds[i]);
            if (data == null || selected.Contains(data)) throw new InvalidOperationException("서로 다른 유효한 스킬 4개를 지정하세요.");
            selected.Add(data);
        }
        UIManager.Instance.CloseAllOpenedUI(); PauseManager.Reset();
        app.EnterTheExplorationProcess();
        UIManager.Instance.CloseAllOpenedUI(); PauseManager.Reset();
        var explore = app.GetExploreManager();
        foreach (var data in available) { data.currentLevel = 0; data.isUnlocked = false; }
        for (int i = 0; i < selected.Count; i++)
        {
            selected[i].currentLevel = 1; selected[i].isUnlocked = true;
            selected[i].OnDataChanged?.Invoke(selected[i]); app.EquipActiveSkill(characterId, i, selected[i]);
        }
        explore.FinallizeSetupAndGenerateMap(new ExplorationSetupData { SelectedCharacterId = characterId, SelectedClassId = classId });
        SetCoins();
        RefreshMap(true);
        status = "새 탐사 준비 완료. 첫 보상 또는 이벤트 노드를 실행하세요.";
    }

    private void SetCoins()
    {
        var currency = CurrencyManager.Instance;
        int current = currency.GetCurrency(CurrencyType.EXPOLORE_COIN);
        if (coins > current) currency.AddCurrency(CurrencyType.EXPOLORE_COIN, coins - current);
        else if (coins < current) currency.SpendCurrency(CurrencyType.EXPOLORE_COIN, current - coins);
        status = $"탐사 재화: {currency.GetCurrency(CurrencyType.EXPOLORE_COIN)}";
    }
    public void RestoreMidRun()
    {
        var explore = AppManager.Instance.GetExploreManager();
        UIManager.Instance.CloseAllOpenedUI(); PauseManager.Reset();
        explore.SaveExploreMap(); explore.ResetData(); explore.Init(false);
        AppManager.Instance.GetRecordManager().GenerateChapterStartRecords();
        RefreshMap(true);
        status = $"MidRun 복구: {explore.RunStatus}, 첫 보상 대기={explore.InitialRecordRewardPending} (false여야 정상)";
    }
    private void LoadDraft()
    {
        var original = FindDatabase()?.GetEventInfo(selectedEventId);
        if (original == null) throw new InvalidOperationException("활성 이벤트 ID를 선택하세요.");
        if (draft != null) DestroyImmediate(draft);
        draft = CreateInstance<EventScenarioDraft>(); draft.hideFlags = HideFlags.HideAndDontSave;
        draft.data = JsonUtility.FromJson<EventInfo>(JsonUtility.ToJson(original));
        draftInspector = new SerializedObject(draft);
        status = $"이벤트 {selectedEventId} 복사 완료. 선택지의 비용/액션/보상/실패 분기를 편집할 수 있습니다.";
    }
    private void SetProbability(int value)
    {
        foreach (var choice in draft.data.eventChoices) choice.Probability = value;
        draftInspector.Update();
    }
    public static void AssignEventNode(ExploreManager explore, int targetNode, EventInfo data, EventDataBase db)
    {
        var node = explore.StageReplacer.GetLevels().SelectMany(l => l).FirstOrDefault(n => n.id == targetNode);
        if (node == null || targetNode <= 0 || explore.StageReplacer.IsFinalNode(targetNode))
            throw new InvalidOperationException("시작/최종 노드를 제외한 유효한 노드 ID를 지정하세요.");
        db.SetTestEvent(data);
        explore.StageReplacer.GetNodeToInfo()[targetNode] = new MapNodeInfo
        { nodeId = targetNode, type = StageType.Event, contentId = data.id, biome = explore.BiomeName, mapIndex = -1 };
    }
    private void Apply(bool all)
    {
        if (draft?.data == null) throw new InvalidOperationException("이벤트를 먼저 복사하세요.");
        var explore = AppManager.Instance.GetExploreManager();
        var targets = all ? explore.StageReplacer.GetLevels().SelectMany(l => l).Where(n => n.id > 0 && !explore.StageReplacer.IsFinalNode(n.id)).Select(n => n.id).ToArray() : new[] { nodeId };
        foreach (int target in targets) AssignEventNode(explore, target, draft.data, FindDatabase());
        RefreshMap(); status = $"이벤트 {draft.data.id} -> 노드 {string.Join(", ", targets)} 적용 완료";
    }
    private void EnterNode()
    {
        UIManager.Instance.CloseAllOpenedUI(); PauseManager.Reset();
        Apply(false);
        var explore = AppManager.Instance.GetExploreManager();
        var node = explore.StageReplacer.GetLevels().SelectMany(l => l).First(n => n.id == nodeId);
        explore.ConsumeInitialRecordReward();
        explore.EnterStageByNode(node);
        status = $"이벤트 {draft.data.id}, 노드 {nodeId} 진입. UI에서 선택지를 누르세요.";
    }
    private static void RefreshMap(bool rebuild = false)
    {
        // Reuse existing views to preserve their click subscriptions.
        var explore = AppManager.Instance.GetExploreManager();
        foreach (var map in UnityEngine.Object.FindObjectsByType<UIMapReplacer>(FindObjectsSortMode.None))
        {
            if (rebuild)
            {
                var serialized = new SerializedObject(map);
                foreach (string field in new[] { "NodeContainer", "LineContainer" })
                {
                    var container = serialized.FindProperty(field).objectReferenceValue as GameObject;
                    if (container == null) continue;
                    foreach (Transform child in container.transform)
                    {
                        child.gameObject.SetActive(false);
                        UnityEngine.Object.Destroy(child.gameObject);
                    }
                }
                map.ReplaceUINode(explore.StageReplacer);
            }
            var nodes = new List<UIMapNode>(); map.GetUIMapNodes(ref nodes);
            foreach (var node in nodes)
            {
                node.Init(node.Node);
                if (rebuild && node is UIStageMapNode stageNode)
                    stageNode.OnClicked += info => UIManager.Instance.OpenStageInfo(stageNode.Node, info);
            }
            map.UpdateMapUIState(explore, ref nodes);
        }
    }
    private void DrawState()
    {
        if (!CanRun || AppManager.Instance == null) return;
        var explore = AppManager.Instance.GetExploreManager();
        if (explore == null) return;
        EditorGUILayout.LabelField($"상태: {explore.RunStatus} / Chapter {explore.Chapter} / Node {explore.MapNodeID}");
        EditorGUILayout.LabelField($"첫 보상 대기: {explore.InitialRecordRewardPending}");
        EditorGUILayout.LabelField("장착: " + string.Join(", ", AppManager.Instance.GetEquippedActiveSkillIDListByCharID(characterId) ?? new List<int>()));
        EditorGUILayout.LabelField("소지 레코드: " + string.Join(", ", AppManager.Instance.GetRecordManager().GetPossesRecord().Select(r => r.id)));
        EditorGUILayout.HelpBox(Snapshot(), MessageType.None);
    }

    private string Snapshot()
    {
        var app = AppManager.Instance;
        var explore = app.GetExploreManager();
        var slots = app.GetEquippedActiveSkillListByCharID(characterId);
        string equipped = slots == null ? "없음" : string.Join(", ", slots.Select(s => s == null ? "-" : $"{s.GetSkillID()} Lv.{s.currentLevel}"));
        string records = string.Join(", ", app.GetRecordManager()?.GetPossesRecord().Select(r => r.id) ?? Enumerable.Empty<int>());
        return $"{explore.RunStatus} / {explore.CurrentState} / Node {explore.MapNodeID} / Clear {explore.IsCurrentNodeCleared}\n" +
            $"재화 {CurrencyManager.Instance?.GetCurrency(CurrencyType.EXPOLORE_COIN)} / 스킬 [{equipped}] / 레코드 [{records}]";
    }
}
#endif
