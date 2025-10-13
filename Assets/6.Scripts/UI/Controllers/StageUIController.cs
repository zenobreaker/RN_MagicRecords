using System.Collections.Generic;
using UnityEngine;

public class StageUIController
    : UIController
{
    [SerializeField] private UIMapReplacer uiMapReplacer;
    [SerializeField] private UIStageInfo uiStageInfo;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (ManagerWaiter.TryGetManager(out UIManager ui))
        {

            ui.OnReturnedStageSelectStage += UpdateCurrencies;

        }
        else
        {
            AppManager.Instance.OnAwaked += (() =>
            {
                ManagerWaiter.WaitForManager<UIManager>(uiManager =>
                {
                    ui.OnReturnedStageSelectStage += UpdateCurrencies;
                });
            });
        }
    }

    protected void Start()
    {
        InitUIMapeReplace();
    }

    protected void OnDisable()
    {
        if (ManagerWaiter.TryGetManager<UIManager>(out UIManager ui))
            ui.OnReturnedStageSelectStage -= UpdateCurrencies;
    }

    private void InitUIMapeReplace()
    {
        if (uiMapReplacer == null) return;

        AppManager.Instance.InitLevel();

        List<UIMapNode> uiMapNodes = new List<UIMapNode>();
        // Set Map Node 
        {
            uiMapReplacer.ReplaceUINode(AppManager.Instance.MapReplacer);
            uiMapReplacer.GetUIMapNodes(ref uiMapNodes);
        }

        // Set Node Event
        foreach (var node in uiMapNodes)
        {
            node.OnClicked += (stageInfo) =>
            {
                uiStageInfo.SetStageData(node.Node, stageInfo);
                UIManager.Instance.OpenUI(uiStageInfo);
            };
        }
    }
}
