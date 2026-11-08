using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : UiBase
{
    private ItemCategory category;
    private List<ItemData> items;

    private AppManager app;
    private MapNodeInfo shopNode;
    private bool openingOffer;
    private UiBase offerPopup;

    [Header("Exploration stock")]
    [SerializeField] private GameObject lobbyCategory;
    [SerializeField] private GameObject exploreCategory;
    [SerializeField] private RectTransform exploreContent;
    [SerializeField] private ScrollRect shopScroll;
    [SerializeField] private RecordCard inventoryCardPrefab;
    [SerializeField] private TMP_Text shelfLabel;
    [SerializeField] private TMP_Text rerollLabel;

    private enum StockPage
    {
        Buy,
        Upgrade,
        Sell
    }

    private StockPage page;
    private readonly List<RecordCard> cards = new();
    private readonly List<UIShopSlot> services = new();
    private int cardIndex;
    private int lastRerollFrame = -1;

    protected override void OnEnable()
    {
        base.OnEnable();

        app = AppManager.Instance;
        category = ItemCategory.EQUIPMENT;
        SetCategoryVisible(true);

        DrawShop();
    }

    public void SetExploreShop(MapNodeInfo node)
    {
        shopNode = node;
        page = StockPage.Buy;
        SetCategoryVisible(false);
        DrawExploreShop();
    }

    private void SetCategoryVisible(bool visible)
    {
        lobbyCategory.SetActive(visible);
        exploreCategory.SetActive(visible == false);
        content.SetActive(visible);
        exploreContent.gameObject.SetActive(visible == false);
        shopScroll.content = visible ? (RectTransform)content.transform : exploreContent;
        shopScroll.StopMovement();
        shopScroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>레코드에는 인벤토리 카드를, 일반 서비스에는 기존 상점 슬롯을 사용합니다.</summary>
    private void DrawExploreShop()
    {
        var explore = app.SafeInvoke(value => value.GetExploreManager());
        var stock = explore.SafeInvoke(value => value.GetShopStock(shopNode));
        var manager = app.SafeInvoke(value => value.GetRecordManager());
        var policy = DataBaseManager.Instance.SafeInvoke(database => database.ExploreShopPolicy);
        if (stock == null || manager == null || policy == null)
        {
            return;
        }

        shelfLabel.text = (stock.recordSlotCount == 8 ? "특별 상점" : "일반 상점") +
            $"\n보유 {CurrencyManager.Instance.SafeInvoke(wallet => wallet.GetCurrency(CurrencyType.EXPOLORE_COIN))} 코인";
        rerollLabel.text = $"리롤 · {stock.RerollPrice} 코인";
        cardIndex = 0;
        int serviceIndex = 0;
        if (page == StockPage.Buy)
        {
            foreach (var offer in stock.offers.Where(item => item.kind != ExploreShopKind.Record))
            {
                if (serviceIndex == services.Count)
                {
                    services.Add(Instantiate(childObject, exploreContent).GetComponent<UIShopSlot>());
                }
                var slot = services[serviceIndex++];
                slot.transform.SetAsLastSibling();
                slot.gameObject.SetActive(true);
                string title = offer.kind switch
                {
                    ExploreShopKind.SkillSwap => "스킬 교체",
                    ExploreShopKind.CharacterLevelUp => "캐릭터 레벨업",
                    ExploreShopKind.Heal15 => "체력 15% 회복",
                    _ => "체력 60% 회복"
                };
                bool maxLevel = offer.kind == ExploreShopKind.CharacterLevelUp &&
                    PlayerManager.Instance.SafeInvoke(players => players.GetRunCharacterStatus(explore.CurrentSetupData.SelectedCharacterId))?.level >= CharStatusData.MaxLevel;
                slot.SetOffer(app.GetStageIcon(StageType.Shop), title, offer.price, offer.sold,
                    () => OpenOffer(offer, null, title), showPrice: offer.kind != ExploreShopKind.SkillSwap,
                    unavailableLabel: maxLevel ? "최대 레벨" : null);
            }

            var available = new HashSet<int>(manager.GetShopCandidates().Select(record => record.id));
            foreach (var offer in stock.offers.Where(item => item.kind == ExploreShopKind.Record))
            {
                var record = manager.GetShopRecord(offer.recordId);
                if (record == null || (offer.sold == false && available.Contains(record.id) == false))
                {
                    continue;
                }
                AddCard(record, offer.sold ? "판매 완료" : $"{offer.price} 코인", offer.sold == false,
                    () => OpenOffer(offer, record, record.recordName));
            }
        }
        else
        {
            foreach (var record in page == StockPage.Upgrade ? manager.GetUpgradeableRecords() : manager.GetPossesRecord())
            {
                if (page == StockPage.Upgrade)
                {
                    if (manager.TryGetUpgradeLevel(record, out int level, out int maximum))
                    {
                        int price = policy.GetUpgradePrice(level);
                        AddCard(record, $"Lv.{level}/{maximum} · 강화 {price} 코인", true,
                            () => ConfirmRecordAction(record, price, level));
                    }
                }
                else
                {
                    var range = policy.GetRecordPrice(record.rarity);
                    if (range != null)
                    {
                        AddCard(record, $"판매 +{range.price} 코인", true,
                            () => ConfirmRecordAction(record, range.price, 0));
                    }
                }
            }
        }
        for (int i = cardIndex; i < cards.Count; i++)
        {
            cards[i].gameObject.SetActive(false);
        }
        for (int i = serviceIndex; i < services.Count; i++)
        {
            services[i].gameObject.SetActive(false);
        }
        if (cardIndex == 0 && page != StockPage.Buy)
        {
            UIManager.Instance.SafeInvoke(ui => ui.ShowToast(page == StockPage.Sell ? "판매할 레코드가 없습니다." : "강화할 레코드가 없습니다."));
        }
    }

    private void AddCard(RecordData record, string label, bool available, System.Action click)
    {
        if (cardIndex == cards.Count)
        {
            cards.Add(Instantiate(inventoryCardPrefab, exploreContent));
        }
        var card = cards[cardIndex++];
        card.transform.SetAsLastSibling();
        card.gameObject.SetActive(true);
        card.SetupShopEntry(record, label, available, click);
    }

    /// <summary>프리팹의 왼쪽 메뉴에서 진열, 리롤, 강화 및 판매를 선택합니다.</summary>
    public void OnExploreCategory(int action)
    {
        if (shopNode == null || openingOffer || (offerPopup != null && offerPopup.gameObject.activeSelf))
        {
            return;
        }
        if (action == 1)
        {
            if (lastRerollFrame == Time.frameCount || shopNode.shopStock == null)
            {
                return;
            }
            lastRerollFrame = Time.frameCount;
            if (app.GetExploreManager().RerollShop(shopNode, shopNode.shopStock.RerollPrice) == false)
            {
                return;
            }
        }
        page = action == 2 ? StockPage.Upgrade : action == 3 ? StockPage.Sell : StockPage.Buy;
        DrawExploreShop();
        shopScroll.StopMovement();
        shopScroll.verticalNormalizedPosition = 1f;
    }

    private void ConfirmRecordAction(RecordData record, int price, int level)
    {
        if (shopNode == null || (offerPopup != null && offerPopup.gameObject.activeSelf))
        {
            return;
        }
        bool upgrade = level > 0;
        if (upgrade && CurrencyManager.Instance.SafeInvoke(wallet => wallet.GetCurrency(CurrencyType.EXPOLORE_COIN)) < price)
        {
            UIManager.Instance.SafeInvoke(ui => ui.ShowToast("ui_toast_not_enough_cost_coin"));
            return;
        }
        var popup = UIManager.Instance.OpenUI<UIPopUpTwoButton>(true);
        if (popup == null)
        {
            return;
        }
        offerPopup = popup;
        var node = shopNode;
        var explore = app.GetExploreManager();
        string title = upgrade ? "레코드 강화" : "레코드 판매";
        string message = upgrade ? $"{record.recordName} · Lv.{level} → {level + 1}\n{price} 코인을 사용하시겠습니까?" :
            $"{record.recordName}을(를) 판매하고 {price} 코인을 받으시겠습니까?";
        popup.SetData(title, message, upgrade ? "강화" : "판매", "취소", () =>
        {
            if (shopNode != node || popup.isActiveAndEnabled == false)
            {
                return;
            }
            bool changed = upgrade ? explore.UpgradeShopRecord(node, record, level, price) :
                explore.SellShopRecord(node, record, price);
            if (changed == false)
            {
                UIManager.Instance.SafeInvoke(ui => ui.ShowToast("상품 상태가 변경되어 거래하지 못했습니다."));
            }
            DrawExploreShop();
        }, null);
    }

    private void OpenOffer(ExploreShopOffer offer, RecordData record, string title)
    {
        var explore = app.GetExploreManager();
        if (openingOffer || (offerPopup != null && offerPopup.gameObject.activeSelf) || explore.CanBuyShopOffer(shopNode, offer) == false)
        {
            return;
        }

        openingOffer = true;
        try
        {
            if (offer.kind == ExploreShopKind.SkillSwap)
            {
                var popup = UIManager.Instance.OpenUI<UIRecordSkillUpPopUp>(true);
                offerPopup = popup;
                if (popup != null)
                {
                    popup.SetShopData(explore.ShopSwapChoice, offer.price,
                    total => explore.PayForShopSwap(shopNode, offer, total), () =>
                    {
                        explore.CompleteShopSwap(offer);
                        DrawExploreShop();
                    });
                }
            }
            else
            {
                var popup = UIManager.Instance.OpenUI<UIPopUpShop>(true);
                offerPopup = popup;
                if (popup != null)
                {
                    popup.SetOffer(record?.icon ?? app.GetStageIcon(StageType.Shop), title, record?.description ??
                    (offer.kind == ExploreShopKind.CharacterLevelUp
                        ? "탐사 캐릭터의 레벨이 1 증가하고 성장 스탯이 적용됩니다. 이번 탐사에서만 유지됩니다."
                        : "현재 탐사에서 살아 있는 캐릭터들의 최대 체력을 기준으로 회복합니다."), offer.price, () =>
                    {
                        if (explore.BuyShopOffer(shopNode, offer) == false)
                        {
                            return false;
                        }

                        DrawExploreShop();
                        return true;
                    });
                }
            }
        }
        finally
        {
            openingOffer = false;

        }
    }

    protected override void OnDisable()
    {
        var node = shopNode;
        shopNode = null;
        if (offerPopup != null)
        {
            UIManager.Instance.SafeInvoke(ui => ui.CloseSpecificUI(offerPopup));
        }

        offerPopup = null;
        // Restore the same prefab for a subsequent lobby use.
        SetCategoryVisible(true);
        if (node != null)
        {
            app.SafeInvoke(value => value.GetExploreManager()).SafeInvoke(explore => explore.LeaveShop(node));
        }

        base.OnDisable();
    }

    private void DrawShop()
    {
        if (app == null)
        {
            return;
        }

        items = app.GetShopItems(category);
        if (items == null)
        {
            return;
        }

        UIListDrawer.DrawList<UIShopSlot, ItemData>(
            items, (slot, item, index) =>
            {
                slot.SetItemData(item);
                if (slot.gameObject.activeSelf == false)
                {
                    slot.gameObject.SetActive(true);
                }

                slot.OnClickedSlot -= OnClickedSlot;
                slot.OnClickedSlot += OnClickedSlot;
            },
            slot =>
            {
                slot.OnClickedSlot -= OnClickedSlot;
                if (slot.gameObject.activeSelf == true)
                {
                    slot.gameObject.SetActive(false);
                }
            },
            InitReplaceContentObject,
             SetContentChildObjectsCallback<UIShopSlot>
            );
    }

    // ShopUI.cs - OnClickedSlot
    public void OnClickedSlot(ItemData item)
    {
        if (shopNode != null || (offerPopup != null && offerPopup.gameObject.activeSelf))
        {
            return;
        }

        if (item is ShopItem shopItem == false)
        {
            return;
        }

        offerPopup = UIManager.Instance.OpenShopPopUp(item, shopItem.Price, shopItem.CurrencyType);
    }

    public void OnCategoryButton(int categoryNumber)
    {
        if (shopNode != null)
        {
            return;
        }

        this.category = (ItemCategory)categoryNumber;
        DrawShop();
    }
}
