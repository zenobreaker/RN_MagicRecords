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

    public override void DrawSlot()
    {
        base.DrawSlot();

        if (itemData == null)
            return;

        nameText.text = itemData.name;
    }
}
