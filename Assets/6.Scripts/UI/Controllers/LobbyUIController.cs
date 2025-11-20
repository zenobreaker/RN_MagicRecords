using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class LobbyUIController
    : UIController
{
    [System.Serializable]
    public class UIUnit
    {
        public UIType type;
        public UiBase ui; 
    }

    public List<UIUnit> units = new();

    private void Awake()
    {
        ManagerWaiter.RegisterManagerEvent<UIManager>(this,
            onRegister: ui =>
            {
                if (ui != null)
               {
                    foreach (var unit in units)
                    {
                        ui.RegistUI(unit.type, unit.ui);
                    }
                }

            },
            onUnregister: ui => { });
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (AppManager.Instance == null) return;

        AppManager.Instance.OnAwaked += () =>
        {
            if (bIsAwaked) return; 
            ManagerWaiter.WaitForManager<UIManager>(uiManager =>
            {
                uiManager.OnJoinedLobby += UpdateCurrencies;    
            });
            bIsAwaked = true; 
        };
    }

    protected void OnDisable()
    {
        if (ManagerWaiter.TryGetManager<UIManager>(out UIManager ui))
            ui.OnJoinedLobby -= UpdateCurrencies;
    }

    public void OnOpenShop()
    {
        UIManager.Instance.OpenUI(UIType.SHOP);
    }

    public void OnOpenCharInfo()
    {
        UIManager.Instance.OpenUI(UIType.INFOMATION);
    }

    public void OnOpenSkillUI()
    {
        UIManager.Instance.OpenUI(UIType.SKILL);
    }

    public void OnOpenEnhanceUI()
    {
        UIManager.Instance.OpenUI(UIType.ENHANCEMENT);
    }

    public void OnOpenInventoryUI()
    {
        UIManager.Instance.OpenUI(UIType.INVENTORY);
    }

    public void OnOpenExploreUI()
    {
        UIManager.Instance.OpenUI(UIType.EXPLORE_MAIN);
    }
}
