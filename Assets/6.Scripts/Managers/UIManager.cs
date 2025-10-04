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
    IN_GAME,
}

public class UIManager : Singleton<UIManager>
{

    private Dictionary<UIType, UiBase> uiTable = new Dictionary<UIType, UiBase>();
    private Stack<UiBase> openedUIs = new Stack<UiBase>();

    [SerializeField] private GameObject mobileUIGroup;
    [SerializeField] private GameObject pcUIGroup;
    [SerializeField] private GameObject popupUI;
    [SerializeField] private GameObject popupUIReward;
    public UiBase soundUI;

    public event Action OnJoinedLobby;

    private Stack<UiBase> openPopUps = new();
    private GameLocate currLocate; 

    public bool IsLTitle() => currLocate == GameLocate.TITLE;
    public bool IsLobby() => currLocate == GameLocate.LOBBY;
    public bool IsInGame() => currLocate == GameLocate.IN_GAME;

    protected override void Awake()
    {
        base.Awake();

        if (Instance == this)
            SceneManager.sceneLoaded += HandeSceneLoaded;
    }

    protected override void SyncDataFromSingleton()
    {
        base.SyncDataFromSingleton();
        SceneManager.sceneLoaded -= HandeSceneLoaded;

        mobileUIGroup = Instance.mobileUIGroup;
        pcUIGroup = Instance.pcUIGroup;

        soundUI = Instance.soundUI;
    }

    private void HandeSceneLoaded(Scene scene, LoadSceneMode mode)
    {
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
    }

    public void TogglePauseMenu()
    {

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
        UIController uc = FindAnyObjectByType<UIController>();
        if (uc == null) return null;

        GameObject ui = uc.CreatePopUpUI(popUpObj);
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
