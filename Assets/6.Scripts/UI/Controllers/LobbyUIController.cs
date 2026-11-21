using System.Collections.Generic;
using UnityEngine;

public class LobbyUIController
    : UIController
{
    private void Start()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.ChangeBGM("Lobby1");
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (AppManager.Instance == null)
        {
            return;
        }

        AppManager.Instance.OnAwaked += () =>
        {
            if (bIsAwaked)
            {
                return;
            }

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
        {
            ui.OnJoinedLobby -= UpdateCurrencies;
        }
    }

    public void OnOpenShop()
    {
        // 로비 상점 임시 잠금.
        UIManager.Instance?.ShowToast("준비중입니다");
    }

    public void OnOpenCharInfo()
    {
        UIManager.Instance.OpenUI<CharacterInfoController>();
    }

    /// <summary>가이드 드론의 프로필과 기능을 확인하는 팝업을 엽니다.</summary>
    public void OnOpenGuideDroneUI()
    {
        UIManager.Instance.SafeInvoke(ui => ui.OpenGuideDronePopUp());
    }

    public void OnOpenSkillUI()
    {
        UIManager.Instance.OpenUI<SkillTreeController>();
    }

    public void OnOpenEnhanceUI()
    {
        // 로비 강화 임시 잠금.
        UIManager.Instance?.ShowToast("준비중입니다");
    }

    public void OnOpenInventoryUI()
    {
        UIManager.Instance.OpenUI<InventoryUI>();
    }

    public void OnOpenExploreUI()
    {
        UIManager.Instance.OpenUI<Exploration_Main_UI>();
    }

    public void OnOpenOptionUI()
    {
        UIManager.Instance.OpenOptionPopUp();
    }
}
