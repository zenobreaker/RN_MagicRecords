using System;
using System.Collections.Generic;
using UnityEngine;

public class StageUIController : MonoBehaviour
{
    [SerializeField] private UIMapReplacer uiMapReplacer;
    [SerializeField] private UIStageInfo uiStageInfo;

    private void Start()
    {
        InitUIMapeReplace();
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
                uiStageInfo.SetStageData(stageInfo);
                UIManager.Instance.OpenUI(uiStageInfo);
            };
        }
    }
}
