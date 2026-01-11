using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public enum UIType
{
    NONE, 
    INVENTORY, 
    ENHANCEMENT,
    SHOP,
    INFOMATION,
    SKILL,
    STAGE_INTO, 
    EXPLORE_MAIN,
}

public enum GameLocate
{
    TITLE,
    LOBBY,
    STAGE_SELCT,
    IN_GAME,
}

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private GameObject mobileUIGroup;
    [SerializeField] private GameObject pcUIGroup;
    [SerializeField] private GameObject popupUI;
    [SerializeField] private GameObject popupUIReward;
    [SerializeField] private GameObject popupUIShop;
    [SerializeField] private GameObject popupUIEquipment;


    public UiBase soundUI;

    public event Action OnJoinedLobby;
    public event Action OnJoinedStage; 
    public event Action OnReturnedStageSelectStage;

    private Dictionary<UIType, UiBase> uiTable = new Dictionary<UIType, UiBase>();
    private Stack<UiBase> openedUIs = new Stack<UiBase>();
    private Stack<UiBase> openPopUps = new();
    private GameLocate currLocate;
    private GameObject currentUIGroup; 

    public bool IsLTitle() => currLocate == GameLocate.TITLE;
    public bool IsLobby() => currLocate == GameLocate.LOBBY;
    public bool IsInGame() => currLocate == GameLocate.IN_GAME;

    protected override void Awake()
    {
        base.Awake();

        if (Instance == this)
            SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    protected override void SyncDataFromSingleton()
    {
        base.SyncDataFromSingleton();
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        mobileUIGroup = Instance.mobileUIGroup;
        pcUIGroup = Instance.pcUIGroup;

        soundUI = Instance.soundUI;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        openedUIs.Clear();
        if (scene.name == "Stage" || scene.name == "UnitTest")
        {
            currLocate = GameLocate.IN_GAME;
            SetStageUserInterface();
        }
        else if (scene.name == "Lobby")
        {
            currLocate =  GameLocate.LOBBY;
            SetLobbyProgress();
        }
        else if(scene.name == "StageSelectScene")
        {
            currLocate = GameLocate.STAGE_SELCT;
            SetStageSelectScene();
        }
    }

    public void RegistUI(UIType type, UiBase ui)
    {
        uiTable[type] = ui;
    }

    public void OpenUI(UiBase ui)
    {
        if (ui == null) return;
        ui.gameObject.SetActive(true);
        openedUIs.Push(ui);
    }

    public void OpenUI(UIType type)
    {
        if(uiTable.TryGetValue(type, out var ui))
            OpenUI(ui);
    }

    public void CloseTopUI()
    {
        if (openedUIs.Count > 0)
        {
            var top = openedUIs.Pop();
            top.CloseUI();
        }
    }

    public void CloseAllOpenedUI()
    {
        while (openedUIs.Count > 0)
        {
            var top = openedUIs.Pop();
            top.CloseUI();
        }
    }

    private void SetStageUserInterface()
    {
        GameObject currentObject = null;
        if (Application.isMobilePlatform == false)
            currentObject = pcUIGroup;
        else
            currentObject = mobileUIGroup;

        currentUIGroup = currentObject;
        if (currentObject == null) return;
        
        var go = Instantiate<GameObject>(currentObject);
        go?.SetActive(true);
    }

    private void SetStageSelectScene()
    {
        OnReturnedStageSelectStage?.Invoke();
    }

    private void SetLobbyProgress()
    {
        OnJoinedLobby?.Invoke();
    }

    #region GameOver
    //-------------------------------------------------------------------------
    // Game Over 
    //-------------------------------------------------------------------------

    public void ShowGameOverUI()
    {

    }

    public void HideGameOverUI()
    {

    }
    #endregion


    #region Sound 
    //-------------------------------------------------------------------------
    // Sound 
    //-------------------------------------------------------------------------


    public void ToggleSoundUI()
    {
        bool? isActive = soundUI?.gameObject.activeSelf;
        soundUI?.gameObject.SetActive(!isActive.Value);
    }

    public void ShowSoundControlUI()
    {
        soundUI?.gameObject.SetActive(true);
    }

    public void HidSoundControlUI()
    {
        soundUI?.gameObject.SetActive(false);
    }


    #endregion
    public void OpenRewardPopUp(ItemData[] itemDatas)
    {
        var ui = OpenPopUp(popupUIReward);
        if (ui != null && ui.TryGetComponent<UIPopUpRewards>(out var target))
            target.SetData(itemDatas);
    }

    public void OpenRewardPopUp(List<ItemData> itemDatas)
    {
        OpenRewardPopUp(itemDatas.ToArray());
    }

    public void OpenShopPopUp(ItemData itemData, int price, CurrencyType currencyType)
    {
        var ui = OpenPopUp(popupUIShop);
        if (ui != null && ui.TryGetComponent<UIPopUpShop>(out var target))
            target.SetData(itemData, price, currencyType);
    }

    public void OpenItemPopUp(ItemData itemData)
    {
        if (itemData is EquipmentItem)
        {
            var ui = OpenPopUp(popupUIEquipment);
            if (ui != null && ui.TryGetComponent<UIPopUpEquipment>(out var target))
                target.SetData(itemData);
        }
        else
        {
            var ui = OpenPopUp(popupUI);
            if (ui != null && ui.TryGetComponent<UIPopUpItem>(out var target))
                target.SetData(itemData);
        }
    }

    private GameObject OpenPopUp(GameObject popUpObj)
    {
        if (popUpObj == null) return null;
        IUIContainer container = UIRegistry.Get<IUIContainer>();
        Transform parent = container?.PopUpParent ?? null;
        if (parent == null) 
            return null;

        GameObject ui = Instantiate(popUpObj, parent);
        if (ui != null && ui.TryGetComponent<UIPopUp>(out var target))
        {
            openPopUps.Push(target);
            return ui;
        }
        return null;
    }

    public void ClosePopup()
    {
        if (openPopUps.Count > 0)
        {
            var top = openPopUps.Pop();
            if (top != null)
            {
                Destroy(top.gameObject);
            }
        }
    }

    public void DrawDamageText(Vector3 pos, float value, DamageEvent damageEvent)
    {
        if(currentUIGroup == null) return;

        Transform dtParent = currentUIGroup.transform.FindChildByName("DmgTxtParent");
        if(dtParent == null) return;

        DamageText dt = ObjectPooler.SpawnFromPool<DamageText>("DamageText", dtParent);
        dt?.DrawDamage(pos, value, damageEvent);
    }
}
