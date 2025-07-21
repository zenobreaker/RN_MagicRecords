using System.Collections.Generic;
using UnityEngine;

public class StageUIController : MonoBehaviour
{
    [SerializeField] private UIMapReplacer uiMapReplacer;
    [SerializeField] private UIStageInfo uiStageInfo; 

    private void Start()
    {
        uiMapReplacer.ReplaceStage();

        InitUIMapeReplace();
    }


    private void InitUIMapeReplace()
    {
        if (uiMapReplacer == null) return;

        List<UIMapNode> uiMapNodes = new List<UIMapNode>();

        uiMapReplacer.GetUIMapNodes(ref uiMapNodes);

        foreach (var node in uiMapNodes)
        {
            node.OnClicked += (stage) =>
            {
                uiStageInfo.SetStageData(stage);
                UIManager.Instance.OpenUI(uiStageInfo);
            };
        }
    }
}
