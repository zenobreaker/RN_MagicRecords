#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class CheatCommandPlayer : Player
{
    protected override void Awake() { }
    protected override void Start() { }
    protected override void OnDisable() { }
}

[InitializeOnLoad]
public static class CheatCommandRegression
{
    private const string Pending = "CheatCommandRegression.Pending";
    private const string Report = "Library/CheatCommandRegression-result.txt";
    private static int checks;

    static CheatCommandRegression()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            Run().Forget();
        };
    }

    [MenuItem("Tools/Cheats/Run Command Regression (Lobby)")]
    public static void Start()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "Lobby")
            throw new InvalidOperationException("Start from the Lobby scene in Edit Mode.");
        SessionState.SetBool(Pending, true);
        try { ShopRegression.StartSandbox(); }
        catch { SessionState.SetBool(Pending, false); throw; }
    }

    private static void Check(bool result, string message)
    {
        File.AppendAllText(Report, (result ? "PASS " : "FAIL ") + message + "\n");
        if (!result) throw new Exception(message);
        checks++;
    }
    private static string Execute(string command) => CheatConsoleUI.ExecuteCommand(command);

    private static async UniTaskVoid Run()
    {
        File.WriteAllText(Report, "Cheat command regression (isolated save folder)\n");
        checks = 0;
        GameObject playerObject = null;
        SO_ActiveSkillData external = null;
        try
        {
            await UniTask.Delay(500, ignoreTimeScale: true);
            Check(!string.IsNullOrEmpty(SessionState.GetString("ShopRegression.SaveDirectory", "")), "All test writes use an isolated save folder");
            var tree = SkillTreeManager.Instance;
            var skills = SkillManager.Instance;
            var currency = CurrencyManager.Instance;
            var app = AppManager.Instance;
            Check(tree != null && skills != null && currency != null && app != null, "Real lobby managers initialized");
            var available = tree.GetAvailableSkills(1).ToList();
            var first = available.First(s => s.template is SO_ActiveSkillData && s.GetMaxSkillLevel() > 1);
            var second = available.First(s => s.template is SO_ActiveSkillData && s != first);
            first.currentLevel = first.GetMaxSkillLevel(); first.isUnlocked = true;
            string learned = Execute("  LEARN_ALL_SKILLS  ");
            Check(learned.Contains("학습 완료") && available.All(s => s.currentLevel >= 1 && s.isUnlocked), "Lobby cheat learns all registered skills");
            Check(first.currentLevel == first.GetMaxSkillLevel(), "Learning all preserves already learned higher levels");
            Check(Execute("learn_all_skills").Contains("0개 갱신"), "Learning all is idempotent");
            var savedLearned = SaveManager.LoadSkillData();
            Check(savedLearned.skillSaveDatas.All(s => s.unlocked && s.skillLevel > 0), "Learned state persists through existing skill save");
            Check(Execute($"gain_skill {first.GetSkillID()} 1").Contains("인게임"), "Lobby rejects gain_skill");

            int gold = currency.GetCurrency(CurrencyType.GOLD), exploreCoin = currency.GetCurrency(CurrencyType.EXPOLORE_COIN);
            Execute("give_coin"); Check(currency.GetCurrency(CurrencyType.GOLD) == gold + 10000, "give_coin defaults to 10000");
            Execute("give_money 37"); Check(currency.GetCurrency(CurrencyType.GOLD) == gold + 10037, "give_money is the same ordinary currency");
            Execute(" GIVE_COIN   63 "); Check(currency.GetCurrency(CurrencyType.GOLD) == gold + 10100, "Command case and repeated whitespace are accepted");
            Execute("give_explore_coin"); Execute("give_explore_coin 19");
            Check(currency.GetCurrency(CurrencyType.EXPOLORE_COIN) == exploreCoin + 10019 && currency.GetCurrency(CurrencyType.GOLD) == gold + 10100,
                "Exploration currency default and explicit amounts are separate from gold");
            foreach (var value in new[] { "0", "-1", "1.5", "abc", "2147483648", "1 2" })
            {
                Check(Execute("give_coin " + value).StartsWith("사용법:"), "Invalid amount rejected: " + value);
            }
            Check(Execute("give_coin 2147483647").Contains("초과") && currency.GetCurrency(CurrencyType.GOLD) == gold + 10100,
                "Overflow is rejected without changing money");
            Check(File.Exists(Path.Combine(SessionState.GetString("ShopRegression.SaveDirectory", ""), "invetory.json")), "Currency changes reach inventory save");
            foreach (var command in new[] { "gain_skill", "gain_skill 1001", "gain_skill x 1", "gain_skill 1001 0", "gain_skill 1001 5",
                "gain_skill 1001 1 0", "gain_skill 1001 1 -2", "gain_skill 1001 1 2 3", "gain_record", "gain_record abc", "heal 1", "kill_all 1" })
                Check(Execute(command).StartsWith("사용법:"), "Invalid argument list rejected: " + command);
            Check(Execute("help give_money").Contains("give_coin") && Execute("help gain_skill").Contains("슬롯"), "Per-command help resolves aliases and arguments");

            var explore = app.GetExploreManager();
            explore.StartExplore();
            Check(explore.FinallizeSetupAndGenerateMap(new ExplorationSetupData { SelectedCharacterId = 1, SelectedClassId = 1 }), "Exploration setup completed");
            explore.ConsumeInitialRecordReward();
            var records = app.GetRecordManager();
            var record = records.GetShopRecord(1);
            Check(record != null, "Registered record ID 1 is available independently of shop filters");
            int recordCount = records.GetPossesRecord().Count;
            Check(Execute($"gain_record {record.id}").Contains("지급 완료") && records.GetPossesRecord().Any(r => r.id == record.id), "gain_record grants an actual registered record");
            Check(records.GetPossesRecord().Count == recordCount + 1 && SaveManager.LoadRecordData().recordIDs.Any(r => r.recordID == record.id), "Record grant is saved");
            Check(Execute($"gain_record {record.id}").Contains("중복 지급 규칙"), "Duplicate records follow existing fallback policy");
            recordCount = records.GetPossesRecord().Count;
            Check(Execute("gain_record 2147483647").Contains("존재하지") && records.GetPossesRecord().Count == recordCount, "Unknown record ID does not change inventory");

            var testScene = SceneManager.CreateScene("UnitTest"); SceneManager.SetActiveScene(testScene);
            playerObject = new GameObject("Cheat command player");
            playerObject.SetActive(false);
            var player = playerObject.AddComponent<CheatCommandPlayer>(); player.CharID = 1; player.JobID = 1;
            var component = playerObject.AddComponent<SkillComponent>();
            playerObject.SetActive(true); PlayerManager.Instance.SetCurrentPlayer(player);
            Check(Execute("learn_all_skills").Contains("로비에서만"), "Ingame rejects permanent learn-all cheat");
            int id = first.GetSkillID(); int lobbyLevel = first.currentLevel;
            Check(Execute($"gain_skill {id} 1").Contains("Lv.1") && skills.GetActiveSkillData(1, 0).currentLevel == 1,
                "Omitted level equips skill at level 1");
            Check(component.TryGetRegisteredActiveSkill(SkillSlot.SLOT1, out var live) && live.SkillLevel == 1 && live.SkillID == id,
                "Live player slot receives requested skill and level");
            Execute($"gain_skill {second.GetSkillID()} 2"); component.TryGetRegisteredActiveSkill(SkillSlot.SLOT2, out var untouched);
            Execute($"gain_skill {id} 1 2147483647");
            Check(skills.GetActiveSkillData(1, 0).currentLevel == first.GetMaxSkillLevel(), "Requested level is clamped to the skill maximum");
            Check(component.TryGetRegisteredActiveSkill(SkillSlot.SLOT2, out var unchanged) && ReferenceEquals(untouched, unchanged), "Unrelated live slots are not rebuilt");
            Execute($"gain_skill {id} 3 2");
            Check(skills.GetActiveSkillData(1, 0) == null && !component.TryGetRegisteredActiveSkill(SkillSlot.SLOT1, out _) &&
                component.TryGetRegisteredActiveSkill(SkillSlot.SLOT3, out live) && live.SkillID == id && live.SkillLevel == 2,
                "Re-granting an equipped skill moves it and applies explicit level");
            Check(first.currentLevel == lobbyLevel, "Ingame grants leave permanent lobby skill levels unchanged");
            var before = skills.GetActiveSkillData(1, 2);
            Check(Execute("gain_skill 2147483647 3").Contains("ID가 아닙니다") && skills.GetActiveSkillData(1, 2) == before,
                "Unknown skill ID does not replace an existing slot");
            var passive = available.First(s => s.template is SO_PassiveSkillData);
            Check(Execute($"gain_skill {passive.GetSkillID()} 3").Contains("ID가 아닙니다"), "Passive skills cannot be placed in active slots");

            external = Object.Instantiate((SO_ActiveSkillData)first.template); external.id = 9900001;
            var allTrees = (List<SkillTree>)typeof(SkillTreeManager).GetField("classSkillTreeList", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tree);
            allTrees.Add(new SkillTree { id = 99, allSkills = new List<SO_SkillData> { external } });
            Check(Execute("gain_skill 9900001 4 2").Contains("장착 완료"), "A registered skill outside the current job can be granted");
            var savedRun = SaveManager.LoadExploreRun();
            Check(savedRun.activeSkills.Any(s => s.slots.Contains(9900001)), "Granted skill is included in the run save");
            skills.ResetRunTimeData(); skills.RestoreRunSkills(savedRun.activeSkills); skills.SetActiveSkills(1, component);
            Check(skills.GetActiveSkillData(1, 2)?.GetSkillID() == id && skills.GetActiveSkillData(1, 2).currentLevel == 2 &&
                component.TryGetRegisteredActiveSkill(SkillSlot.SLOT4, out live) && live.SkillID == 9900001 && live.SkillLevel == 2,
                "Save restore preserves slot and level, including skills outside the current job");
            allTrees.RemoveAt(allTrees.Count - 1);
            File.AppendAllText(Report, $"SUCCESS: {checks} checks\n");
        }
        catch (Exception error) { File.AppendAllText(Report, error + "\n"); Debug.LogException(error); }
        finally
        {
            if (playerObject != null) Object.Destroy(playerObject);
            if (external != null) Object.Destroy(external);
            EditorApplication.ExitPlaymode();
        }
    }
}
#endif
