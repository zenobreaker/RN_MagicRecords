using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    [SerializeField] private InputActionReference submitAction;

    public event Action OnJoinedLobby;                  // 로비 
    //public event Action OnJoinedStage;                  // 스테이지 입장
    public event Action OnReturnedStageSelect;     // 스테이지 선택 씬 

    // 💡 [수정] 팝업 큐를 삭제하고 오직 하나의 스택(openedUIs)으로 모든 UI와 팝업을 관리합니다.
    private Stack<UiBase> openedUIs = new Stack<UiBase>();
    private GameLocate currLocate;
    private GameObject currentUIGroup;

    [Header("Toast Settings")]
    [SerializeField] private UIToast toastPrefab;
    [SerializeField] private int maxToastCount = 3; // 화면에 띄울 최대 토스트 개수 

    private UIToast[] toastPool;
    private int currentToastIndex = 0; 

    public bool IsLTitle() => currLocate == GameLocate.TITLE;
    public bool IsLobby() => currLocate == GameLocate.LOBBY;
    public bool IsInGame() => currLocate == GameLocate.IN_GAME;

    protected override void Awake()
    {
        base.Awake();

        if (Instance == this)
            SceneManager.sceneLoaded += HandleSceneLoaded;

        InitToastPool();
    }

    private void OnEnable()
    {
        Debug.Assert(cancelAction != null);
        cancelAction.action.performed += OnCancelPressed;
        cancelAction.action.Enable();

        Debug.Assert(submitAction != null);
        submitAction.action.performed += OnSubmitPressed;
        submitAction.action.Enable(); 
    }

    private void OnDisable()
    {
        Debug.Assert(cancelAction != null);
        cancelAction.action.performed -= OnCancelPressed;
        cancelAction.action.Disable();

        Debug.Assert(submitAction != null);
        submitAction.action.performed -= OnSubmitPressed;
        submitAction.action.Disable();
    }

    protected override void SyncDataFromSingleton()
    {
        base.SyncDataFromSingleton();
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        uiInstances = Instance.uiInstances;
        uiDB = Instance.uiDB;
    }

    private void InitToastPool()
    {
        if (toastPrefab == null) return;

        toastPool = new UIToast[maxToastCount];

        // 💡 1. 토스트 전용 '최상단 캔버스'를 코드로 직접 생성합니다.
        GameObject toastCanvasObj = new GameObject("GlobalToastCanvas");

        // UIManager가 DontDestroyOnLoad라면, 토스트 캔버스도 씬 전환 시 파괴되지 않게 묶어줍니다.
        // (만약 UIManager의 자식으로 넣는다면 UIManager.transform을 부모로 설정해도 되지만, 
        // 캔버스는 보통 Root에 두는 것이 렌더링에 안전합니다.)
        DontDestroyOnLoad(toastCanvasObj);

        // 💡 2. 캔버스 컴포넌트 세팅 (무조건 맨 위에 보이게 9999 설정!)
        Canvas canvas = toastCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        // 해상도 대응을 위한 CanvasScaler 추가
        CanvasScaler scaler = toastCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); // 프로젝트 해상도에 맞게 수정하세요

        // UI 터치/클릭을 막지 않도록 GraphicRaycaster는 굳이 추가하지 않거나,
        // 필요하다면 toastCanvasObj.AddComponent<GraphicRaycaster>(); 를 넣어줍니다.

        Transform toastParent = toastCanvasObj.transform;

        for (int i = 0; i < maxToastCount; i++)
        {
            toastPool[i] = Instantiate(toastPrefab, toastParent);
            toastPool[i].gameObject.SetActive(false);
        }
    }

    // 💡 어디서든 UIManager.Instance.ShowToast("돈이 부족합니다!"); 로 호출!
    public void ShowToast(string message)
    {
        if (toastPool == null || toastPool.Length == 0) return;

        // 현재 인덱스의 토스트를 꺼내서 메시지를 띄움
        UIToast toast = toastPool[currentToastIndex];
        toast.ShowMessage(message);

        // 💡 [핵심] 인덱스를 다음으로 넘깁니다. 끝에 도달하면 다시 0으로 돌아옵니다! (원형 큐)
        currentToastIndex = (currentToastIndex + 1) % maxToastCount;
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

    // 💡 엔터/스페이스를 눌렀을 때
    private void OnSubmitPressed(InputAction.CallbackContext context)
    {
        // 최상단에 열려있는 팝업이나 UI가 있다면
        if (openedUIs.Count > 0)
        {
            var topUI = openedUIs.Peek(); // 닫지는 않고 맨 위 UI를 가져오기만 함

            // 해당 UI의 OnSubmit 함수 실행!
            topUI.OnSubmit();
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
            
            existingUI.transform.SetAsLastSibling();

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

    /// <summary>
    /// 스택의 최상단이 아니더라도 특정 UI를 찾아 안전하게 닫고 스택에서 제거합니다.
    /// </summary>
    public void CloseSpecificUI(UiBase targetUI)
    {
        if (openedUIs.Count == 0 || targetUI == null) return;

        // 1. 타겟이 마침 스택 맨 위라면 기존 함수를 재사용하여 깔끔하게 Pop
        if (openedUIs.Peek() == targetUI)
        {
            CloseTopUI();
            return;
        }

        // 2. 타겟이 스택 중간에 껴있다면? (Stack은 중간 삭제가 안 되므로 분해 후 재조립)
        if (openedUIs.Contains(targetUI))
        {
            // 스택을 리스트로 변환 (주의: 인덱스 0이 스택의 Top입니다)
            List<UiBase> tempUiList = new List<UiBase>(openedUIs);

            // 타겟 제거
            tempUiList.Remove(targetUI);

            // 다시 스택으로 만들기 위해 순서를 뒤집어줍니다 (Top이 마지막으로 들어가야 하므로)
            tempUiList.Reverse();

            // 스택 재할당
            openedUIs = new Stack<UiBase>(tempUiList);

            // UI 끄기
            targetUI.gameObject.SetActive(false);
        }
    }

    private void SetStageUserInterface()
    {
        GameObject currentObject = null;
        if (UnityEngine.Application.isMobilePlatform == false)
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
        OnReturnedStageSelect?.Invoke();
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
    public void OpenStageInfo(MapNode node, MapNodeInfo mapNodeInfo)
    {
        var ui = OpenUI<UIStageInfo>(false); // 스테이지 인포는 메인 UI라면 false
        if (ui != null && ui.TryGetComponent<UIStageInfo>(out var target))
            target.SetStageData(node, mapNodeInfo);
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

    public void OpenRecordSelectPopUp(List<RecordData> records, bool canReroll, RecordUIMode mode)
    {
        var ui = OpenUI<RecordUI>(true);
        if(ui != null && ui.TryGetComponent<RecordUI>(out var target))
        {
            target.SetData(records, canReroll, mode);
        }
    }

    public void OpenExploreResultPopUp()
    {
        var ui = OpenUI<UITotalResultPopUp>(true); // 팝업
    }

    public void OpenExploreEventPopup(EventInfo eventInfo)
    {
        var ui = OpenUI<UIPopupEventScreen>(true);  
        if(ui!= null && ui.TryGetComponent<UIPopupEventScreen>(out var target))
        {
            target.SetData(eventInfo);
        }
    }

    public void OpenOptionPopUp()
    {
        UIPopUpOption ui = OpenUI<UIPopUpOption>(true);
        if(ui != null && ui.TryGetComponent<UIPopUpOption>(out UIPopUpOption target))
        {

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