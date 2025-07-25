using System.Collections.Generic;
using UnityEngine;

public class StageUIController : MonoBehaviour
{
    [SerializeField] private UIMapReplacer uiMapReplacer;
    [SerializeField] private UIStageInfo uiStageInfo;

    private StageReplacer stageReplacer;

    private void Start()
    {
        InitUIMapeReplace();
    }


    private void InitUIMapeReplace()
    {
        if (uiMapReplacer == null) return;

        List<UIMapNode> uiMapNodes = new List<UIMapNode>();

        // Set Map Node 
        {
            uiMapReplacer.ReplaceStage();
            uiMapReplacer.GetUIMapNodes(ref uiMapNodes);
        }

        // Set Stage Data 
        {
            if(stageReplacer == null)
                stageReplacer = new StageReplacer(); 
            stageReplacer.AssignStages(uiMapNodes);
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
