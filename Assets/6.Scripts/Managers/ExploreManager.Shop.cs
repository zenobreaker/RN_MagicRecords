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
    public EventChoice ShopSwapChoice => DataBaseManager.Instance.SafeInvoke(database => database.GetEventInfo(1012))?.eventChoices
        .FirstOrDefault(c => c.ChoiceIsActive && c.ActionType == EventActionType.RECORD_SKILL_UP &&
            c.ActionParam == EventActionParam.DRAFT_3 && c.CostType == EventCostType.CURRENCY &&
            c.CostParam == EventCostParam.EXPLORE_COIN);

    public ExploreShopStock GetShopStock(MapNodeInfo node)
    {
        if (node == null || node.type != StageType.Shop || node != GetReplacedNodeInfo()) return null;
        if (node.shopStock != null)
        {
            UpdateShopOffers(node.shopStock);
            return node.shopStock;
        }
        var policy = DataBaseManager.Instance.SafeInvoke(database => database.ExploreShopPolicy);
        var records = AppManager.Instance.SafeInvoke(app => app.GetRecordManager());
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
        UpdateShopOffers(stock);
        SaveExploreMap();
        return stock;
    }

    private static void UpdateShopOffers(ExploreShopStock stock)
    {
        var policy = DataBaseManager.Instance.SafeInvoke(database => database.ExploreShopPolicy);
        if (!stock.offers.Any(offer => offer.kind == ExploreShopKind.CharacterLevelUp))
            stock.offers.Add(new ExploreShopOffer { kind = ExploreShopKind.CharacterLevelUp });
        foreach (var offer in stock.offers)
        {
            if (offer.kind == ExploreShopKind.Heal15) offer.price = 15;
            else if (offer.kind == ExploreShopKind.Heal60) offer.price = 50;
            else if (offer.kind == ExploreShopKind.SkillSwap) offer.sold = false;
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
            offer == null || node.shopStock == null || !node.shopStock.offers.Contains(offer) ||
            (offer.sold && offer.kind != ExploreShopKind.SkillSwap)) return false;
        string error = null;
        if (offer.price < 0) error = "가격이 아직 정해지지 않은 상품입니다.";
        else if (CurrencyManager.Instance == null || CurrencyManager.Instance.GetCurrency(CurrencyType.EXPOLORE_COIN) < offer.price)
            error = "ui_toast_not_enough_cost_coin";
        else if (offer.kind == ExploreShopKind.Record &&
            !AppManager.Instance.GetRecordManager().GetShopCandidates().Any(r => r.id == offer.recordId))
            error = "현재 구매할 수 없는 레코드입니다.";
        else if (offer.kind == ExploreShopKind.SkillSwap && ShopSwapChoice == null)
            error = "스킬 교체 정보를 불러올 수 없습니다.";
        else if (offer.kind == ExploreShopKind.CharacterLevelUp &&
            (RunStatus != RunStatus.MidRun || CurrentSetupData == null || !CurrentSetupData.HasCharacter ||
             !PlayerManager.Instance.SafeInvoke(players => players.CanLevelUpRunCharacter(CurrentSetupData.SelectedCharacterId))))
            error = "레벨을 올릴 탐사 캐릭터가 없습니다.";
        else if ((offer.kind == ExploreShopKind.Heal15 || offer.kind == ExploreShopKind.Heal60) &&
            !runHealth.Any(h => h.current > 0 && h.current < h.maximum))
            error = "회복할 탐사 캐릭터가 없습니다.";
        if (error != null && notify) UIManager.Instance.SafeInvoke(ui => ui.ShowToast(error));
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
            if (offer.kind == ExploreShopKind.CharacterLevelUp && !LevelUpRunCharacter())
            {
                CurrencyManager.Instance.AddCurrency(CurrencyType.EXPOLORE_COIN, offer.price);
                return false;
            }
            offer.sold = true; // Lock before inventory / HP callbacks can re-enter.
            offer.purchases++;
            if (record != null) recordManager.AddRecord(record);
            else if (offer.kind != ExploreShopKind.CharacterLevelUp)
                HealParty(offer.kind == ExploreShopKind.Heal15 ? .15f : .60f);
            UpdateShopOffers(node.shopStock);
            AppManager.Instance.SaveIfDirty();
            return true;
        }
        finally { purchasing = false; }
    }

    private bool LevelUpRunCharacter()
    {
        int characterId = CurrentSetupData.SelectedCharacterId;
        var players = PlayerManager.Instance;
        if (!players.SafeInvoke(value => value.TryLevelUpRunCharacter(characterId))) return false;
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
            else health.current = health.maximum = data.GetStatusValue(StatusType.HEALTH);
        }
        return true;
    }

    // Called only by SkillEventSession.Commit, after its skill-state checks.
    public bool PayForShopSwap(MapNodeInfo node, ExploreShopOffer offer, int totalCost)
    {
        // The existing session totals replacement and optional level-up costs.
        if (!CanBuyShopOffer(node, offer) || offer.kind != ExploreShopKind.SkillSwap || totalCost < 0) return false;
        if (CurrencyManager.Instance.GetCurrency(CurrencyType.EXPOLORE_COIN) < totalCost)
        { UIManager.Instance.SafeInvoke(ui => ui.ShowToast("ui_toast_not_enough_cost_coin")); return false; }
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
            if (health.current <= 0) continue;
            if (liveHealth.TryGetValue(health.characterId, out var hp) && hp != null)
                hp.Heal(hp.GetMaxHP * fraction);
            else health.current = HealthPointComponent.HealedValue(health.current, health.maximum, health.maximum * fraction);
        }
    }
}
