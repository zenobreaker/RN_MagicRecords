using System;
using UnityEditor;
using UnityEngine;

public class UIStageMapNode 
    : UIMapNode
{
    [SerializeField] private StageInfo stageInfo;

    public event Action<StageInfo> OnClicked;

    public override void Init(MapNode mapNode)
    {
        base.Init(mapNode);

        stageInfo = AppManager.Instance.GetStageInfoMatchedMapNode(mapNode);
    }
    public void OnClick()
    {
        OnClicked?.Invoke(stageInfo);
    }
}
