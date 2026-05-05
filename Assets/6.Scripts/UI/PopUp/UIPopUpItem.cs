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
    [SerializeField] protected Button useButton;
    [SerializeField] protected Button exitButton;

    protected override void Awake()
    {
        base.Awake();

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() =>
            {
                UIManager.Instance.CloseTopUI();
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

        Debug.Assert(LocalizationManager.Instance != null);
        if (itemNameText != null)
            itemNameText.text = LocalizationManager.Instance.GetText(item.name);

        if (itemMainDescText != null)
            itemMainDescText.text = LocalizationManager.Instance.GetText(item.description);

        DrawItemOption();
        if (item is not EquipmentItem)
        {
            //  equipButton.gameObject.SetActive(false);
            useButton.gameObject.SetActive(true);
        }
    }

    protected void DrawItemOption()
    {
        if (item == null) return;
        if (itemMainOptionText == null) return;

        Debug.Assert(LocalizationManager.Instance != null); 

        if (item is EquipmentItem equipment)
        {
            string key = equipment.modifier.GetLocalKey();
            itemMainOptionText.text = LocalizationManager.Instance.GetText(key) + " "+ equipment.modifier.GetValueAndValueType(); 
        }
    }

}
