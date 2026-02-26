using System;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

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
    RECORD_SELECT,
    STAGE_RESULT,
    EXPLORE_RESULT,
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
    private Dictionary<Type, UiBase> uiInstances = new Dictionary<Type, UiBase>();

    [SerializeField] private UIDatabase uiDB;
    [SerializeField] private GameObject pcUIGroup;
    [SerializeField] private GameObject mobileUIGroup;

    public event Action OnJoinedLobby;                  // 로비 
    public event Action OnJoinedStage;                  // 스테이지 입장
    public event Action OnReturnedStageSelectStage;     // 스테이지 선택 씬 

    private Stack<UiBase> openedUIs = new Stack<UiBase>();
    private Queue<Action> popupQueue = new Queue<Action>();
    private bool isPopupOpening = false;
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

        uiInstances = Instance.uiInstances;
        uiDB = Instance.uiDB;
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
            currLocate = GameLocate.LOBBY;
            SetLobbyProgress();
        }
        else if (scene.name == "StageSelectScene")
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

    public T OpenUI<T>() where T : UiBase
    {
        Type uiType = typeof(T);

        // 켜져 있는지 확인 
        if (uiInstances.TryGetValue(uiType, out UiBase existingUI) && existingUI != null)
        {
            existingUI.gameObject.SetActive(true);
            openedUIs.Push(existingUI);

            return existingUI as T;
        }

        // DB에서 프리팹 찾기 
        UiBase prefab = uiDB.GetPrefab<T>();
        if (prefab != null)
        {
            IUIContainer container = UIRegistry.Get<IUIContainer>();
            Transform parent = container?.PopUpParent ?? null;
            if (parent == null)
                return null;

            T newUI = Instantiate(prefab.gameObject, parent).GetComponent<T>();
            uiInstances[uiType] = newUI;
            newUI.gameObject.SetActive(true);

            openedUIs.Push(newUI);

            return newUI;
        }

        return null;
    }

    public void CloseTopUI()
    {
        if (openedUIs.Count > 0)
        {
            var top = openedUIs.Pop();
            top.gameObject.SetActive(false);

            Invoke(nameof(ShowNextPopup), 0.1f);
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


    public void EnqueuePopup(Action popupAction)
    {
        popupQueue.Enqueue(popupAction);

        if (!isPopupOpening)
            ShowNextPopup();
    }

    private void ShowNextPopup()
    {
        if (popupQueue.Count > 0)
        {
            isPopupOpening = true;
            var nextPopup = popupQueue.Dequeue();
            nextPopup?.Invoke();
        }
        else
        {
            isPopupOpening = false;
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

    #region RESULT
    //-------------------------------------------------------------------------
    // STAGE RESULT
    //-------------------------------------------------------------------------

    public void ShowStageResultUI(bool isSuccess)
    {
        var ui = OpenUI<UIResultPage>();
        if (ui != null && ui.TryGetComponent<UIResultPage>(out var result))
        {
            result.Show(isSuccess);
        }
    }

    #endregion


    #region Sound 
    //-------------------------------------------------------------------------
    // Sound 
    //-------------------------------------------------------------------------


    public void ToggleSoundUI()
    {

    }

    public void ShowSoundControlUI()
    {

    }

    public void HidSoundControlUI()
    {

    }


    #endregion

    public void OpenStageInfo(MapNode node, StageInfo stageInfo)
    {
        var ui = OpenUI<UIStageInfo>();
        if (ui != null && ui.TryGetComponent<UIStageInfo>(out var target))
            target.SetStageData(node, stageInfo);
    }

    public void OpenRewardPopUp(ItemData[] itemDatas)
    {
        var ui = OpenUI<UIPopUpRewards>();
        if (ui != null && ui.TryGetComponent<UIPopUpRewards>(out var target))
            target.SetData(itemDatas);
    }

    public void OpenRewardPopUp(List<ItemData> itemDatas)
    {
        OpenRewardPopUp(itemDatas.ToArray());
    }

    public void OpenShopPopUp(ItemData itemData, int price, CurrencyType currencyType)
    {
        var ui = OpenUI<UIPopUpShop>();
        if (ui != null && ui.TryGetComponent<UIPopUpShop>(out var target))
            target.SetData(itemData, price, currencyType);
    }

    public void OpenItemPopUp(ItemData itemData)
    {
        if (itemData is EquipmentItem)
        {
            var ui = OpenUI<UIPopUpEquipment>();
            if (ui != null && ui.TryGetComponent<UIPopUpEquipment>(out var target))
                target.SetData(itemData);
        }
        else
        {
            var ui = OpenUI<UIPopUpItem>();
            if (ui != null && ui.TryGetComponent<UIPopUpItem>(out var target))
                target.SetData(itemData);
        }
    }

    public void OpenPausePopUp()
    {
        var ui = OpenUI<UIPopUpPause>();
    }


    public void OpenRecordInvenPopUp()
    {
        var ui = OpenUI<UIRecordInventory>();
        if (ui != null && ui.TryGetComponent<UIRecordInventory>(out var target))
        {
            target.SetRecordManager(AppManager.Instance?.GetRecordManager());
            target.RefreshUI();
        }
    }

    public void OpenRecordInfoPopUp(RecordData data)
    {
        var ui = OpenUI<UIRecordInfo>(); 
        if(ui != null && ui.TryGetComponent<UIRecordInfo>(out var target))
        {
            target.SetData(data); 
        }
    }

    //--------------------------------------------------------------------------
    // Damage Text
    //--------------------------------------------------------------------------
    public void DrawDamageText(Vector3 pos, float value, DamageEvent damageEvent)
    {
        if (currentUIGroup == null) return;

        Transform dtParent = currentUIGroup.transform.FindChildByName("DmgTxtParent");
        if (dtParent == null) return;

        DamageText dt = ObjectPooler.SpawnFromPool<DamageText>("DamageText", dtParent);
        dt?.DrawDamage(pos, value, damageEvent);
    }

}
