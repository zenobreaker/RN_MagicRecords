#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit sandbox request only. Does not change authored scenes/assets or user saves.
[InitializeOnLoad]
public static class CurseShopRegression
{
    const string Request = "Library/CurseShop.request";
    const string Report = "Library/CurseShop-result.txt";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static CurseShopRegression() => EditorApplication.update += Tick;
    static void Tick()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        File.Delete(Request);
        try { Run(); }
        catch (Exception e) { File.AppendAllText(Report, "FAIL " + e + "\n"); Debug.LogException(e); }
    }
    static void Check(bool ok, string message)
    {
        File.AppendAllText(Report, (ok ? "PASS " : "FAIL ") + message + "\n");
        if (!ok) throw new Exception(message);
    }
    static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, Hidden).GetValue(obj);
    public static void Run()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("ShopRegression.SaveDirectory", "")))
            throw new Exception("Requires sandbox Play Mode.");
        File.WriteAllText(Report, "Unity " + Application.unityVersion + " actual Play Mode\n");
        var objects = new List<GameObject>();
        var skills = new List<PassiveSkill>();
        GameObject Actor(string name)
        {
            var go = new GameObject(name); objects.Add(go);
            go.AddComponent<HealthPointComponent>().SetHealthPoint(1000);
            go.AddComponent<StatusComponent>().SetStatusValue(StatusType.ATTACK, 100);
            // Damage handler's standalone path lets the fixture use actual HP/effect APIs without animation.
            go.AddComponent<DamageHandleComponent>();
            go.AddComponent<CurseRegressionActor>();
            go.AddComponent<EffectComponent>();
            return go;
        }
        try
        {
            var owner = Actor("Curse owner"); var target = Actor("Curse target"); var stranger = Actor("Other attacker");
            var status = owner.GetComponent<StatusComponent>();
            var hp = target.GetComponent<HealthPointComponent>();
            var effects = target.GetComponent<EffectComponent>();
            var damage = target.GetComponent<DamageHandleComponent>();
            PassiveSkill Load(string name)
            {
                var so = AssetDatabase.LoadAssetAtPath<SO_PassiveSkillData>("Assets/10.ScriptableObjects/Skills/Shooter/Passive/forbidden-" + name + ".asset");
                Check(so != null && so.Modules.Count > 0 && so.Modules.All(m => m != null), name + " asset resolves executable modules");
                var skill = (PassiveSkill)so.CreateSkill(); skill.SetLevel(1); skill.OnAcquire(owner); skills.Add(skill); return skill;
            }
            var grief = Load("grief");
            hp.RestoreCurrentHealth(500); damage.OnDamage(owner, new DamageEvent(10));
            Check(Mathf.Approximately(hp.GetCurrentHP, 480), "grief adds 2% of target missing HP through actual damage calculation");
            var foreign = new DamageEvent(10); BattleManager.Instance.NotifyAttackHit(stranger, target, foreign);
            Check(foreign.MissingHPRatio == 0, "grief does not amplify another attacker's hit");
            grief.OnLose();

            var curse = Load("magic"); curse.OnApplyStaticEffect(status); curse.OnApplyStaticEffect(status);
            Check(Mathf.Approximately(status.GetStatusValue(StatusType.ATTACK), 110) &&
                Mathf.Approximately(status.GetStatusValue(StatusType.CRIT_RATIO), .05f), "curse stat bonuses apply once");
            hp.RestoreCurrentHealth(500); damage.OnDamage(owner, new DamageEvent(10));
            Check(effects.HasEffect("Curse") is CurseEffect && Mathf.Approximately(hp.GetCurrentHP, 488), "curse applies and increases direct damage by 20%");
            curse.OnLose();
            Check(Mathf.Approximately(status.GetStatusValue(StatusType.ATTACK), 100), "losing curse removes stat bonus");

            var hatred = Load("hatred");
            hp.RestoreCurrentHealth(500); BattleManager.Instance.NotifyAttackHit(owner, target, new DamageEvent(10) { AttackInstanceID = 101 });
            var dot = effects.HasEffect("Curse_Hatred");
            Check(dot is HatredEffect && dot.FxIcon != null && Mathf.Approximately(hp.GetCurrentHP, 480), "hatred applies existing DOT and icon with 20% attack damage");
            foreach (var action in dot.Actions) action.Execute(target, owner, dot.StackCount);
            Check(Mathf.Approximately(hp.GetCurrentHP, 460), "hatred subsequent tick deals damage without recursive procs");
            effects.RemoveEffect(dot);
            BattleManager.Instance.NotifyAttackHit(owner, target, new DamageEvent(10) { AttackInstanceID = 102 });
            Check(effects.HasEffect("Curse_Hatred") == null, "hatred cooldown blocks a new attack");
            BattleManager.Instance.NotifyAttackHit(owner, target, new DamageEvent(10) { AttackInstanceID = 101 });
            Check(effects.HasEffect("Curse_Hatred") != null, "hatred retains same-attack multi-target allowance");
            effects.ClearEffects(); hatred.OnLose(); hatred.OnAcquire(owner);
            BattleManager.Instance.NotifyAttackHit(owner, target, new DamageEvent(10));
            Check(effects.HasEffect("Curse_Hatred") == null, "hatred requires curse on target");
            hatred.OnLose();

            var wrath = Load("anger"); wrath.SetLevel(3); wrath.OnApplyStaticEffect(status);
            effects.ApplyEffect(new CurseEffect("Curse", "", 5), target, owner);
            var amplified = new DamageEvent(10); BattleManager.Instance.NotifyAttackHit(owner, target, amplified);
            Check(Mathf.Approximately(amplified.DamageAmp, 1.1f) && Mathf.Approximately(status.GetStatusValue(StatusType.CRIT_DMG), .2f),
                "wrath uses debuff count and existing level-three crit bonus");
            wrath.OnLose();

            // All four listeners together: own hit only, DOT excluded, clean unsubscribe.
            foreach (var skill in skills) skill.OnAcquire(owner);
            effects.ClearEffects();
            var enemyHit = new DamageEvent(10); BattleManager.Instance.NotifyAttackHit(stranger, target, enemyHit);
            Check(effects.DebuffCount == 0 && enemyHit.MissingHPRatio == 0 && enemyHit.DamageAmp == 0, "curse passives cannot strengthen enemy or unrelated attacks");
            var dotHit = new DamageEvent(10); dotHit.hitData.DamageType = DamageType.DOT_HATERD;
            BattleManager.Instance.NotifyAttackHit(owner, target, dotHit);
            Check(effects.DebuffCount == 0 && dotHit.MissingHPRatio == 0 && dotHit.DamageAmp == 0, "DOT cannot retrigger or extend curse passives");
            foreach (var skill in skills) skill.OnLose();
            var removedHit = new DamageEvent(10); BattleManager.Instance.NotifyAttackHit(owner, target, removedHit);
            Check(effects.DebuffCount == 0 && removedHit.MissingHPRatio == 0, "removed passives unsubscribe from battle events");
            CheckShop();
            // Run the existing transaction/state regressions independently of localization catalog checks.
            foreach (string method in new[] { "RunShopIsolation", "RunDevelopmentLocks" })
            {
                int count = (int)typeof(SkillEventRegression).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                File.AppendAllText(Report, "PASS " + method + ": " + count + " existing checks\n");
            }
            File.AppendAllText(Report, "CURSE_SHOP_REGRESSION_PASS\n");
        }
        finally
        {
            foreach (var skill in skills) skill.OnLose();
            foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
        }
    }
    static void CheckShop()
    {
        var app = AppManager.Instance; var explore = app.GetExploreManager(); var ui = UIManager.Instance;
        var eventDatabase = Object.FindFirstObjectByType<EventDataBase>();
        Check(eventDatabase.GetEventInfo(1012) == null && eventDatabase.GetEventInfo(1012, true) != null,
            "disabled event remains unavailable to events while shop can reuse its definition");
        Check(Enumerable.Range(0, 500).All(_ => eventDatabase.GetRandomEventID(1) != 1012),
            "disabled skill event never enters the random event pool");
        app.EnterTheExplorationProcess();
        explore.FinallizeSetupAndGenerateMap(new ExplorationSetupData { SelectedCharacterId = 1, SelectedClassId = 1 });
        ui.CloseAllOpenedUI();
        var levels = explore.StageReplacer.GetLevels();
        typeof(ExploreManager).GetField("<MapNodeID>k__BackingField", Hidden).SetValue(explore, levels[levels.Count - 2][0].id);
        var node = explore.GetReplacedNodeInfo();
        var stock = explore.GetShopStock(node); var offer = stock.offers.First(o => o.kind == ExploreShopKind.SkillSwap);
        Check(offer.price == 0, "new shop skill replacement price is zero");
        offer.price = 50; offer.sold = true;
        node.shopStock = JsonUtility.FromJson<ExploreShopStock>(JsonUtility.ToJson(stock));
        offer = explore.GetShopStock(node).offers.First(o => o.kind == ExploreShopKind.SkillSwap);
        Check(offer.price == 0 && !offer.sold, "old saved paid/sold skill offer migrates to free repeatable offer");
        var coin = CurrencyManager.Instance; coin.ClearExploreCurrency();
        Check(explore.CanBuyShopOffer(node, offer, false), "zero balance may open skill shop");
        var shop = ui.OpenUI<ShopUI>(); shop.SetExploreShop(node);
        shop.GetComponentsInChildren<UIShopSlot>()[node.shopStock.offers.IndexOf(offer)].GetComponent<Button>().onClick.Invoke();
        var popup = Object.FindFirstObjectByType<UIRecordSkillUpPopUp>();
        Check(popup != null, "clicking real shop slot opens skill popup with zero balance");
        var session = Field<SkillEventSession>(popup, "session");
        var candidate = Field<HashSet<int>>(popup, "candidates").First();
        Check(session != null && session.TryReplace(0, candidate, true) && session.PendingCost == 0, "actual shop popup permits free replacement with zero balance");
        Check(!session.TryUpgrade(candidate), "skill upgrade still requires currency");
        Check(session.Commit() && coin.GetCurrency(CurrencyType.EXPOLORE_COIN) == 0, "free replacement commits without currency deduction");
        Check(!session.Commit(), "duplicate confirmation cannot commit twice");
        popup.CloseUI();
        coin.AddCurrency(CurrencyType.EXPOLORE_COIN, 1000);
        popup = ui.OpenUI<UIRecordSkillUpPopUp>(true);
        popup.SetShopData(explore.ShopSwapChoice, 0, cost => explore.PayForShopSwap(node, offer, cost), null);
        session = Field<SkillEventSession>(popup, "session");
        int cost = session.UpgradeCost(candidate);
        Check(cost > 0 && session.TryUpgrade(candidate) && session.PendingCost == cost, "upgrade retains authored nonzero cost without entry surcharge");
        popup.CloseUI();
        Check(coin.GetCurrency(CurrencyType.EXPOLORE_COIN) == 1000, "canceling upgrade leaves currency intact");
        popup = ui.OpenUI<UIRecordSkillUpPopUp>(true);
        popup.SetShopData(explore.ShopSwapChoice, 0, amount => explore.PayForShopSwap(node, offer, amount), null);
        session = Field<SkillEventSession>(popup, "session");
        Check(session.TryUpgrade(candidate) && session.Commit() && coin.GetCurrency(CurrencyType.EXPOLORE_COIN) == 1000 - cost,
            "confirming upgrade deducts only upgrade price once");
        popup.CloseUI();
        shop.CloseUI();
    }
}

public sealed class CurseRegressionActor : Character
{
    protected override void Awake() { }
    protected override void Start() { }
}
#endif
