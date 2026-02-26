using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageUIController
    : UIController
{
    [SerializeField] private UIMapReplacer uiMapReplacer;

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
        ui.OnReturnedStageSelectStage += UpdateCurrencies;
    }

    protected void Start()
    {
        // 데이터가 로드 안되어 있다면 강제로 Init 
        var em = AppManager.Instance.GetExploreManager();
        em?.EnsureInitialized();

        InitUIMapReplace();

        // 배치가 끝났다면 탐사 상태로 변경 
        if (em.CurrentState != ExploreState.ON_EXPLORE)
            em?.ChangeState(ExploreState.ON_EXPLORE);
    }

    protected void OnDisable()
    {
        if (ManagerWaiter.TryGetManager<UIManager>(out UIManager ui))
            ui.OnReturnedStageSelectStage -= UpdateCurrencies;

        if (ManagerWaiter.TryGetManager(out CurrencyManager manager))
            manager.OnUpdatedCurrency -= UpdateCurrencies;
    }

    private void InitUIMapReplace()
    {
        if (uiMapReplacer == null) return;

        List<UIMapNode> uiMapNodes = new List<UIMapNode>();
        // Set Map Node 
        {
            uiMapReplacer.ReplaceUINode(AppManager.Instance.GetMapReplacer());
            uiMapReplacer.GetUIMapNodes(ref uiMapNodes);
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

    public void OnBackButton()
    {
        AppManager.Instance.SaveExploreMap();

        SceneManager.LoadScene(0);
    }

    public void OnRecordInvenButton()
    {
        UIManager.Instance?.OpenRecordInvenPopUp();
    }
}
