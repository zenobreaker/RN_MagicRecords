using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPopUpItem : UIPopUpBase
{
    [SerializeField] protected ItemData item;
    [SerializeField] protected Image itemIconImage; 
    [SerializeField] protected TextMeshProUGUI itemNameText; 
    [SerializeField] protected TextMeshProUGUI itemMainOptionText; 
    [SerializeField] protected TextMeshProUGUI itemMainDescText;
    [SerializeField] protected Button equipButton; 
    [SerializeField] protected Button useButton; 
    [SerializeField] protected Button exitButton; 

    protected override void Awake()
    {
        base.Awake();

        if( exitButton != null)
        {
            exitButton.onClick.AddListener(() =>
            {
                UIManager.Instance.ClosePopup();
            });
        }

        if(equipButton != null)
        {
            equipButton.onClick.AddListener(() =>
            {
                if (item == null ) return;
                if (InventoryManager.Instance == null) return;

                var equipment = item as EquipmentItem;
                if (equipment == null) return;

                if(equipment.Eqeuipped == false)
                    InventoryManager.Instance.EquipItem(1, equipment);
                else
                    InventoryManager.Instance.UnequipItem(1, equipment);

                DrawPopUp();
            });
        }
    }

    public void SetData(ItemData item)
    {
        this.item = item;
        DrawPopUp();
    }

    protected override void DrawPopUp()
    {
        if (item == null) return;

        if (itemIconImage != null)
            itemIconImage.sprite = item.icon;

        if (itemNameText != null)
            itemNameText.text = item.name;

        if(itemMainDescText!=null)
            itemMainDescText.text = item.description;

        DrawEquipButton();
        DrawItemOption();
        if (item is EquipmentItem)
        {
            equipButton.gameObject.SetActive(true);
            useButton.gameObject.SetActive(false);
        }
        else 
        {
            equipButton.gameObject.SetActive(false);
            useButton.gameObject.SetActive(true);
        }
    }

    protected void DrawItemOption()
    {
        if(item == null) return;
        if (itemMainOptionText == null) return;

        if (item is EquipmentItem equipment)
        {
            itemMainOptionText.text = equipment.modifier.GetFullValue(); 
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
