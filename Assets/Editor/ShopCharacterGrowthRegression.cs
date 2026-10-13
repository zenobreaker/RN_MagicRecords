#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ShopCharacterGrowthRegression
{
    const string Request = "Library/ShopCharacterGrowth.request";
    const string Report = "Library/ShopCharacterGrowth-result.txt";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static ShopCharacterGrowthRegression() => EditorApplication.update += Tick;
    static void Tick()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        File.Delete(Request);
        try { Run(); }
        catch (Exception e) { File.AppendAllText(Report, "FAIL " + e + "\n"); Debug.LogException(e); }
    }
    static void Check(bool condition, string message)
    {
        File.AppendAllText(Report, (condition ? "PASS " : "FAIL ") + message + "\n");
        if (!condition) throw new Exception(message);
    }
    static void Run()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("ShopRegression.SaveDirectory", "")))
            throw new Exception("Requires sandbox Play Mode.");
        File.WriteAllText(Report, "Unity " + Application.unityVersion + " actual Play Mode\n");
        var app = AppManager.Instance; var explore = app.GetExploreManager(); var players = PlayerManager.Instance;
        var ui = UIManager.Instance; var coin = CurrencyManager.Instance;
        var source = players.GetCharacterStatus(1);
        string original = JsonUtility.ToJson(source);
        Check(source != null && source.hp.growth > 0 && source.attack.growth > 0 && source.defense.growth > 0,
            "selected character has authored HP, attack and defense growth data");
        app.EnterTheExplorationProcess();
        explore.FinallizeSetupAndGenerateMap(new ExplorationSetupData { SelectedCharacterId = 1, SelectedClassId = 1 });
        ui.CloseAllOpenedUI();
        var levels = explore.StageReplacer.GetLevels();
        typeof(ExploreManager).GetField("<MapNodeID>k__BackingField", Hidden).SetValue(explore, levels[levels.Count - 2][0].id);
        var node = explore.GetReplacedNodeInfo(); var stock = explore.GetShopStock(node);
        var offer = stock.offers.Single(o => o.kind == ExploreShopKind.CharacterLevelUp);
        Check(offer.price == 10 && !offer.sold, "exactly one character level-up product costs 10 exploration coins");
        var shop = ui.OpenUI<ShopUI>(); shop.SetExploreShop(node);
        var slots = shop.GetComponentsInChildren<UIShopSlot>();
        var skillSlot = slots[stock.offers.FindIndex(o => o.kind == ExploreShopKind.SkillSwap)];
        var levelSlot = slots[stock.offers.IndexOf(offer)];
        Check(!skillSlot.GetComponentInChildren<UICurrency>(true).gameObject.activeSelf,
            "skill replacement slot hides both currency icon and price");
        Check(levelSlot.GetComponentInChildren<UICurrency>(true).gameObject.activeSelf,
            "character level-up slot still shows its price");
        skillSlot.SetOffer(null, "fixture paid offer", 10, false, () => { });
        Check(skillSlot.GetComponentInChildren<UICurrency>(true).gameObject.activeSelf,
            "reused slot restores price for a paid product");
        skillSlot.SetOffer(null, "fixture skill", 0, false, () => { }, false);
        skillSlot.SetItemData(app.GetShopItems(ItemCategory.EQUIPMENT).First());
        Check(skillSlot.GetComponentInChildren<UICurrency>(true).gameObject.activeSelf,
            "reused lobby slot restores its original currency display");
        shop.SetExploreShop(node);
        coin.ClearExploreCurrency();
        levelSlot.GetComponent<Button>().onClick.Invoke();
        Check(Object.FindFirstObjectByType<UIPopUpShop>() == null && players.GetRunCharacterStatus(1).level == source.level,
            "insufficient currency does not open a purchase popup or change level");
        coin.AddCurrency(CurrencyType.EXPOLORE_COIN, 10);
        levelSlot.GetComponent<Button>().onClick.Invoke();
        var popup = Object.FindFirstObjectByType<UIPopUpShop>();
        Check(popup != null, "level-up slot opens existing purchase popup");
        popup.CloseUI();
        Check(coin.GetCurrency(CurrencyType.EXPOLORE_COIN) == 10 && players.GetRunCharacterStatus(1).level == source.level,
            "canceling purchase preserves currency and level");
        levelSlot.GetComponent<Button>().onClick.Invoke();
        popup = Object.FindFirstObjectByType<UIPopUpShop>();
        coin.ClearExploreCurrency(); popup.OnSubmit();
        Check(popup.isActiveAndEnabled && !offer.sold && players.GetRunCharacterStatus(1).level == source.level,
            "confirmation rechecks funds before leveling");
        coin.AddCurrency(CurrencyType.EXPOLORE_COIN, 10); popup.OnSubmit(); popup.OnSubmit();
        var grown = players.GetRunCharacterStatus(1);
        Check(grown.level == source.level + 1 && coin.GetCurrency(CurrencyType.EXPOLORE_COIN) == 0,
            "confirmation raises only selected run character one level and deducts exactly 10 coins");
        foreach (var type in new[] { StatusType.HEALTH, StatusType.ATTACK, StatusType.DEFENSE })
            Check(Mathf.Approximately(grown.GetStatusValue(type) - source.GetStatusValue(type), 2.5f),
                type + " follows current authored growth (+2.5 per level)");
        foreach (var type in new[] { StatusType.ATTACKSPEED, StatusType.MOVESPEED, StatusType.CRIT_RATIO, StatusType.CRIT_DMG })
            Check(Mathf.Approximately(grown.GetStatusValue(type), source.GetStatusValue(type)), type + " retains authored zero growth");
        Check(JsonUtility.ToJson(source) == original && !ReferenceEquals(source, grown),
            "lobby/source character data remains unchanged");
        Check(offer.sold && offer.purchases == 1 && !explore.BuyShopOffer(node, offer),
            "one purchase per shop policy prevents duplicate grants");
        Check(!levelSlot.GetComponent<Button>().interactable &&
            levelSlot.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text == "판매 완료"),
            "purchased product shows sold out and cannot be clicked");
        var saved = SaveManager.LoadExploreRun();
        Check(saved.characterLevels.Single(l => l.charId == 1).charLevel == grown.level,
            "purchase immediately saves selected run character level");
        shop.CloseUI();
        SaveManager.SaveExploreRun(saved); explore.ResetData(); explore.Init(false);
        var restored = explore.GetShopStock(explore.GetReplacedNodeInfo()).offers.Single(o => o.kind == ExploreShopKind.CharacterLevelUp);
        Check(players.GetRunCharacterStatus(1).level == source.level + 1 && restored.sold && restored.purchases == 1,
            "save reload restores level, growth and sold state");
        // Verify the same status API used by Player.SetStatus when entering a battle.
        var statusObject = new GameObject("Growth status fixture");
        try
        {
            var status = statusObject.AddComponent<StatusComponent>();
            status.ApplyBuff(new StatModifier(StatusType.ATTACK, 7, ModifierValueType.FIXED));
            status.SetStatusData(players.GetRunCharacterStatus(1));
            Check(Mathf.Approximately(status.GetStatusValue(StatusType.ATTACK), grown.attack.GetValue(source.level + 1) + 7),
                "battle status application uses grown stats and preserves existing modifiers");
        }
        finally { Object.DestroyImmediate(statusObject); }
        CheckMaxLevel();
        File.AppendAllText(Report, "SHOP_CHARACTER_GROWTH_REGRESSION_PASS\n");
    }

    static void CheckMaxLevel()
    {
        var app = AppManager.Instance; var explore = app.GetExploreManager(); var players = PlayerManager.Instance;
        var coin = CurrencyManager.Instance; var node = explore.GetReplacedNodeInfo();
        var offer = node.shopStock.offers.Single(o => o.kind == ExploreShopKind.CharacterLevelUp);
        void Restore(int level) => players.RestoreRunCharacterLevels(new() { new CharacterSaveData { charId = 1, charLevel = level } });
        Restore(19); offer.purchases = 0; offer.sold = false;
        var shop = UIManager.Instance.OpenUI<ShopUI>(); shop.SetExploreShop(node);
        var slot = shop.GetComponentsInChildren<UIShopSlot>()[node.shopStock.offers.IndexOf(offer)];
        coin.ClearExploreCurrency(); coin.AddCurrency(CurrencyType.EXPOLORE_COIN, 20);
        slot.GetComponent<Button>().onClick.Invoke();
        var popup = Object.FindFirstObjectByType<UIPopUpShop>();
        Check(popup != null && players.CanLevelUpRunCharacter(1), "level 19 may open purchase popup");
        Restore(20); popup.OnSubmit();
        Check(coin.GetCurrency(CurrencyType.EXPOLORE_COIN) == 20 && !offer.sold && popup.isActiveAndEnabled,
            "confirmation rechecks level cap and charges nothing after reaching 20");
        Restore(19); popup.OnSubmit();
        Check(players.GetRunCharacterStatus(1).level == 20 && coin.GetCurrency(CurrencyType.EXPOLORE_COIN) == 10,
            "level 19 purchase reaches exactly 20 for 10 coins");
        Check(!slot.GetComponent<Button>().interactable && slot.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text == "최대 레벨"),
            "max-level shop product is labeled and disabled");
        Check(!players.CanLevelUpRunCharacter(1) && !players.TryLevelUpRunCharacter(1), "direct level-up API cannot exceed 20");
        offer.purchases = 0; offer.sold = false;
        Check(!explore.BuyShopOffer(node, offer) && coin.GetCurrency(CurrencyType.EXPOLORE_COIN) == 10,
            "unsold product at another shop cannot charge a max-level character");
        Restore(99);
        Check(players.GetRunCharacterStatus(1).level == 20 && players.CaptureRunCharacterLevels().Single().charLevel == 20,
            "legacy saves above cap restore and save at 20");
        Check(Mathf.Approximately(players.GetRunCharacterStatus(1).GetStatusValue(StatusType.HEALTH), 150),
            "restored stats use level 20 growth values");
        shop.CloseUI();
    }
}
#endif
