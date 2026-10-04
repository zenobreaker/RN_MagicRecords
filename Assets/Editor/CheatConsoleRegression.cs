#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using Object = UnityEngine.Object;

public sealed class CheatRegressionEnemy : Enemy
{
    public int Deaths;
    protected override void Awake()
    {
        state = GetComponent<StateComponent>();
        healthPoint = GetComponent<HealthPointComponent>();
    }
    protected override void Start() { }
    protected override void OnDisable() { }
    protected override void OnDamageDeath() => Deaths++;
}

[InitializeOnLoad]
public static class CheatConsoleRegression
{
    private const string Pending = "CheatConsoleRegression.Pending";
    private const string Backup = "CheatConsoleRegression.Scenes";
    private const string Report = "Library/CheatConsoleRegression-result.txt";
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int checks;
    [Serializable] private sealed class SceneBackup { public SceneSetup[] scenes; }

    static CheatConsoleRegression()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                Run().Forget();
            }
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetString(Backup, "") != "")
            {
                var scenes = JsonUtility.FromJson<SceneBackup>(SessionState.GetString(Backup, ""));
                SessionState.EraseString(Backup);
                EditorSceneManager.RestoreSceneManagerSetup(scenes.scenes);
            }
        };
    }

    [MenuItem("Tools/Cheats/Run Console Regression")]
    public static void Start()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run in Edit Mode.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save open scenes before running the regression.");
        SessionState.SetString(Backup, JsonUtility.ToJson(new SceneBackup { scenes = EditorSceneManager.GetSceneManagerSetup() }));
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
    private static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Fields).SetValue(owner, value);
    private static void Press(Keyboard keyboard, params Key[] keys)
    { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update(); }

    private static async UniTaskVoid Run()
    {
        File.WriteAllText(Report, "Cheat console regression (isolated empty scene; no player saves)\n");
        checks = 0;
        Keyboard keyboard = null;
        var owned = new List<GameObject>();
        var oldUpdateMode = InputSystem.settings.updateMode;
        try
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            var console = Object.FindAnyObjectByType<CheatConsoleUI>();
            Check(console != null, "Console prefab bootstraps automatically");
            var cameraObject = new GameObject("Preview camera", typeof(Camera)); owned.Add(cameraObject);
            cameraObject.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            cameraObject.GetComponent<Camera>().backgroundColor = new Color(.08f, .11f, .15f);
            var events = new GameObject("Console test events", typeof(EventSystem), typeof(InputSystemUIInputModule)); owned.Add(events);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var playerObject = new GameObject("Input fixture"); playerObject.SetActive(false); owned.Add(playerObject);
            var input = playerObject.AddComponent<PlayerInput>();
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = actions.AddActionMap("Player");
            map.AddAction("Move", InputActionType.Button, "<Keyboard>/w");
            map.AddAction("DisabledAction", InputActionType.Button, "<Keyboard>/q");
            input.actions = actions; input.defaultActionMap = "Player";
            playerObject.SetActive(true);
            var move = input.actions.FindAction("Player/Move");
            var disabled = input.actions.FindAction("Player/DisabledAction"); disabled.Disable();
            int moveRequests = 0; move.performed += _ => moveRequests++;
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            Press(keyboard, Key.Backquote);
            console.SendMessage("Update");
            Press(keyboard);
            Check(CheatConsoleUI.CapturesInput && Time.timeScale == 0, "Backquote opens console and pauses battle");
            Check(!move.enabled && !disabled.enabled, "Player actions are suspended while typing");
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            var field = console.GetComponentInChildren<TMP_InputField>(true);
            Check(field.isFocused, "Command field receives keyboard focus");
            Press(keyboard, Key.W); Press(keyboard);
            Check(moveRequests == 0, "Typing movement keys does not send gameplay actions");
            field.text = " HELP ";
            Press(keyboard, Key.Enter);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            Press(keyboard);
            Check(field.text == "", "Enter submission clears command field");
            Check(CheatConsoleUI.ExecuteCommand("unknown").Contains("알 수 없는"), "Unknown command reports an error");
            Check(CheatConsoleUI.ExecuteCommand("killall").Contains("없습니다"), "No enemies is handled safely");

            PauseManager.RequestPause();
            Press(keyboard, Key.Escape); console.SendMessage("Update"); Press(keyboard);
            Check(Time.timeScale == 0 && move.enabled && !disabled.enabled, "Close restores only enabled actions and preserves another pause owner");
            PauseManager.RequestResume();
            Check(Time.timeScale == 1, "All pause owners release correctly");
            console.Open();
            console.gameObject.SetActive(false);
            Check(move.enabled && Time.timeScale == 1, "Disabling console restores input and pause");
            console.gameObject.SetActive(true);

            var enemyObject = new GameObject("Enemy fixture"); enemyObject.SetActive(false); owned.Add(enemyObject);
            enemyObject.AddComponent<StatusEffectComponent>();
            var state = enemyObject.AddComponent<StateComponent>();
            var hp = enemyObject.AddComponent<HealthPointComponent>();
            var enemy = enemyObject.AddComponent<CheatRegressionEnemy>();
            enemyObject.SetActive(true); hp.SetHealthPoint(100);
            int hpEvents = 0; hp.OnChangedHP_TwoParam += (_, _) => hpEvents++;
            hp.Damage(60); hp.Heal(100);
            Check(hp.GetCurrentHP == 100 && hpEvents == 2, "Healing clamps to max HP and emits health updates");
            var battleObject = new GameObject("Battle fixture"); battleObject.SetActive(false); owned.Add(battleObject);
            var battle = battleObject.AddComponent<BattleManager>();
            battle.ResistEnemy(enemy);
            int deaths = 0; enemy.OnDead += character => { deaths++; battle.UnreistEnemy(character); };
            Check(battle.KillCurrentEnemiesForCheat() == 1 && hp.Dead && state.DeadMode && deaths == 1 && enemy.Deaths == 1,
                "BattleManager kills through death callbacks even when callbacks modify its list");
            Check(battle.KillCurrentEnemiesForCheat() == 0 && !enemy.KillForCheat() && deaths == 1, "Repeated kill does not duplicate death events");
            hp.Heal(100); Check(hp.Dead, "Heal does not revive dead enemies");
            enemyObject.SetActive(false);

            var exploreObject = new GameObject("Explore fixture"); owned.Add(exploreObject);
            var explore = exploreObject.AddComponent<ExploreManager>();
            Set(explore, "<RunStatus>k__BackingField", RunStatus.MidRun);
            var savedMap = new MapData { nodes = new List<MapNode> {
                new() { id = 0, level = 0, nextNodeIds = new List<int> { 1 } },
                new() { id = 1, level = 1, nextNodeIds = new List<int> { 2 } },
                new() { id = 2, level = 2, nextNodeIds = new List<int> { 3 } },
                new() { id = 3, level = 3 } } };
            var savedNodes = new StageNodeData { nodeInfos = new List<MapNodeInfo> {
                new() { nodeId = 1, type = StageType.Combat },
                new() { nodeId = 2, type = StageType.Shop },
                new() { nodeId = 3, type = StageType.Boss_Combat } } };
            explore.StageReplacer.RestoreStages(1, savedMap, savedNodes);
            Check(!explore.CanEnableNode(3), "Boss is initially locked");
            Check(explore.StageReplacer.UnlockFinalBossForCheat() && explore.CanEnableNode(3) &&
                explore.GetNodeState(3) == MapNodeState.Selectable && !explore.CanEnableNode(2), "Only final boss becomes selectable without clearing other nodes");
            Set(explore, "<RunStatus>k__BackingField", RunStatus.ChapterCleared);
            Check(!explore.CanEnableNode(3), "Boss cheat respects completed chapter state");
            Set(explore, "<RunStatus>k__BackingField", RunStatus.MidRun);
            explore.StageReplacer.RestoreStages(1, savedMap, savedNodes);
            Check(!explore.CanEnableNode(3), "Map reload resets transient boss unlock");
            var health = new ExploreHealthState { characterId = 1, current = 30, maximum = 100 };
            Set(explore, "runHealth", new List<ExploreHealthState> { health });
            Check(explore.HealPartyForCheat() && health.current == 100, "Map-screen party healing updates stored run HP");

            console.Open();
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Library/CheatConsole-preview.png"));
            await UniTask.Delay(300, ignoreTimeScale: true);
            console.Close();
            Object.Destroy(actions);
            File.AppendAllText(Report, $"SUCCESS: {checks} checks\n");
        }
        catch (Exception error) { File.AppendAllText(Report, error + "\n"); Debug.LogException(error); }
        finally
        {
            InputSystem.settings.updateMode = oldUpdateMode;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            foreach (var obj in owned) if (obj != null) Object.Destroy(obj);
            PauseManager.Reset();
            EditorApplication.ExitPlaymode();
        }
    }
}
#endif
