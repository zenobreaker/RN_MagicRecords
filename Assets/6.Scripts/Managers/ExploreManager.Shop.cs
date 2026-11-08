using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Shop state belongs to the existing run and MapNodeInfo, not to the popup.
public sealed partial class ExploreManager
{
    private List<ExploreHealthState> runHealth = new();
    private readonly Dictionary<int, HealthPointComponent> liveHealth = new();
    private bool purchasing;

    // Reuse event 1012's candidates. Shop entry/replacement is free; upgrades retain their own cost.
    public EventChoice ShopSwapChoice
    {
        get
        {
            return DataBaseManager.Instance.SafeInvoke(database => database.GetEventInfo(1012, includeInactive: true))?.eventChoices
        .FirstOrDefault(c => c.ChoiceIsActive && c.ActionType == EventActionType.RECORD_SKILL_UP &&
            c.ActionParam == EventActionParam.DRAFT_3);
        }
    }

    public ExploreShopStock GetShopStock(MapNodeInfo node)
    {
        if (node == null || node.type != StageType.Shop || node != GetReplacedNodeInfo())
        {
            return null;
        }

        var policy = DataBaseManager.Instance.SafeInvoke(database => database.ExploreShopPolicy);
        var records = AppManager.Instance.SafeInvoke(app => app.GetRecordManager());
        if (records == null || policy == null)
        {
            return null;
        }

        var stock = node.shopStock;
        if (stock != null && stock.recordSlotCount > 0)
        {
            UpdateShopOffers(stock);
            return stock;
        }

        stock ??= new ExploreShopStock();
        stock.offers ??= new List<ExploreShopOffer>();
        stock.recordSlotCount = UnityEngine.Random.value < policy.specialShopChance ? 8 : 4;
        // 이전 저장의 이미 구매한 카드와 서비스 구매 횟수를 보존하고 빈 자리만 채웁니다.
        var existing = stock.offers.Where(offer => offer.kind == ExploreShopKind.Record).ToList();
        foreach (var offer in existing)
        {
            var record = records.GetShopRecord(offer.recordId);
            var range = record == null ? null : policy.GetRecordPrice(record.rarity);
            if (offer.sold == false && range != null && (offer.price < range.price || offer.price > range.maximumPrice))
            {
                offer.price = range.Roll(record.type);
            }
        }
        var additional = RollShopRecords(records, policy, stock, existing, out _, out _);
        stock.offers.AddRange(additional);
        foreach (var kind in new[] { ExploreShopKind.SkillSwap, ExploreShopKind.Heal15, ExploreShopKind.Heal60 })
        {
            if (stock.offers.Any(offer => offer.kind == kind) == false)
            {
                stock.offers.Add(new ExploreShopOffer
                {
                    kind = kind
                });
            }
        }
        node.shopStock = stock;
        UpdateShopOffers(stock);
        SaveExploreMap();
        return stock;
    }

    /// <summary>한 진열 전체에 같은 보정을 사용하며 ID가 같은 카드는 두 번 뽑지 않습니다.</summary>
    private static List<ExploreShopOffer> RollShopRecords(RecordManager manager, ShopJsonAllData policy,
        ExploreShopStock stock, List<ExploreShopOffer> existing, out bool highestEligible, out bool highestShown)
    {
        var excluded = new HashSet<int>(existing.Select(offer => offer.recordId));
        var candidates = manager.GetShopCandidates()
            .Where(record => excluded.Contains(record.id) == false && policy.GetRecordPrice(record.rarity) != null).ToList();
        var result = new List<ExploreShopOffer>();
        highestEligible = false;
        highestShown = false;
        while (result.Count + existing.Count < stock.recordSlotCount && candidates.Count > 0)
        {
            var record = RecordManager.PickWeightedRecord(candidates, stock.rarityMissCount, out bool eligible, out var highest);
            highestEligible |= eligible;
            if (record == null)
            {
                break;
            }
            highestShown |= eligible && record.rarity == highest;
            result.Add(new ExploreShopOffer
            {
                kind = ExploreShopKind.Record,
                recordId = record.id,
                price = policy.GetRecordPrice(record.rarity).Roll(record.type)
            });
            candidates.RemoveAll(candidate => candidate.id == record.id);
        }
        return result;
    }

    private bool IsOpenShop(MapNodeInfo node)
    {
        return purchasing == false && RunStatus == RunStatus.MidRun && node != null && node.type == StageType.Shop &&
            node == GetReplacedNodeInfo() && node.shopStock != null;
    }

    /// <summary>결제에 성공한 리롤만 진열, 다음 가격 및 해당 상점의 보정에 반영합니다.</summary>
    public bool RerollShop(MapNodeInfo node, int displayedPrice)
    {
        if (IsOpenShop(node) == false || node.shopStock.RerollPrice != displayedPrice)
        {
            return false;
        }
        var manager = AppManager.Instance.SafeInvoke(app => app.GetRecordManager());
        var policy = DataBaseManager.Instance.SafeInvoke(database => database.ExploreShopPolicy);
        if (policy == null || manager == null)
        {
            return false;
        }

        purchasing = true;
        try
        {
            var stock = node.shopStock;
            var next = RollShopRecords(manager, policy, stock, new List<ExploreShopOffer>(), out bool eligible, out bool shown);
            if (next.Count == 0)
            {
                UIManager.Instance.SafeInvoke(ui => ui.ShowToast("진열할 레코드가 없습니다."));
                return false;
            }
            if (SpendShopCoins(displayedPrice) == false)
            {
                return false;
            }
            stock.offers.RemoveAll(offer => offer.kind == ExploreShopKind.Record);
            stock.offers.AddRange(next);
            stock.rerollCount++;
            stock.rarityMissCount = eligible && shown == false ? Mathf.Min(stock.rarityMissCount + 1, 500) : 0;
            AppManager.Instance.SaveIfDirty();
            return true;
        }
        finally
        {
            purchasing = false;
        }
    }

    private static bool SpendShopCoins(int price)
    {
        if (price < 0 || CurrencyManager.Instance.SafeInvoke(wallet => wallet.SpendCurrency(CurrencyType.EXPOLORE_COIN, price)) == false)
        {
            UIManager.Instance.SafeInvoke(ui => ui.ShowToast("ui_toast_not_enough_cost_coin"));
            return false;
        }
        return true;
    }

    /// <summary>표시된 레벨과 가격을 재검사해 같은 확인 요청으로 두 번 강화하지 않습니다.</summary>
    public bool UpgradeShopRecord(MapNodeInfo node, RecordData record, int quotedLevel, int quotedPrice)
    {
        var manager = AppManager.Instance.SafeInvoke(app => app.GetRecordManager());
        var policy = DataBaseManager.Instance.SafeInvoke(database => database.ExploreShopPolicy);
        if (IsOpenShop(node) == false || policy == null || manager == null ||
            manager.TryGetUpgradeLevel(record, out int current, out _) == false || current != quotedLevel ||
            policy.GetUpgradePrice(current) != quotedPrice)
        {
            return false;
        }
        purchasing = true;
        try
        {
            if (SpendShopCoins(quotedPrice) == false)
            {
                return false;
            }
            if (manager.TryUpgradeOwned(record, quotedLevel) == false)
            {
                CurrencyManager.Instance.AddCurrency(CurrencyType.EXPOLORE_COIN, quotedPrice);
                return false;
            }
            AppManager.Instance.SaveIfDirty();
            return true;
        }
        finally
        {
            purchasing = false;
        }
    }

    /// <summary>인벤토리에서 같은 실물을 제거한 뒤에만 판매 대금을 지급합니다.</summary>
    public bool SellShopRecord(MapNodeInfo node, RecordData record, int quotedPrice)
    {
        var manager = AppManager.Instance.SafeInvoke(app => app.GetRecordManager());
        var policy = DataBaseManager.Instance.SafeInvoke(database => database.ExploreShopPolicy);
        if (IsOpenShop(node) == false || manager == null || CurrencyManager.Instance == null || record == null ||
            policy?.GetRecordPrice(record.rarity)?.price != quotedPrice)
        {
            return false;
        }
        purchasing = true;
        try
        {
            if (manager.TrySellOwned(record) == false)
            {
                return false;
            }
            CurrencyManager.Instance.AddCurrency(CurrencyType.EXPOLORE_COIN, quotedPrice);
            AppManager.Instance.SaveIfDirty();
            return true;
        }
        finally
        {
            purchasing = false;
        }
    }

    private static void UpdateShopOffers(ExploreShopStock stock)
    {
        var policy = DataBaseManager.Instance.SafeInvoke(database => database.ExploreShopPolicy);
        if (stock.offers.Any(offer => offer.kind == ExploreShopKind.CharacterLevelUp) == false)
        {
            stock.offers.Add(new ExploreShopOffer
            {
                kind = ExploreShopKind.CharacterLevelUp
            });
        }

        foreach (var offer in stock.offers)
        {
            if (offer.kind == ExploreShopKind.Heal15)
            {
                offer.price = 15;
            }
            else if (offer.kind == ExploreShopKind.Heal60)
            {
                offer.price = 50;
            }
            else if (offer.kind == ExploreShopKind.SkillSwap)
            {
                offer.price = 0; // Also migrate offers restored from an older run save.
                offer.sold = false;
            }
            else if (offer.kind == ExploreShopKind.CharacterLevelUp)
            {
                offer.price = policy?.exploreCharacterLevelUpPrice ?? -1;
                offer.sold = offer.purchases >= (policy?.exploreCharacterLevelUpLimit ?? 0);
            }
        }
    }

    public bool CanBuyShopOffer(MapNodeInfo node, ExploreShopOffer offer, bool notify = true)
    {
        if (purchasing || node == null || node != GetReplacedNodeInfo() || node.type != StageType.Shop ||
            offer == null || node.shopStock == null || node.shopStock.offers.Contains(offer) == false ||
            (offer.sold && offer.kind != ExploreShopKind.SkillSwap))
        {
            return false;
        }

        string error = null;
        if (offer.price < 0)
        {
            error = "가격이 아직 정해지지 않은 상품입니다.";
        }
        else if (CurrencyManager.Instance == null || CurrencyManager.Instance.GetCurrency(CurrencyType.EXPOLORE_COIN) < offer.price)
        {
            error = "ui_toast_not_enough_cost_coin";
        }
        else if (offer.kind == ExploreShopKind.Record &&
                    AppManager.Instance.GetRecordManager().GetShopCandidates().Any(r => r.id == offer.recordId) == false)
        {
            error = "현재 구매할 수 없는 레코드입니다.";
        }
        else if (offer.kind == ExploreShopKind.SkillSwap && ShopSwapChoice == null)
        {
            error = "스킬 교체 정보를 불러올 수 없습니다.";
        }
        else if (offer.kind == ExploreShopKind.CharacterLevelUp &&
                    (RunStatus != RunStatus.MidRun || CurrentSetupData == null || CurrentSetupData.HasCharacter == false ||
                     PlayerManager.Instance.SafeInvoke(players => players.CanLevelUpRunCharacter(CurrentSetupData.SelectedCharacterId)) == false))
        {
            error = PlayerManager.Instance?.GetRunCharacterStatus(CurrentSetupData?.SelectedCharacterId ?? -1)?.level >= CharStatusData.MaxLevel
            ? "캐릭터가 최대 레벨(20)에 도달했습니다." : "레벨을 올릴 탐사 캐릭터가 없습니다.";
        }
        else if ((offer.kind == ExploreShopKind.Heal15 || offer.kind == ExploreShopKind.Heal60) &&
                    runHealth.Any(h => h.current > 0 && h.current < h.maximum) == false)
        {
            error = "회복할 탐사 캐릭터가 없습니다.";
        }

        if (error != null && notify)
        {
            UIManager.Instance.SafeInvoke(ui => ui.ShowToast(error));
        }

        return error == null;
    }

    public bool BuyShopOffer(MapNodeInfo node, ExploreShopOffer offer)
    {
        if (offer?.kind == ExploreShopKind.SkillSwap || CanBuyShopOffer(node, offer) == false)
        {
            return false;
        }

        var recordManager = AppManager.Instance.GetRecordManager();
        var record = offer.kind == ExploreShopKind.Record ? recordManager.GetShopRecord(offer.recordId) : null;
        if (offer.kind == ExploreShopKind.Record && record == null)
        {
            return false;
        }

        purchasing = true;
        try
        {
            if (offer.price > 0 && CurrencyManager.Instance.SpendCurrency(CurrencyType.EXPOLORE_COIN, offer.price) == false)
            {
                return false;
            }

            if (offer.kind == ExploreShopKind.CharacterLevelUp && LevelUpRunCharacter() == false)
            {
                CurrencyManager.Instance.AddCurrency(CurrencyType.EXPOLORE_COIN, offer.price);
                return false;
            }
            offer.sold = true;
            offer.purchases++;
            if (record != null && recordManager.GrantRecord(record) == null)
            {
                offer.sold = false;
                offer.purchases--;
                CurrencyManager.Instance.AddCurrency(CurrencyType.EXPOLORE_COIN, offer.price);
                return false;
            }
            if (record == null && offer.kind != ExploreShopKind.CharacterLevelUp)
            {
                HealParty(offer.kind == ExploreShopKind.Heal15 ? .15f : .60f);
            }

            UpdateShopOffers(node.shopStock);
            AppManager.Instance.SaveIfDirty();
            return true;
        }
        finally
        {
            purchasing = false;

        }
    }

    private bool LevelUpRunCharacter()
    {
        int characterId = CurrentSetupData.SelectedCharacterId;
        var players = PlayerManager.Instance;
        if (players.SafeInvoke(value => value.TryLevelUpRunCharacter(characterId)) == false)
        {
            return false;
        }

        var data = players.GetRunCharacterStatus(characterId);
        var health = runHealth.FirstOrDefault(value => value.characterId == characterId);
        if (health != null)
        {
            // SetStatusData uses SetHealthPoint, which refills HP on growth.
            // Apply the same result when the battle object is absent in the node scene.
            if (liveHealth.TryGetValue(characterId, out var hp) && hp != null)
            {
                health.maximum = hp.GetMaxHP;
                health.current = hp.GetCurrentHP;
            }
            else
            {
                health.current = health.maximum = data.GetStatusValue(StatusType.HEALTH);
            }
        }
        return true;
    }

    // Called only by SkillEventSession.Commit, after its skill-state checks.
    public bool PayForShopSwap(MapNodeInfo node, ExploreShopOffer offer, int totalCost)
    {
        // The existing session totals replacement and optional level-up costs.
        if (CanBuyShopOffer(node, offer) == false || offer.kind != ExploreShopKind.SkillSwap || totalCost < 0)
        {
            return false;
        }

        if (CurrencyManager.Instance.GetCurrency(CurrencyType.EXPOLORE_COIN) < totalCost)
        {
            UIManager.Instance.SafeInvoke(ui => ui.ShowToast("ui_toast_not_enough_cost_coin"));
            return false;
        }
        purchasing = true;
        try
        {
            return totalCost == 0 || CurrencyManager.Instance.SpendCurrency(CurrencyType.EXPOLORE_COIN, totalCost);
        }
        finally
        {
            purchasing = false;

        }
    }

    public void CompleteShopSwap(ExploreShopOffer offer)
    {
        offer.purchases++;
        AppManager.Instance.SaveIfDirty();
    }

    public void LeaveShop(MapNodeInfo node)
    {
        if (node != GetReplacedNodeInfo() || node?.type != StageType.Shop)
        {
            return;
        }

        ClearStage(true);
    }

    public void RegisterRunHealth(int characterId, HealthPointComponent hp)
    {
        if (RunStatus != RunStatus.MidRun || hp == null)
        {
            return;
        }

        var saved = runHealth.FirstOrDefault(h => h.characterId == characterId);
        if (saved == null)
        {
            saved = new ExploreHealthState
            {
                characterId = characterId,
                current = hp.GetCurrentHP,
                maximum = hp.GetMaxHP
            };
            runHealth.Add(saved);
        }
        else
        {
            hp.RestoreCurrentHealth(saved.current);
        }

        liveHealth[characterId] = hp;
        saved.maximum = hp.GetMaxHP;
        saved.current = hp.GetCurrentHP;
        hp.OnChangedHP_TwoParam += (current, maximum) =>
        {
            saved.current = current;
            saved.maximum = maximum;

        };
    }

    // 노드 씬에서는 전투 오브젝트 없이 기존 탐사 HP를 읽습니다.
    public bool TryGetRunHealth(int characterId, out float current, out float maximum)
    {
        var health = runHealth.FirstOrDefault(value => value.characterId == characterId);
        current = health?.current ?? 0f;
        maximum = health?.maximum ?? 0f;
        return health != null;
    }

    private void HealParty(float fraction)
    {
        foreach (var health in runHealth)
        {
            if (health.current <= 0)
            {
                continue;
            }

            if (liveHealth.TryGetValue(health.characterId, out var hp) && hp != null)
            {
                hp.Heal(hp.GetMaxHP * fraction);
            }
            else
            {
                health.current = HealthPointComponent.HealedValue(health.current, health.maximum, health.maximum * fraction);
            }
        }
    }

    public bool HealPartyForCheat()
    {
        if (RunStatus != RunStatus.MidRun || runHealth.Any(health => health.current > 0) == false)
        {
            return false;
        }

        HealParty(1f);
        return true;
    }
}
