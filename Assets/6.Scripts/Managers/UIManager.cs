using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public enum UIType
{
    StageInfo,
}


public class UIManager : Singleton<UIManager>
{

    private Dictionary<UIType, UiBase> uiTable = new Dictionary<UIType, UiBase>();
    private Stack<UiBase> openedUIs = new Stack<UiBase>();

    [SerializeField] private GameObject mobileUIGroup;
    [SerializeField] private GameObject pcUIGroup;

    [SerializeField] private GameObject popupUI;


    private Stack<UiBase> openPopUps = new();
    public UiBase soundUI; 

    protected override void Awake()
    {
        base.Awake();

        if(Instance == this)
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
        if(scene.name == "Stage")
        {
            SetStageUserInterface();
        }
    }

    public void TogglePauseMenu()
    {
        
    }

    public void OpenUI (UiBase ui)
    {
        if (ui == null) return; 
        ui.gameObject.SetActive(true);
        openedUIs.Push(ui); 
    }

    public void CloseTopUI()
    {
        if(openedUIs.Count > 0)
        {
            var top = openedUIs.Pop();
            top.CloseUI();
        }
    }

    public void CloseAllOpenedUI()
    {
        while(openedUIs.Count > 0)
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


    public void OpenPopUp(ItemData data)
    {
        if (popupUI == null) return;
        UIController uc = FindAnyObjectByType<UIController>(); 
        var ui = Instantiate(popupUI, uc.transform);
        if(ui != null && ui.TryGetComponent<UIPopUpItem>(out var popUp) && uc != null)
        {
            popUp.SetData(data);
            if (ui.TryGetComponent<UIPopUp>(out var target))
                openPopUps.Push(target);
        }
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
