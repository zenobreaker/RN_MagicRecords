using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPopUpItem : UIPopUp
{
    [SerializeField] private ItemData item;
    [SerializeField] private Image itemIconImage; 
    [SerializeField] private TextMeshProUGUI itemNameText; 
    [SerializeField] private TextMeshProUGUI itemMainOptionText; 
    [SerializeField] private TextMeshProUGUI itemMainDescText;
    [SerializeField] private Button equipButton; 
    [SerializeField] private Button useButton; 
    [SerializeField] private Button exitButton; 

    private Button panelButton;

    protected void Awake()
    {
        panelButton = GetComponent<Button>(); 

        if(panelButton != null)
        {
            panelButton.onClick.AddListener(() =>
            {
                UIManager.Instance.ClosePopup();
            });
        }

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

    private void DrawItemOption()
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
