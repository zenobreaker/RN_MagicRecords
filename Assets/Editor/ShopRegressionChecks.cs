#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ShopRegressionChecks
{
    const string Report = "Library/ShopRegression-checks.txt";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message)
    {
        File.AppendAllText(Report, (condition ? "PASS " : "FAIL ") + message + "\n");
        if (!condition) throw new Exception(message);
    }
    static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Hidden).GetValue(target);
    static void Playing()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SessionState.GetString("ShopRegression.SaveDirectory", "")))
            throw new Exception("Run only in sandbox Play Mode (ShopRegression play request).");
    }
    [MenuItem("Tools/Shop/2 Check Lobby and Map (Play Mode)")]
    public static void Run()
    {
        Playing();
        File.WriteAllText(Report, "Unity " + Application.unityVersion + " Play Mode\n");
        var random = UnityEngine.Random.state;
        try
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                UnityEngine.Random.InitState(seed);
                var map = new NodeReplacer(); map.GenerateNodeMap();
                if (map.GetLevels().Count != 6 || !map.ValidateShopConnections()) throw new Exception("Map seed " + seed);
                var restored = new NodeReplacer();
                var data = new MapData { nodes = map.GetLevels().SelectMany(r => r).ToList() };
                restored.RestoreMap(JsonUtility.FromJson<MapData>(JsonUtility.ToJson(data)).nodes);
                if (!restored.ValidateShopConnections()) throw new Exception("Restored seed " + seed);
                var levels = map.GetLevels(); levels[3][0].nextNodeIds.Add(levels[5][0].id);
                if (map.ValidateShopConnections()) throw new Exception("Bypass undetected " + seed);
            }
            Check(true, "1000 seeds: six levels, <=3 nodes, mandatory shop, save/restore, injected bypass rejection");
        }
        finally { UnityEngine.Random.state = random; }
        var legacy = new NodeReplacer(); legacy.SetMaxNodeLevel(5); legacy.GenerateNodeMap();
        int oldBoss = legacy.GetLevels().Last()[0].id;
        var inserted = legacy.InsertShopBeforeBoss();
        Check(inserted != null && legacy.ValidateShopConnections() && legacy.IsFinalNode(oldBoss), "legacy migration preserves boss ID and mandatory shop");

        SkillEventRegression.Run();
        SkillDashRegression.ValidateAssets();
        Check(true, "existing SkillEventRegression.Run and Dash asset validation completed");
        Check(Mathf.Approximately(HealthPointComponent.HealedValue(1, 101, 101 * .15f), 16.15f) &&
            HealthPointComponent.HealedValue(90, 100, 60) == 100 && HealthPointComponent.HealedValue(0, 100, 60) == 0,
            "float HP rule, maximum clamp, no resurrection");

        var ui = UIManager.Instance;
        var shop = ui.OpenUI<ShopUI>();
        var slots = shop.GetComponentsInChildren<UIShopSlot>();
        var item = (ShopItem)AppManager.Instance.GetShopItems(ItemCategory.EQUIPMENT)[0];
        Check(slots.Length == 15 && slots.All(s => s.OnClickedSlot?.GetInvocationList().Length == 1), "lobby slots each have exactly one callback");
        var copy = (ShopItem)item.Copy();
        Check(copy.TargetItemData != null && copy.name == item.name && copy.Price == item.Price, "shop copy preserves resolved data");
        CurrencyManager.Instance.AddCurrency(item.CurrencyType, 10000);
        int balance = CurrencyManager.Instance.GetCurrency(item.CurrencyType);
        int count = InventoryManager.Instance.GetItems(ItemCategory.EQUIPMENT).Count;
        slots[0].GetComponent<Button>().onClick.Invoke();
        var popup = Object.FindFirstObjectByType<UIPopUpShop>();
        Check(popup != null && popup.GetComponent<CanvasGroup>().alpha == 1 && popup.GetComponent<CanvasGroup>().blocksRaycasts,
            "lobby actual slot button shows popup and enables raycasts");
        Check(Field<ItemData>(popup, "item") == item && Field<TextMeshProUGUI>(popup, "itemNameText").text == item.LocalizedName &&
            popup.transform.GetSiblingIndex() == popup.transform.parent.childCount - 1, "selected item and UI stacking order");
        popup.CloseUI();
        Check(CurrencyManager.Instance.GetCurrency(item.CurrencyType) == balance && InventoryManager.Instance.GetItems(ItemCategory.EQUIPMENT).Count == count,
            "lobby cancel preserves money and inventory");
        slots[0].GetComponent<Button>().onClick.Invoke();
        popup.OnSubmit(); popup.OnSubmit();
        Check(CurrencyManager.Instance.GetCurrency(item.CurrencyType) == balance - item.Price &&
            InventoryManager.Instance.GetItems(ItemCategory.EQUIPMENT).Count == count + 1, "lobby purchase grants and charges exactly once on repeated submit");
        ui.CloseAllOpenedUI();
        Debug.Log("SHOP_LOBBY_MAP_CHECKS_PASS");
    }

    [MenuItem("Tools/Shop/3 Load Exploration Test Scene (Play Mode)")]
    public static void PrepareExplore()
    {
        Playing();
        UIManager.Instance.CloseAllOpenedUI();
        var explore = AppManager.Instance.GetExploreManager();
        explore.StartExplore();
        explore.FinallizeSetupAndGenerateMap(new ExplorationSetupData { SelectedCharacterId = 1, SelectedClassId = 1 });
        explore.ConsumeInitialRecordReward();
        SceneManager.LoadScene("StageSelectScene");
    }

    [MenuItem("Tools/Shop/4 Check Exploration Shop (Play Mode)")]
    public static void Explore()
    {
        Playing();
        Check(SceneManager.GetActiveScene().name == "StageSelectScene", "exploration checks run in the real map scene");
        var app = AppManager.Instance; var explore = app.GetExploreManager(); var ui = UIManager.Instance;
        var policy = DataBaseManager.Instance.ExploreShopPolicy;
        var originalPrices = policy.exploreRecordPrices;
        int heal15 = policy.exploreHeal15Price, heal60 = policy.exploreHeal60Price;
        var objects = new List<GameObject>();
        try
        {
            // Explicit test fixture only; never writes product balance to JSON/assets.
            policy.exploreRecordPrices = Enum.GetValues(typeof(RecordRarity)).Cast<RecordRarity>()
                .Select(r => new ExploreRecordPrice { rarity = r, price = 100 }).ToList();
            policy.exploreHeal15Price = 20; policy.exploreHeal60Price = 60;
            var levels = explore.StageReplacer.GetLevels();
            var before = levels[levels.Count - 3][0];
            typeof(ExploreManager).GetField("<MapNodeID>k__BackingField", Hidden).SetValue(explore, before.id);
            explore.GetReplacedNodeInfo().isCleared = true;
            var node = levels[levels.Count - 2][0];
            Check(!explore.CanEnableNode(levels.Last()[0].id) && explore.CanEnableNode(node.id), "movement denies boss bypass and allows shop");
            var party = new[] { (id: 1, current: 10f, max: 101f), (id: 2, current: 90f, max: 100f), (id: 3, current: 0f, max: 100f) };
            foreach (var member in party)
            {
                var go = new GameObject("Test party HP " + member.id); objects.Add(go);
                var hp = go.AddComponent<HealthPointComponent>(); hp.SetHealthPoint(member.max); hp.RestoreCurrentHealth(member.current);
                explore.RegisterRunHealth(member.id, hp);
            }
            var unrelated = new GameObject("Unregistered lobby HP"); objects.Add(unrelated);
            var unrelatedHP = unrelated.AddComponent<HealthPointComponent>(); unrelatedHP.SetHealthPoint(100); unrelatedHP.RestoreCurrentHealth(20);
            explore.EnterStageByNode(node);
            var info = explore.GetReplacedNodeInfo();
            var stock = info.shopStock;
            var shop = Object.FindFirstObjectByType<ShopUI>();
            Check(shop != null && SceneManager.GetActiveScene().name == "StageSelectScene", "shop node opens existing ShopUI without scene transition");
            var offers = stock.offers.Where(o => o.kind == ExploreShopKind.Record).ToList();
            Check(offers.Count == Math.Min(3, app.GetRecordManager().GetShopCandidates().Count) && offers.Select(o => o.recordId).Distinct().Count() == offers.Count,
                "up to three unique eligible records without placeholders");
            var slots = shop.GetComponentsInChildren<UIShopSlot>();
            Check(slots.Length == offers.Count + 3 && slots.All(s => s.GetComponentsInChildren<TMP_Text>().Length == 2), "reused slots contain only name, price and icons");
            var coin = CurrencyManager.Instance;
            coin.ClearExploreCurrency();
            slots[0].GetComponent<Button>().onClick.Invoke();
            Check(Object.FindFirstObjectByType<UIPopUpShop>() == null && coin.GetCurrency(CurrencyType.EXPOLORE_COIN) == 0,
                "insufficient record click opens no popup and does not spend");
            Check(Object.FindObjectsByType<UIToast>(FindObjectsSortMode.None).Any(t =>
                Field<TextMeshProUGUI>(t, "messageText").text == LocalizationManager.Instance.GetText("ui_toast_not_enough_cost_coin")),
                "insufficient purchase uses the existing localized toast");
            slots[offers.Count].GetComponent<Button>().onClick.Invoke();
            Check(Object.FindFirstObjectByType<UIRecordSkillUpPopUp>() == null, "insufficient swap click opens no skill popup");
            coin.AddCurrency(CurrencyType.EXPOLORE_COIN, 1000);
            int balance = coin.GetCurrency(CurrencyType.EXPOLORE_COIN);
            slots[0].GetComponent<Button>().onClick.Invoke();
            var popup = Object.FindFirstObjectByType<UIPopUpShop>();
            Check(popup != null && !string.IsNullOrEmpty(Field<TextMeshProUGUI>(popup, "itemMainDescText").text), "record detail reuses shop popup with description");
            popup.CloseUI();
            Check(coin.GetCurrency(CurrencyType.EXPOLORE_COIN) == balance && !offers[0].sold, "record cancel preserves money and availability");
            slots[0].GetComponent<Button>().onClick.Invoke();
            coin.ClearExploreCurrency(); popup.OnSubmit();
            Check(!offers[0].sold && coin.GetCurrency(CurrencyType.EXPOLORE_COIN) == 0, "record confirmation rechecks changed balance");
            coin.AddCurrency(CurrencyType.EXPOLORE_COIN, 1000); popup.OnSubmit(); popup.OnSubmit();
            Check(offers[0].sold && coin.GetCurrency(CurrencyType.EXPOLORE_COIN) == 900 &&
                app.GetRecordManager().GetPossesRecord().Count(r => r.id == offers[0].recordId) == 1,
                "record repeated confirmation grants/charges once");
            Check(!slots[0].GetComponent<Button>().interactable && slots[0].GetComponentsInChildren<TMP_Text>().Any(t => t.text == "판매 완료"), "sold slot disabled with sold label");
            Check(slots[0].GetComponentsInChildren<TMP_Text>().First(t => t.text == "판매 완료").font.HasCharacters("판매 완료"),
                "sold label uses a font with Korean glyphs");

            var small = stock.offers.First(o => o.kind == ExploreShopKind.Heal15);
            var large = stock.offers.First(o => o.kind == ExploreShopKind.Heal60);
            slots[offers.Count + 1].GetComponent<Button>().onClick.Invoke();
            Object.FindFirstObjectByType<UIPopUpShop>().OnSubmit();
            Check(Mathf.Approximately(objects[0].GetComponent<HealthPointComponent>().GetCurrentHP, 25.15f) &&
                objects[1].GetComponent<HealthPointComponent>().GetCurrentHP == 100 && objects[2].GetComponent<HealthPointComponent>().Dead &&
                unrelatedHP.GetCurrentHP == 20, "15 percent uses each run member max HP, caps, preserves dead and unrelated HP");
            Check(!explore.BuyShopOffer(info, small) && small.purchases == 1, "15 percent is one purchase per node");
            Check(explore.BuyShopOffer(info, large) && Mathf.Approximately(objects[0].GetComponent<HealthPointComponent>().GetCurrentHP, 85.75f) &&
                !explore.BuyShopOffer(info, large), "60 percent heals the same run party and is one purchase per node");

            Swap(shop, info, stock.offers.First(o => o.kind == ExploreShopKind.SkillSwap), slots[offers.Count]);
            balance = coin.GetCurrency(CurrencyType.EXPOLORE_COIN);
            string stockBefore = JsonUtility.ToJson(stock);
            shop.CloseUI();
            Check(explore.CanEnableNode(node.id) && explore.CanEnableNode(levels.Last()[0].id), "closing shop unlocks boss and permits current shop revisit");
            explore.EnterStageByNode(node);
            Check(JsonUtility.ToJson(info.shopStock) == stockBefore && coin.GetCurrency(CurrencyType.EXPOLORE_COIN) == balance,
                "shop reopen preserves stock, purchase state and currency");
            explore.SaveExploreMap();
            var save = SaveManager.LoadExploreRun();
            var savedStock = save.stageNodeData.nodeInfos.First(n => n.nodeId == node.id).shopStock;
            Check(JsonUtility.ToJson(savedStock) == stockBefore && save.partyHealth.Count == 3, "run save contains exact stock and party HP");
            var restored = new StageReplacer(); restored.RestoreStages(save.mapData.chapter, save.mapData, save.stageNodeData);
            Check(JsonUtility.ToJson(restored.GetNodeInfo(node.id).shopStock) == stockBefore &&
                !ReferenceEquals(restored.GetNodeInfo(node.id).shopStock, savedStock), "restored stock is an independent copy");
            Check(SaveManager.LoadRecordData().recordIDs.Count(r => r.recordID == offers[0].recordId) == 1,
                "purchased record is saved once through the existing record inventory");
            var currencyId = app.GetCurrencyItemByType(CurrencyType.EXPOLORE_COIN).id;
            Check(SaveManager.LoadInventoryData().itemInfoList.First(i => i.itemId == currencyId).itemCount == balance,
                "existing inventory save contains the exact post-purchase exploration balance");
            ui.CloseAllOpenedUI();
            explore.ResetData(); explore.Init(false);
            Check(explore.MapNodeID == node.id && JsonUtility.ToJson(explore.GetReplacedNodeInfo().shopStock) == stockBefore,
                "ExploreManager reload preserves node and shop stock");
            var restoredHPObject = new GameObject("Restored party HP"); objects.Add(restoredHPObject);
            var restoredHP = restoredHPObject.AddComponent<HealthPointComponent>(); restoredHP.SetHealthPoint(101);
            explore.RegisterRunHealth(1, restoredHP);
            Check(Mathf.Approximately(restoredHP.GetCurrentHP, 85.75f), "healed HP survives run reload and binds to the next character instance");
            explore.EnterStageByNode(node);
            Check(coin.GetCurrency(CurrencyType.GOLD) > 0, "exploration transactions leave lobby gold intact");
            Debug.Log("SHOP_EXPLORATION_CHECKS_PASS");
        }
        finally
        {
            policy.exploreRecordPrices = originalPrices; policy.exploreHeal15Price = heal15; policy.exploreHeal60Price = heal60;
            foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
        }
    }

    [MenuItem("Tools/Shop/5 Check Chapter Transition (Play Mode)")]
    public static void Chapters()
    {
        Playing();
        var app = AppManager.Instance; var explore = app.GetExploreManager();
        var ui = UIManager.Instance;
        ui.CloseAllOpenedUI();
        int oldMax = Field<int>(explore, "maxChapter");
        typeof(ExploreManager).GetField("maxChapter", Hidden).SetValue(explore, 2);
        var healthObject = new GameObject("Chapter transition HP fixture");
        try
        {
            Check(explore.Chapter == 1 && explore.CurrentSetupData.SelectedCharacterId == 1 &&
                explore.CurrentSetupData.SelectedClassId == 1, "chapter transition fixture starts with valid setup");
            var health = healthObject.AddComponent<HealthPointComponent>(); health.SetHealthPoint(100);
            explore.RegisterRunHealth(1, health); health.RestoreCurrentHealth(37);
            CurrencyManager.Instance.AddCurrency(CurrencyType.EXPOLORE_COIN, 1000);
            int balance = CurrencyManager.Instance.GetCurrency(CurrencyType.EXPOLORE_COIN);
            var equipped = app.GetEquippedActiveSkillListByCharID(1).ToArray();
            void SetNode(int id) => typeof(ExploreManager).GetField("<MapNodeID>k__BackingField", Hidden).SetValue(explore, id);
            SetNode(explore.StageReplacer.GetLevels().Last()[0].id);
            explore.SaveExploreMap();
            var checkpoint = SaveManager.LoadExploreRun(); checkpoint.runStatus = RunStatus.ChapterCleared;
            explore.ChangeState(ExploreState.IN_STAGE);
            explore.ClearStage(true); // Real boss-clear path, no direct chapter assignment.
            Check(explore.Chapter == 2 && explore.CurrentSetupData.SelectedCharacterId == 1 &&
                explore.CurrentSetupData.SelectedClassId == 1 && explore.RunStatus == RunStatus.MidRun,
                "boss clear preserves character/class when generating chapter 2");
            Check(app.GetEquippedActiveSkillListByCharID(1).SequenceEqual(equipped) &&
                CurrencyManager.Instance.GetCurrency(CurrencyType.EXPOLORE_COIN) == balance &&
                SaveManager.LoadExploreRun().partyHealth.Single(h => h.characterId == 1).current == 37 &&
                !explore.InitialRecordRewardPending, "chapter transition preserves equipped skills, currency and HP without another starting reward");

            void OpenShopAndSwap(string phase)
            {
                var levels = explore.StageReplacer.GetLevels();
                SetNode(levels[levels.Count - 3][0].id); explore.GetReplacedNodeInfo().isCleared = true;
                explore.EnterStageByNode(levels[levels.Count - 2][0]);
                var shop = Object.FindFirstObjectByType<ShopUI>();
                var stock = explore.GetReplacedNodeInfo().shopStock;
                int index = stock.offers.FindIndex(o => o.kind == ExploreShopKind.SkillSwap);
                shop.GetComponentsInChildren<UIShopSlot>()[index].GetComponent<Button>().onClick.Invoke();
                var popup = Object.FindFirstObjectByType<UIRecordSkillUpPopUp>();
                Check(popup != null && Field<SkillEventSession>(popup, "session") != null,
                    phase + ": actual shop skill slot opens initialized popup");
                popup.CloseUI(); shop.CloseUI();
            }
            OpenShopAndSwap("chapter 2");
            explore.SaveExploreMap(); explore.ResetData(); explore.Init(false);
            Check(explore.Chapter == 2 && explore.CurrentSetupData.SelectedCharacterId == 1 &&
                explore.CurrentSetupData.SelectedClassId == 1, "chapter 2 save/reload retains setup");
            OpenShopAndSwap("chapter 2 after reload");

            // Resume the checkpoint saved immediately after a boss clear.
            SaveManager.SaveExploreRun(checkpoint);
            explore.ResetData(); explore.Init(false);
            Check(explore.Chapter == 2 && explore.CurrentSetupData.SelectedCharacterId == 1 &&
                explore.CurrentSetupData.SelectedClassId == 1 && explore.StageReplacer.GetLevels().Count == 6 &&
                !explore.InitialRecordRewardPending, "boss-checkpoint resume advances chapter without resetting setup");
            OpenShopAndSwap("chapter 2 boss-checkpoint resume");
            explore.StartExplore();
            Check(explore.Chapter == 1 && explore.RunStatus == RunStatus.SetupIncomplete &&
                !explore.CurrentSetupData.HasCharacter && !explore.CurrentSetupData.HasClass,
                "explicit new run still clears old setup");
            Debug.Log("SHOP_CHAPTER_CHECKS_PASS");
        }
        finally
        {
            typeof(ExploreManager).GetField("maxChapter", Hidden).SetValue(explore, oldMax);
            Object.DestroyImmediate(healthObject);
        }
    }

    static void Swap(ShopUI shop, MapNodeInfo node, ExploreShopOffer offer, UIShopSlot slot)
    {
        var app = AppManager.Instance; var currency = CurrencyManager.Instance;
        var skills = SkillTreeManager.Instance.GetAvailableSkills(1).Where(s => s.template is SO_ActiveSkillData).ToList();
        var first = skills.First(); first.currentLevel = Math.Max(1, first.currentLevel); first.isUnlocked = true;
        app.EquipActiveSkill(1, 0, first);
        int balance = currency.GetCurrency(CurrencyType.EXPOLORE_COIN);
        UIRecordSkillUpPopUp OpenDraft(bool slotFirst = false, int slotIndex = 0)
        {
            slot.GetComponent<Button>().onClick.Invoke();
            var popup = Object.FindFirstObjectByType<UIRecordSkillUpPopUp>();
            var session = Field<SkillEventSession>(popup, "session");
            var candidates = Field<HashSet<int>>(popup, "candidates");
            Check(candidates.All(id => session.GetSkill(id).currentLevel >= 1) && !session.HasChanges,
                "shop candidates start at Lv.1 without granting or charging on open");
            int target = candidates.First(id => session.GetSkill(id).currentLevel < session.GetSkill(id).GetMaxSkillLevel());
            var entries = Field<List<Button>>(popup, "entries");
            var skillButton = entries.First(b => b.name == target.ToString());
            var targetSlot = Field<Button[]>(popup, "slotButtons")[slotIndex];
            if (slotFirst) targetSlot.onClick.Invoke(); else skillButton.onClick.Invoke();
            Check(session.ReplacementCount == 0 && (slotFirst ?
                targetSlot.GetComponentInChildren<TMP_Text>().text.StartsWith("선택") : Field<int>(popup, "selectedSkill") == target),
                "first selection is visible and does not equip an implicit default");
            if (slotFirst) skillButton.onClick.Invoke(); else targetSlot.onClick.Invoke();
            Check(session.ReplacementCount == 1 && session.PendingCost == offer.price && session.Slots[slotIndex] == target,
                (slotFirst ? "slot-first" : "skill-first") + " selection immediately equips draft including empty slots");
            targetSlot.onClick.Invoke(); skillButton.onClick.Invoke();
            Check(session.ReplacementCount == 1 && session.PendingCost == offer.price &&
                Field<Button>(popup, "replaceButton").GetComponentInChildren<TMP_Text>().text.Contains("해제"),
                "repeat selection does not charge twice; former replace button now unequips");
            return popup;
        }
        var draft = OpenDraft(); draft.CloseUI();
        Check(currency.GetCurrency(CurrencyType.EXPOLORE_COIN) == balance && app.GetEquippedActiveSkillListByCharID(1)[0] == first,
            "swap Escape/cancel preserves money and original skill");
        for (int i = 0; i < 2; i++)
        {
            if (i == 1) app.EquipActiveSkill(1, 1, null);
            draft = OpenDraft(i == 1, i);
            var session = Field<SkillEventSession>(draft, "session");
            int selected = Field<int>(draft, "selectedSkill");
            int expectedLevel = session.GetSkill(selected).currentLevel;
            int upgradeCost = i == 1 ? session.UpgradeCost(selected) : 0;
            if (i == 1)
            {
                var upgrade = Field<Button>(draft, "upgradeButton");
                Check(upgrade.gameObject.activeSelf && upgrade.interactable, "existing level-up button is visible and enabled in shop");
                upgrade.onClick.Invoke();
                Field<Button>(Object.FindFirstObjectByType<UISkillEventConfirmation>(), "cancelButton").onClick.Invoke();
                Check(session.PendingCost == offer.price && session.GetSkill(selected).currentLevel == expectedLevel,
                    "canceling level-up confirmation preserves cost and level");
                upgrade.onClick.Invoke();
                Field<Button>(Object.FindFirstObjectByType<UISkillEventConfirmation>(), "confirmButton").onClick.Invoke();
                expectedLevel++;
                Check(session.PendingCost == offer.price + upgradeCost && session.GetSkill(selected).currentLevel == expectedLevel,
                    "shop level-up reuses existing cost and increments the equipped draft");
                int beforeFailure = currency.GetCurrency(CurrencyType.EXPOLORE_COIN);
                currency.ClearExploreCurrency();
                Field<Button>(draft, "closeButton").onClick.Invoke();
                Field<Button>(Object.FindFirstObjectByType<UISkillEventConfirmation>(), "confirmButton").onClick.Invoke();
                Check(draft.isActiveAndEnabled && app.GetEquippedActiveSkillListByCharID(1)[1] == null &&
                    currency.GetCurrency(CurrencyType.EXPOLORE_COIN) == 0,
                    "changed balance blocks combined apply without changing equipped skills");
                currency.AddCurrency(CurrencyType.EXPOLORE_COIN, beforeFailure);
            }
            Field<Button>(draft, "closeButton").onClick.Invoke();
            var confirm = Object.FindFirstObjectByType<UISkillEventConfirmation>();
            Field<Button>(confirm, "confirmButton").onClick.Invoke();
            Field<Button>(confirm, "confirmButton").onClick.Invoke();
            Check(currency.GetCurrency(CurrencyType.EXPOLORE_COIN) == balance - (i + 1) * offer.price - upgradeCost && !offer.sold &&
                app.GetEquippedActiveSkillListByCharID(1)[i].currentLevel == expectedLevel,
                "swap confirmation charges once and permits subsequent purchase " + (i + 1));
        }

        var originalSlots = app.GetEquippedActiveSkillListByCharID(1).ToArray();
        balance = currency.GetCurrency(CurrencyType.EXPOLORE_COIN);
        int purchases = offer.purchases;
        slot.GetComponent<Button>().onClick.Invoke();
        draft = Object.FindFirstObjectByType<UIRecordSkillUpPopUp>();
        var layout = Field<SkillEventSession>(draft, "session");
        var slotButtons = Field<Button[]>(draft, "slotButtons");
        var entryButtons = Field<List<Button>>(draft, "entries");
        Button Entry(int id) => entryButtons.First(b => b.name == id.ToString());
        int skill0 = originalSlots[0].GetSkillID(), skill1 = originalSlots[1].GetSkillID();
        Entry(skill1).onClick.Invoke(); slotButtons[0].onClick.Invoke();
        Check(layout.Slots[0] == skill1 && layout.Slots[1] == 0 && layout.PendingCost == 0,
            "skill-first occupied-slot move clears source and replaces destination without duplicate");
        slotButtons[3].onClick.Invoke(); Entry(skill0).onClick.Invoke();
        Check(layout.Slots[3] == skill0 && layout.Slots.Count(id => id == skill0) == 1,
            "slot-first re-equip restores displaced skill from existing entries");
        slotButtons[0].onClick.Invoke();
        Check(layout.Slots[0] == skill1 && layout.Slots[3] == skill0,
            "selecting slot for removal does not move the previous selected skill");
        var unequip = Field<Button>(draft, "replaceButton");
        Check(unequip.gameObject.activeSelf && unequip.interactable, "existing button shows enabled unequip action for occupied slot");
        unequip.onClick.Invoke(); unequip.onClick.Invoke();
        Check(layout.Slots[0] == 0 && !unequip.interactable && layout.PendingCost == 0,
            "unequip clears only selected slot and disables itself on empty slot");
        draft.CloseUI();
        Check(app.GetEquippedActiveSkillListByCharID(1).SequenceEqual(originalSlots) &&
            currency.GetCurrency(CurrencyType.EXPOLORE_COIN) == balance, "cancel restores move/unequip draft without spending");

        slot.GetComponent<Button>().onClick.Invoke();
        draft = Object.FindFirstObjectByType<UIRecordSkillUpPopUp>();
        slotButtons = Field<Button[]>(draft, "slotButtons");
        entryButtons = Field<List<Button>>(draft, "entries");
        slotButtons[3].onClick.Invoke(); Entry(skill0).onClick.Invoke();
        slotButtons[1].onClick.Invoke(); Field<Button>(draft, "replaceButton").onClick.Invoke();
        Field<Button>(draft, "closeButton").onClick.Invoke();
        Field<Button>(Object.FindFirstObjectByType<UISkillEventConfirmation>(), "confirmButton").onClick.Invoke();
        var committedSlots = app.GetEquippedActiveSkillListByCharID(1);
        Check(committedSlots[0] == null && committedSlots[1] == null && committedSlots[3] == originalSlots[0] &&
            currency.GetCurrency(CurrencyType.EXPOLORE_COIN) == balance && offer.purchases == purchases,
            "move and unequip commit through existing equip API without additional purchase or charge");
    }
}
#endif
