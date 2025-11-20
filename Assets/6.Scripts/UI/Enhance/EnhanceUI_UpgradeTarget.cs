using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhanceUI_UpgradeTarget : UiBase
{
    [SerializeField] private Sprite emptyIcon;
    [SerializeField] private Image selcetedItemIcon;
    [SerializeField] private TextMeshProUGUI selcetedItemNameText;
    [SerializeField] private TextMeshProUGUI selcetedItemEnhanceText;

    private ItemData selectedItem;

    public void Init()
    {
        selectedItem = null;
    }

    public override void RefreshUI()
    {
        Draw();
    }

    public void Draw()
    {
        if (selectedItem == null)
        {
            if (selcetedItemIcon != null)
                selcetedItemIcon.sprite = emptyIcon;
            if (selcetedItemNameText != null)
                selcetedItemNameText.text = "";
            if (selcetedItemEnhanceText != null)
                selcetedItemEnhanceText.text = "";
            return;
        }

        if (selcetedItemIcon != null)
            selcetedItemIcon.sprite = selectedItem.icon;
        if (selcetedItemNameText != null)
            selcetedItemNameText.text = selectedItem.name;

        if (selcetedItemEnhanceText != null)
        {
            if (selectedItem is EquipmentItem equipment && isMaxEnhance(equipment))
                selcetedItemEnhanceText.text = $"{equipment.Enhance} >> {equipment.Enhance + 1} ";
            else
                selcetedItemEnhanceText.text = "MAX";
        }
    }


    private bool isMaxEnhance(EquipmentItem item)
    {
        if (item == null) return false;

        if (item.Enhance < Constants.ENHANCE_MAX_COUNT)
            return true;

        return false;
    }

    public void SetSelectItem(ItemData item)
    {
        if (selectedItem != item)
            selectedItem = item;
        else
            selectedItem = null;
    }

    public EquipmentItem GetSelectedEquipment()
    {
        return selectedItem as EquipmentItem;
    }
}
