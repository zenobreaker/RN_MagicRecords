using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPopUpEquipment : UIPopUpItem
{
    [SerializeField] protected Button equipButton;
    [SerializeField] private Button enhanceButton; 

    protected override void Awake()
    {
        base.Awake();

        if (equipButton != null)
        {
            equipButton.onClick.AddListener(() =>
            {
                if (item == null) return;
                if (InventoryManager.Instance == null) return;

                var equipment = item as EquipmentItem;
                if (equipment == null) return;

                if (equipment.Eqeuipped == false)
                    InventoryManager.Instance.EquipItem(1, equipment);
                else
                    InventoryManager.Instance.UnequipItem(1, equipment);

                DrawPopUp();
            });
        }

        if (enhanceButton != null)
        {
            enhanceButton.onClick.AddListener(() =>
            {
                if (item == null) return;
                var equipment = item as EquipmentItem;
                if (equipment == null) return;
                UIManager.Instance.ClosePopup();
                UIManager.Instance.OpenUI(UIType.ENHANCEMENT);
            });
        }
    }

    protected override void DrawPopUp()
    {
        base.DrawPopUp();

        DrawEquipButton();

        if (item == null) return;
        var equipment = item as EquipmentItem;
        if (equipment == null) return;
        if (enhanceButton != null)
        {
            enhanceButton.gameObject.SetActive(true);
        }
    }

    private void DrawEquipButton()
    {
        if (item == null) return;

        if (item is EquipmentItem equipment)
        {
            TextMeshProUGUI tm = equipButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tm == null) return;
            if (equipment.Eqeuipped)
                tm.text = "Unequip";
            else
                tm.text = "Equip";
        }
    }
}
