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

        ManagerWaiter.WaitForManager<UIManager>(uiManager =>
        {
            uiManager.OnReturnedStageSelectStage += UpdateCurrencies;
        });

        InitUIMapeReplace();
    }

    protected void OnDisable()
    {
        ManagerWaiter.WaitForManager<UIManager>(uiManager =>
        {
            uiManager.OnReturnedStageSelectStage -= UpdateCurrencies;
        });
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
