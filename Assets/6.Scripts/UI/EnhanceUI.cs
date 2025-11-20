using UnityEngine;
using UnityEngine.UI;

public class EnhanceUI : UiBase
{
    [SerializeField] private EnhanceUI_EquipmentList enhanceUI_EquipmentList;
    [SerializeField] private EnhanceUI_UpgradeTarget enhanceUI_UpgradeTarget;
    [SerializeField] private EnhanceUI_EquipmentOption enhanceUI_EquipmentOption;
    [SerializeField] private EnhanceUI_Enhancement enhanceUI_Enhancement;

    [SerializeField] private Button enhanceButton;

    protected override void OnEnable()
    {
        base.OnEnable();

        ManagerWaiter.RegisterManagerEvent<InventoryManager>(this,
           onRegister: inventory =>
           {
               Init(inventory);
               RefreshUI();
           },
           onUnregister: inventory =>
           {
           });

    }

    protected override void OnDisable()
    {
        base.OnDisable();
      
        if (enhanceUI_EquipmentList != null)
        {
            enhanceUI_EquipmentList.OnClicked -= OnClickedItem;
        }

        if (enhanceUI_Enhancement != null)
            enhanceUI_Enhancement.OnEnhanced -= RefreshUI;
    }

    public override void RefreshUI()
    {
        base.RefreshUI();

        enhanceUI_EquipmentList?.RefreshUI();
        enhanceUI_UpgradeTarget?.RefreshUI();
        enhanceUI_EquipmentOption?.RefreshUI();
    }

    public void Init(InventoryManager inventory)
    {
        if (inventory == null)
            return;

        if(enhanceUI_Enhancement != null)
            enhanceUI_Enhancement.OnEnhanced += RefreshUI;

        if (enhanceUI_EquipmentList != null)
        {
            enhanceUI_EquipmentList.OnClicked += OnClickedItem;
            enhanceUI_EquipmentList.Init(inventory.GetItems(ItemCategory.EQUIPMENT));
        }

        if(enhanceUI_UpgradeTarget != null)
        {
            enhanceUI_UpgradeTarget.Init();
        }

        if(enhanceUI_EquipmentOption != null)
        {
            enhanceUI_EquipmentOption.Init();
        }
    }

    public void OnEnhance()
    {
        EquipmentItem equipment = enhanceUI_UpgradeTarget?.GetSelectedEquipment();

        enhanceUI_Enhancement?.TryEnhnace(equipment);
    }

    public void OnClickedItem(ItemData item)
    {
        enhanceUI_UpgradeTarget?.SetSelectItem(item);
        enhanceUI_EquipmentOption?.SetSelectItem(item);

        RefreshUI(); 
    }
}
