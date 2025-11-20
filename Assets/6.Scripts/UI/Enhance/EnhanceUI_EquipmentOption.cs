using TMPro;
using UnityEngine;

public class EnhanceUI_EquipmentOption : UiBase
{
    [SerializeField] private TextMeshProUGUI mainOptionText;

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
        if (mainOptionText == null)
            return;

        if (selectedItem == null)
        {
            mainOptionText.text = string.Empty;
            return;
        }


        if(selectedItem is EquipmentItem equipment)
            mainOptionText.text = equipment.GetMainOptionText();
    }

    public void SetSelectItem(ItemData item)
    {
        if (selectedItem != item)
            selectedItem = item;
        else
            selectedItem = null;
    }
}
