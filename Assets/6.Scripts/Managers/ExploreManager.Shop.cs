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

    // Event 1012 already defines the three-candidate skill swap and its entry price.
    public EventChoice ShopSwapChoice => DataBaseManager.Instance?.GetEventInfo(1012)?.eventChoices
        .FirstOrDefault(c => c.ChoiceIsActive && c.ActionType == EventActionType.RECORD_SKILL_UP &&
            c.ActionParam == EventActionParam.DRAFT_3 && c.CostType == EventCostType.CURRENCY &&
            c.CostParam == EventCostParam.EXPLORE_COIN);

    public ExploreShopStock GetShopStock(MapNodeInfo node)
    {
        if (node == null || node.type != StageType.Shop || node != GetReplacedNodeInfo()) return null;
        if (node.shopStock != null) return node.shopStock;
        var policy = DataBaseManager.Instance?.ExploreShopPolicy;
        var records = AppManager.Instance?.GetRecordManager();
        var stock = new ExploreShopStock();
        var prices = policy?.exploreRecordPrices ?? new List<ExploreRecordPrice>();
        if (records != null)
            foreach (var record in records.GetShopCandidates()
                .Where(r => prices.Any(p => p.rarity == r.rarity && p.price >= 0))
                .OrderBy(_ => UnityEngine.Random.value).Take(3))
                stock.offers.Add(new ExploreShopOffer { kind = ExploreShopKind.Record, recordId = record.id,
                    price = prices.First(p => p.rarity == record.rarity && p.price >= 0).price });
        stock.offers.Add(new ExploreShopOffer { kind = ExploreShopKind.SkillSwap, price = ShopSwapChoice?.CostValue ?? -1 });
        stock.offers.Add(new ExploreShopOffer { kind = ExploreShopKind.Heal15, price = policy?.exploreHeal15Price ?? -1 });
        stock.offers.Add(new ExploreShopOffer { kind = ExploreShopKind.Heal60, price = policy?.exploreHeal60Price ?? -1 });
        node.shopStock = stock;
        SaveExploreMap();
        return stock;
    }

    public bool CanBuyShopOffer(MapNodeInfo node, ExploreShopOffer offer, bool notify = true)
    {
        if (purchasing || node == null || node != GetReplacedNodeInfo() || node.type != StageType.Shop ||
            offer == null || node.shopStock == null || !node.shopStock.offers.Contains(offer) || offer.sold) return false;
        string error = null;
        if (offer.price < 0) error = "가격이 아직 정해지지 않은 상품입니다.";
        else if (CurrencyManager.Instance == null || CurrencyManager.Instance.GetCurrency(CurrencyType.EXPOLORE_COIN) < offer.price)
            error = "ui_toast_not_enough_cost_coin";
        else if (offer.kind == ExploreShopKind.Record &&
            !AppManager.Instance.GetRecordManager().GetShopCandidates().Any(r => r.id == offer.recordId))
            error = "현재 구매할 수 없는 레코드입니다.";
        else if (offer.kind == ExploreShopKind.SkillSwap && ShopSwapChoice == null)
            error = "스킬 교체 정보를 불러올 수 없습니다.";
        else if ((offer.kind == ExploreShopKind.Heal15 || offer.kind == ExploreShopKind.Heal60) &&
            !runHealth.Any(h => h.current > 0 && h.current < h.maximum))
            error = "회복할 탐사 캐릭터가 없습니다.";
        if (error != null && notify) UIManager.Instance?.ShowToast(error);
        return error == null;
    }

    public bool BuyShopOffer(MapNodeInfo node, ExploreShopOffer offer)
    {
        if (offer?.kind == ExploreShopKind.SkillSwap || !CanBuyShopOffer(node, offer)) return false;
        var recordManager = AppManager.Instance.GetRecordManager();
        var record = offer.kind == ExploreShopKind.Record ? recordManager.GetShopRecord(offer.recordId) : null;
        if (offer.kind == ExploreShopKind.Record && record == null) return false;
        purchasing = true;
        try
        {
            if (offer.price > 0 && !CurrencyManager.Instance.SpendCurrency(CurrencyType.EXPOLORE_COIN, offer.price)) return false;
            offer.sold = true; // Lock before inventory / HP callbacks can re-enter.
            offer.purchases++;
            if (record != null) recordManager.AddRecord(record);
            else HealParty(offer.kind == ExploreShopKind.Heal15 ? .15f : .60f);
            AppManager.Instance.SaveIfDirty();
            return true;
        }
        finally { purchasing = false; }
    }

    // Called only by SkillEventSession.Commit, after its skill-state checks.
    public bool PayForShopSwap(MapNodeInfo node, ExploreShopOffer offer, int totalCost)
    {
        // The existing session totals replacement and optional level-up costs.
        if (!CanBuyShopOffer(node, offer) || offer.kind != ExploreShopKind.SkillSwap || totalCost < 0) return false;
        if (CurrencyManager.Instance.GetCurrency(CurrencyType.EXPOLORE_COIN) < totalCost)
        { UIManager.Instance?.ShowToast("ui_toast_not_enough_cost_coin"); return false; }
        purchasing = true;
        try { return totalCost == 0 || CurrencyManager.Instance.SpendCurrency(CurrencyType.EXPOLORE_COIN, totalCost); }
        finally { purchasing = false; }
    }

    public void CompleteShopSwap(ExploreShopOffer offer)
    {
        offer.purchases++;
        AppManager.Instance.SaveIfDirty();
    }

    public void LeaveShop(MapNodeInfo node)
    {
        if (node != GetReplacedNodeInfo() || node?.type != StageType.Shop) return;
        ClearStage(true);
    }

    public void RegisterRunHealth(int characterId, HealthPointComponent hp)
    {
        if (RunStatus != RunStatus.MidRun || hp == null) return;
        var saved = runHealth.FirstOrDefault(h => h.characterId == characterId);
        if (saved == null)
        {
            saved = new ExploreHealthState { characterId = characterId, current = hp.GetCurrentHP, maximum = hp.GetMaxHP };
            runHealth.Add(saved);
        }
        else hp.RestoreCurrentHealth(saved.current);
        liveHealth[characterId] = hp;
        saved.maximum = hp.GetMaxHP;
        saved.current = hp.GetCurrentHP;
        hp.OnChangedHP_TwoParam += (current, maximum) => { saved.current = current; saved.maximum = maximum; };
    }

    private void HealParty(float fraction)
    {
        foreach (var health in runHealth)
        {
            if (health.current <= 0) continue;
            if (liveHealth.TryGetValue(health.characterId, out var hp) && hp != null)
                hp.Heal(hp.GetMaxHP * fraction);
            else health.current = HealthPointComponent.HealedValue(health.current, health.maximum, health.maximum * fraction);
        }
    }
}
