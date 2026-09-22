#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

// Runs only on an explicit request, in the existing isolated-save Play Mode sandbox.
[InitializeOnLoad]
public static class SkillCooldownRegression
{
    const string Request = "Library/SkillCooldown.request";
    const string Report = "Library/SkillCooldown-result.txt";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static SkillCooldownRegression() => EditorApplication.update += Tick;
    static void Tick()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string action;
        try { action = File.ReadAllText(Request).Trim(); File.Delete(Request); }
        catch (IOException) { return; }
        try
        {
            if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("ShopRegression.SaveDirectory", "")))
                throw new Exception("Requires sandbox Play Mode.");
            if (action == "probe") Run(true);
            if (action == "checks") Run(false);
            if (action == "stage") EnterStage();
            if (action == "stage-checks") StageChecks().Forget();
        }
        catch (Exception e) { File.AppendAllText(Report, e + "\n"); Debug.LogException(e); }
    }
    static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Hidden).GetValue(target);
    static void Step(SkillComponent component) => typeof(SkillComponent).GetMethod("Update", Hidden).Invoke(component, null);
    static void StartCooldown(ActiveSkill skill) => typeof(ActiveSkill).GetMethod("SetCooldown", Hidden).Invoke(skill, null);
    static void Check(bool condition, string message)
    {
        File.AppendAllText(Report, (condition ? "PASS " : "FAIL ") + message + "\n");
        if (!condition) throw new Exception(message);
    }
    [MenuItem("Tools/Skill/Check Player HUD Cooldowns (Sandbox Play Mode)")]
    public static void RunChecks() => Run(false);
    static void Run(bool probe)
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("ShopRegression.SaveDirectory", "")))
            throw new Exception("Requires sandbox Play Mode.");
        File.WriteAllText(Report, $"Unity {Application.unityVersion} Play Mode; {(probe ? "pre-fix reproduction" : "cooldown regression")}\n");
        var manager = SkillManager.Instance;
        var previous = manager.GetActiveSkillList(1).ToArray();
        var cached = manager.SkillEventHandler.CurrentActiveSkills.ToArray();
        var playerRoot = new GameObject("Cooldown player fixture"); playerRoot.SetActive(false);
        playerRoot.AddComponent<Player>();
        var player = playerRoot.AddComponent<SkillComponent>();
        var enemyRoot = new GameObject("Cooldown enemy fixture"); enemyRoot.SetActive(false);
        enemyRoot.AddComponent<Character>();
        var enemy = enemyRoot.AddComponent<SkillComponent>();
        var canvas = new GameObject("Cooldown HUD fixture", typeof(Canvas));
        var data = ScriptableObject.CreateInstance<SO_ActiveSkillData>();
        data.levelDatas = new List<SkillLevelData> { new SkillLevelData { cooldown = 10, damageData = new DamageData { hitData = new HitData() } } };
        data.phaseList = new List<PhaseSkill> { new PhaseSkill() };
        int enemyStates = 0, enemyProgress = 0;
        enemy.OnActiveSkillCooldownChanged += (_, __) => enemyStates++;
        enemy.OnActiveSkillCooldownUpdated += (_, __, ___) => enemyProgress++;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/5.Prefabs/UI/PlayerUI_PC.prefab");
            foreach (SkillSlot slot in new[] { SkillSlot.SLOT2, SkillSlot.SLOT1, SkillSlot.SLOT3, SkillSlot.SLOT4 })
            {
                var source = prefab.GetComponentsInChildren<SkillSlotUI>(true).Single(s => Field<SkillSlot>(s, "mySlot") == slot);
                manager.EquipActiveSkill(1, (int)slot - (int)SkillSlot.SLOT1, new SkillRuntimeData { template = data, currentLevel = 1 });
                var hud = Object.Instantiate(source, canvas.transform); hud.gameObject.SetActive(true);
                var overlay = Field<Image>(hud, "img_Cooldown");
                var label = Field<TextMeshProUGUI>(hud, "txt_Cooldown");
                var playerSkill = (ActiveSkill)data.CreateSkill();
                var enemySkill = (ActiveSkill)data.CreateSkill();
                player.SetActiveSkill(slot, playerSkill);
                // The AI uses the string overload, including SLOT2 in eight existing enemy prefabs.
                enemy.SetActiveSkill(slot.ToString(), enemySkill);
                StartCooldown(playerSkill); Step(player);
                Check(overlay.gameObject.activeInHierarchy && label.gameObject.activeInHierarchy && overlay.fillAmount > .9f,
                    slot + " player cooldown appears on real HUD prefab");
                Step(enemy);
                if (probe)
                {
                    File.AppendAllText(Report, $"REPRODUCED={(!overlay.gameObject.activeInHierarchy && playerSkill.IsOnCooldown)}: idle enemy Update hides SLOT2 while player has {playerSkill.CurrentCooldown:F2}s remaining.\n");
                    return;
                }
                Check(overlay.gameObject.activeInHierarchy && label.gameObject.activeInHierarchy, slot + " idle enemy cannot hide player cooldown");
                float playerFill = overlay.fillAmount;
                StartCooldown(enemySkill); enemySkill.Update_Cooldown(7); Step(enemy);
                Check(Mathf.Approximately(overlay.fillAmount, playerFill), slot + " enemy cooldown cannot overwrite player progress");
                enemy.SetActiveSkill(slot, enemySkill);
                Check(manager.SkillEventHandler.CurrentActiveSkills[(int)slot] == playerSkill, slot + " enemy registration cannot replace player HUD cache");
                playerSkill.Update_Cooldown(4);
                hud.gameObject.SetActive(false); overlay.fillAmount = 0;
                hud.gameObject.SetActive(true);
                Check(overlay.gameObject.activeInHierarchy && Mathf.Approximately(overlay.fillAmount, playerSkill.CurrentCooldown / playerSkill.MaxCooldown),
                    slot + " HUD re-enable immediately restores current player cooldown");
                playerSkill.Update_Cooldown(100); Step(player); Step(enemy);
                Check(!overlay.gameObject.activeInHierarchy && !label.gameObject.activeInHierarchy, slot + " player expiry hides overlay despite enemy cooldown");
                Object.DestroyImmediate(hud.gameObject);
            }
            Check(enemyStates > 0 && enemyProgress > 0, "enemy local cooldown events and simulation remain active");
            Debug.Log("SKILL_COOLDOWN_REGRESSION_PASS");
        }
        finally
        {
            Object.DestroyImmediate(canvas); Object.DestroyImmediate(playerRoot); Object.DestroyImmediate(enemyRoot);
            for (int i = 0; i < previous.Length; i++) manager.EquipActiveSkill(1, i, previous[i]);
            Array.Copy(cached, manager.SkillEventHandler.CurrentActiveSkills, cached.Length);
            Object.DestroyImmediate(data);
        }
    }
    static void EnterStage()
    {
        var app = AppManager.Instance;
        var explore = app.GetExploreManager();
        var skills = SkillTreeManager.Instance.GetAvailableSkills(1).Where(s=>s.template is SO_ActiveSkillData).Take(4).ToArray();
        for (int i=0;i<skills.Length;i++) { skills[i].currentLevel = 1; app.EquipActiveSkill(1,i,skills[i]); }
        explore.StartExplore();
        explore.FinallizeSetupAndGenerateMap(new ExplorationSetupData { SelectedCharacterId=1, SelectedClassId=1 });
        explore.ConsumeInitialRecordReward();
        var combat = explore.StageReplacer.GetNodeToInfo().Values.First(n=>n.type == StageType.Combat);
        NodeRouter.EnterNode(explore.Chapter, combat);
    }
    static async UniTaskVoid StageChecks()
    {
        try
        {
            Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Stage" && !SceneLoadingController.IsLoading,
                "integration runs in the loaded Stage scene");
            var player = Object.FindFirstObjectByType<Player>();
            var component = player.GetComponent<SkillComponent>();
            var enemies = Object.FindObjectsByType<SkillComponent>(FindObjectsSortMode.None)
                .Where(c=>c.GetComponentInParent<Player>() == null && c.TryGetRegisteredActiveSkill(SkillSlot.SLOT2, out _)).ToArray();
            Check(enemies.Length > 0, "Stage has active enemy SLOT2 skills: " + enemies.Length);
            Check(component.TryGetRegisteredActiveSkill(SkillSlot.SLOT2, out var skill), "Stage player has SLOT2 equipped");
            var hud = Object.FindObjectsByType<SkillSlotUI>(FindObjectsSortMode.None).Single(s=>Field<SkillSlot>(s, "mySlot") == SkillSlot.SLOT2);
            var overlay = Field<Image>(hud, "img_Cooldown");
            var label = Field<TextMeshProUGUI>(hud, "txt_Cooldown");
            component.CancelCurrentSkill();
            skill.Update_Cooldown(1000);
            player.GetComponent<StateComponent>().SetIdleMode();
            component.UseSkill(SkillSlot.SLOT2);
            Check(skill.IsOnCooldown && overlay.gameObject.activeInHierarchy, "actual Stage SLOT2 UseSkill immediately shows cooldown");
            int frames = 0;
            float first = skill.CurrentCooldown;
            for (int i = 0; i < 30; i++)
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                if (skill.CurrentCooldown <= 0) break;
                if (!overlay.gameObject.activeInHierarchy || !label.gameObject.activeInHierarchy ||
                    !Mathf.Approximately(overlay.fillAmount, Mathf.Clamp01(skill.CurrentCooldown / skill.MaxCooldown)))
                    throw new Exception("Stage player cooldown overwritten at frame " + i);
                frames++;
            }
            Check(frames >= 10 && skill.CurrentCooldown < first, "actual frame updates preserve player fill/text with live enemies: " + frames + " frames");
            File.AppendAllText(Report, $"Stage skill: {skill.SkillID} {skill.Name}; remaining={skill.CurrentCooldown:F2}; max={skill.MaxCooldown:F2}\n");
            Debug.Log("SKILL_COOLDOWN_STAGE_PASS");
        }
        catch (Exception e) { File.AppendAllText(Report, "FAIL " + e + "\n"); Debug.LogException(e); }
    }
}
#endif
