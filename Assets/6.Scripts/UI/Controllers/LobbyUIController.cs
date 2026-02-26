using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class LobbyUIController
    : UIController
{
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
        UIManager.Instance.OpenUI<ShopUI>();
    }

    public void OnOpenCharInfo()
    {
        UIManager.Instance.OpenUI<CharacterInfoController>();
    }

    public void OnOpenSkillUI()
    {
        UIManager.Instance.OpenUI<SkillTreeController>();
    }

    public void OnOpenEnhanceUI()
    {
        UIManager.Instance.OpenUI<EnhanceUI>();
    }

    public void OnOpenInventoryUI()
    {
        UIManager.Instance.OpenUI<InventoryUI>();
    }

    public void OnOpenExploreUI()
    {
        UIManager.Instance.OpenUI<Exploration_Main_UI>();
    }
}
