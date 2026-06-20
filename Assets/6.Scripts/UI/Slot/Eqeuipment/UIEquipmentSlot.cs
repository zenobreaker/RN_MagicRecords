using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIEquipmentSlot : UIItemSlot
{
    [SerializeField] private TextMeshProUGUI nameText;

    public void Awake()
    {
        itemImage = itemImage = transform.FindChildByName("Icon").GetComponent<Image>();
    }

    public override void Refresh()
    {
        base.Refresh();

        if (itemData == null)
            return;

        string enhanceAndName = "";
        string localName = itemData.LocalizedName; 
        if (itemData is EquipmentItem equip)
        {
            if (equip.Enhance > 0)
                enhanceAndName = "+" + equip.Enhance + " " + localName;
            else
                enhanceAndName = localName;
        }
        else
            enhanceAndName = localName;
        
        nameText.text = enhanceAndName;
    }
}
