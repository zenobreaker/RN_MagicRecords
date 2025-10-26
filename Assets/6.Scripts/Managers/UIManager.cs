using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public enum UIType
{
    StageInfo,
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
    public UiBase soundUI;

    public event Action OnJoinedLobby;
    public event Action OnReturnedStageSelectStage;

    private Dictionary<UIType, UiBase> uiTable = new Dictionary<UIType, UiBase>();
    private Stack<UiBase> openedUIs = new Stack<UiBase>();

    private Stack<UiBase> openPopUps = new();
    private GameLocate currLocate; 

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
        if (scene.name == "Stage")
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

    public void OpenUI(UiBase ui)
    {
        if (ui == null) return;
        ui.gameObject.SetActive(true);
        openedUIs.Push(ui);
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


    public void OpenItemPopUp(ItemData itemData)
    {
        var ui = OpenPopUp(popupUI);
        if (ui != null && ui.TryGetComponent<UIPopUpItem>(out var target))
            target.SetData(itemData);
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
}
