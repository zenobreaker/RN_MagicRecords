using System.Collections.Generic;
using UnityEngine;

public class ShopUI : UiBase
{
    private ItemCategory category;
    private List<ItemData> items;

    private AppManager app;
    private MapNodeInfo shopNode;
    private bool openingOffer;
    private UiBase offerPopup;

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
        SetCategoryVisible(false);
        DrawExploreShop();
    }

    private void SetCategoryVisible(bool visible)
    {
        Transform categoryRoot = null;
        foreach (var child in GetComponentsInChildren<Transform>(true))
            if (child.name == "ShopCategory") { categoryRoot = child; break; }
        if (categoryRoot == null) return;
        // Keep the existing layout's left gutter clear for the back button.
        var group = categoryRoot.GetComponent<CanvasGroup>();
        if (group == null) group = categoryRoot.gameObject.AddComponent<CanvasGroup>();
        group.alpha = visible ? 1 : 0;
        group.interactable = group.blocksRaycasts = visible;
    }

    private void DrawExploreShop()
    {
        var explore = app?.GetExploreManager();
        var stock = explore?.GetShopStock(shopNode);
        if (stock == null) return;
        UIListDrawer.DrawList<UIShopSlot, ExploreShopOffer>(stock.offers, (slot, offer, index) =>
        {
            var record = offer.kind == ExploreShopKind.Record ? app.GetRecordManager().GetShopRecord(offer.recordId) : null;
            string title = record?.recordName ?? (offer.kind == ExploreShopKind.SkillSwap ? "스킬 교체" :
                offer.kind == ExploreShopKind.Heal15 ? "체력 15% 회복" : "체력 60% 회복");
            slot.gameObject.SetActive(true);
            slot.SetOffer(record?.icon ?? app.GetStageIcon(StageType.Shop), title, offer.price, offer.sold, () => OpenOffer(offer, record, title));
        }, slot => slot.gameObject.SetActive(false), InitReplaceContentObject, SetContentChildObjectsCallback<UIShopSlot>);
    }

    private void OpenOffer(ExploreShopOffer offer, RecordData record, string title)
    {
        var explore = app.GetExploreManager();
        if (openingOffer || (offerPopup != null && offerPopup.gameObject.activeSelf) || !explore.CanBuyShopOffer(shopNode, offer)) return;
        openingOffer = true;
        try
        {
            if (offer.kind == ExploreShopKind.SkillSwap)
            {
                var popup = UIManager.Instance.OpenUI<UIRecordSkillUpPopUp>(true);
                offerPopup = popup;
                if (popup != null) popup.SetShopData(explore.ShopSwapChoice, offer.price,
                    total => explore.PayForShopSwap(shopNode, offer, total), () => explore.CompleteShopSwap(offer));
            }
            else
            {
                var popup = UIManager.Instance.OpenUI<UIPopUpShop>(true);
                offerPopup = popup;
                if (popup != null) popup.SetOffer(record?.icon ?? app.GetStageIcon(StageType.Shop), title, record?.description ??
                    "현재 탐사에서 살아 있는 캐릭터들의 최대 체력을 기준으로 회복합니다.", offer.price, () =>
                    {
                        if (!explore.BuyShopOffer(shopNode, offer)) return false;
                        DrawExploreShop();
                        return true;
                    });
            }
        }
        finally { openingOffer = false; }
    }

    protected override void OnDisable()
    {
        var node = shopNode;
        shopNode = null;
        if (offerPopup != null) UIManager.Instance?.CloseSpecificUI(offerPopup);
        offerPopup = null;
        // Restore the same prefab for a subsequent lobby use.
        SetCategoryVisible(true);
        if (node != null) app?.GetExploreManager()?.LeaveShop(node);
        base.OnDisable();
    }

    private void DrawShop()
    {
        if (app == null)
            return;

        items = app.GetShopItems(category);
        if (items == null)
            return;


        UIListDrawer.DrawList<UIShopSlot, ItemData>(
            items, (slot, item, index) =>
            {
                slot.SetItemData(item);
                if(slot.gameObject.activeSelf == false) 
                    slot.gameObject.SetActive(true);

                slot.OnClickedSlot -= OnClickedSlot;
                slot.OnClickedSlot += OnClickedSlot;
            },
            slot =>
            {
                slot.OnClickedSlot -= OnClickedSlot;
                if (slot.gameObject.activeSelf == true)
                    slot.gameObject.SetActive(false);
            },
            InitReplaceContentObject,
             SetContentChildObjectsCallback<UIShopSlot>
            );
    }

    // ShopUI.cs - OnClickedSlot
    public void OnClickedSlot(ItemData item)
    {
        if (shopNode != null || (offerPopup != null && offerPopup.gameObject.activeSelf)) return;
        if(item is ShopItem shopItem == false)
            return;

        offerPopup = UIManager.Instance.OpenShopPopUp(item, shopItem.Price, shopItem.CurrencyType);
    }

    public void OnCategoryButton(int categoryNumber)
    {
        if (shopNode != null) return;
        this.category = (ItemCategory)categoryNumber;
        DrawShop();
    }
}
