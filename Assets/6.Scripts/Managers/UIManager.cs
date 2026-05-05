using System;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
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
    [SerializeField] private InputActionReference cancelAction;

    public event Action OnJoinedLobby;                  // 로비 
    public event Action OnJoinedStage;                  // 스테이지 입장
    public event Action OnReturnedStageSelectStage;     // 스테이지 선택 씬 

    // 💡 [수정] 팝업 큐를 삭제하고 오직 하나의 스택(openedUIs)으로 모든 UI와 팝업을 관리합니다.
    private Stack<UiBase> openedUIs = new Stack<UiBase>();
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

    private void OnEnable()
    {
        Debug.Assert(cancelAction != null);
        cancelAction.action.performed += OnCancelPressed;
        cancelAction.action.Enable();
    }

    private void OnDisable()
    {
        Debug.Assert(cancelAction != null);
        cancelAction.action.performed -= OnCancelPressed;
        cancelAction.action.Disable();
    }

    protected override void SyncDataFromSingleton()
    {
        base.SyncDataFromSingleton();
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        uiInstances = Instance.uiInstances;
        uiDB = Instance.uiDB;
    }

    // 💡 [수정] ESC 처리 로직 통합: 스택 맨 위에 있는 것(팝업이든 UI든)을 하나씩 무조건 닫습니다.
    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        if (openedUIs.Count > 0)
        {
            CloseTopUI();
        }
        else
        {
            // 스택이 비어있고 인게임이라면 일시정지 팝업 오픈
            if (IsInGame()) OpenPausePopUp();
        }
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

    // 💡 [수정] 매개변수에 isPopup 플래그 추가 (기본값 false)
    public void OpenUI(UiBase ui, bool isPopup = false)
    {
        if (ui == null) return;

        // 💡 팝업이 아닌 일반 UI가 새로 열릴 경우, 기존에 열려있던 모든 UI/팝업을 닫습니다.
        if (!isPopup)
        {
            CloseAllOpenedUI();
        }

        ui.gameObject.SetActive(true);
        openedUIs.Push(ui);
    }

    // 💡 [수정] 매개변수에 isPopup 플래그 추가 (기본값 false)
    public T OpenUI<T>(bool isPopup = false) where T : UiBase
    {
        // 💡 팝업이 아닌 일반 UI가 새로 열릴 경우, 스택을 모두 비워줍니다.
        if (!isPopup)
        {
            CloseAllOpenedUI();
        }

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
            top.gameObject.SetActive(false); // 또는 top.CloseUI(); (UiBase 내부 구현에 맞게 사용)
        }
    }

    public void CloseAllOpenedUI()
    {
        while (openedUIs.Count > 0)
        {
            var top = openedUIs.Pop();
            top.gameObject.SetActive(false); // 또는 top.CloseUI(); 
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
    public void ShowStageResultUI(bool isSuccess)
    {
        // 💡 결과창은 팝업 형태이므로 true (또는 메인 UI라면 false로)
        var ui = OpenUI<UIResultPage>(true);
        if (ui != null && ui.TryGetComponent<UIResultPage>(out var result))
        {
            result.Show(isSuccess);
        }
    }
    #endregion

    #region Sound 
    public void ToggleSoundUI() { }
    public void ShowSoundControlUI() { }
    public void HidSoundControlUI() { }
    #endregion

    // 💡 여기서부터 팝업들은 명시적으로 isPopup = true 를 전달합니다.
    public void OpenStageInfo(MapNode node, StageInfo stageInfo)
    {
        var ui = OpenUI<UIStageInfo>(false); // 스테이지 인포는 메인 UI라면 false
        if (ui != null && ui.TryGetComponent<UIStageInfo>(out var target))
            target.SetStageData(node, stageInfo);
    }

    public void OpenRewardPopUp(ItemData[] itemDatas)
    {
        var ui = OpenUI<UIPopUpRewards>(true); // 팝업이므로 true
        if (ui != null && ui.TryGetComponent<UIPopUpRewards>(out var target))
            target.SetData(itemDatas);
    }

    public void OpenRewardPopUp(List<ItemData> itemDatas)
    {
        OpenRewardPopUp(itemDatas.ToArray());
    }

    public void OpenShopPopUp(ItemData itemData, int price, CurrencyType currencyType)
    {
        var ui = OpenUI<UIPopUpShop>(true); // 팝업이므로 true
        if (ui != null && ui.TryGetComponent<UIPopUpShop>(out var target))
            target.SetData(itemData, price, currencyType);
    }

    public void OpenItemPopUp(ItemData itemData)
    {
        if (itemData is EquipmentItem)
        {
            var ui = OpenUI<UIPopUpEquipment>(true); // 팝업이므로 true
            if (ui != null && ui.TryGetComponent<UIPopUpEquipment>(out var target))
                target.SetData(itemData);
        }
        else
        {
            var ui = OpenUI<UIPopUpItem>(true); // 팝업이므로 true
            if (ui != null && ui.TryGetComponent<UIPopUpItem>(out var target))
                target.SetData(itemData);
        }
    }

    public void OpenPausePopUp()
    {
        if (IsInGame())
        {
            var ui = OpenUI<UIPopUpPause>(true); // 일시정지도 팝업
        }
    }

    public void OpenRecordInvenPopUp()
    {
        var ui = OpenUI<UIRecordInventory>(true); // 팝업
        if (ui != null && ui.TryGetComponent<UIRecordInventory>(out var target))
        {
            target.SetRecordManager(AppManager.Instance?.GetRecordManager());
            target.RefreshUI();
        }
    }

    public void OpenRecordInfoPopUp(RecordData data)
    {
        var ui = OpenUI<UIRecordInfo>(true); // 팝업
        if (ui != null && ui.TryGetComponent<UIRecordInfo>(out var target))
        {
            target.SetData(data);
            target.RefreshUI();
        }
    }

    public void OpenExploreResultPopUp()
    {
        var ui = OpenUI<UITotalResultPopUp>(true); // 팝업
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