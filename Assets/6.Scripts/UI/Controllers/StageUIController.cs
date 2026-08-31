using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageUIController
    : UIController
{
    [Header("노드 배치자")]
    [SerializeField] private UIMapReplacer uiMapReplacer;

    [Header("UI 배경")]
    [SerializeField] private Image themeBg; 


    private ExploreManager exploreManager;
    

 
    protected override void OnEnable()
    {
        base.OnEnable();

        if (ManagerWaiter.TryGetManager(out UIManager ui))
        {
            Init(ui);
        }
        else
        {
            AppManager.Instance.OnAwaked += (() =>
            {
                if (bIsAwaked) return;
                ManagerWaiter.WaitForManager<UIManager>(uiManager =>
                {
                    Init(uiManager);
                });
                bIsAwaked = true;
            });
        }
    }

    private void Init(UIManager ui)
    {
        ui.OnReturnedStageSelect += UpdateCurrencies;
    }

    protected void Start()
    {
        exploreManager = AppManager.Instance.GetExploreManager();

        exploreManager.SafeInvoke(v => v.EnsureInitialized());

        exploreManager.OnStageClear += RefreshMainUI;

        InitUIMapReplace();

        if (exploreManager.CurrentState != ExploreState.ON_EXPLORE)
            exploreManager.ChangeState(ExploreState.ON_EXPLORE);
    }


    protected void OnDisable()
    {
        if (ManagerWaiter.TryGetManager<UIManager>(out UIManager ui))
            ui.OnReturnedStageSelect -= UpdateCurrencies;

        if (ManagerWaiter.TryGetManager(out CurrencyManager manager))
            manager.OnUpdatedCurrency -= UpdateCurrencies;
        if(AppManager.Instance != null && AppManager.Instance.GetExploreManager() != null )
            AppManager.Instance.GetExploreManager().OnStageClear -= RefreshMainUI;
    }

    private void InitUIMapReplace()
    {
        if (uiMapReplacer == null || exploreManager == null) return;

        List<UIMapNode> uiMapNodes = new List<UIMapNode>();
        // Set Map Node 
        {
            uiMapReplacer.ReplaceUINode(exploreManager.StageReplacer);
            uiMapReplacer.GetUIMapNodes(ref uiMapNodes);
            exploreManager.UpdateMapUIState(uiMapReplacer);
        }

        // Set Node Event
        foreach (var node in uiMapNodes)
        {
            if (node is UIStageMapNode sm)
            {
                sm.OnClicked += (stageInfo) =>
                {
                    UIManager.Instance.OpenStageInfo(node.Node, stageInfo);
                };
            }
        }
    }

    private void RefreshMainUI()
    {
        Debug.Assert(exploreManager != null);

        exploreManager.SafeInvoke(v => v.UpdateMapUIState(uiMapReplacer));


        string biomeName =  exploreManager.BiomeName;
        Sprite bg = DataBaseManager.Instance.GetThemeBgSptByBiome(biomeName); 

        ChangeBgSpt(bg);
    }

    private void ChangeBgSpt(Sprite bgSpt)
    {
        if (bgSpt == null || themeBg == null) return;

        themeBg.sprite = bgSpt;
    }

    #region Button Events
    public void OnBackButton()
    {
        AppManager.Instance.SaveExploreMap();

        SceneManager.LoadScene("Lobby");
    }

    public void OnRecordInvenButton()
    {
        UIManager.Instance.SafeInvoke(v=>v.OpenRecordInvenPopUp());
    }
    #endregion
}
