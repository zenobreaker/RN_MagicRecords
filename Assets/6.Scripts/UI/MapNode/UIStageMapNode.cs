using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIStageMapNode 
    : UIMapNode
{
    [SerializeField] private StageInfo stageInfo;
    [SerializeField] private Image stageIcon; 

    public event Action<StageInfo> OnClicked;

    public override void Init(MapNode mapNode)
    {
        base.Init(mapNode);

        stageInfo = AppManager.Instance.GetStageInfoMatchedMapNode(mapNode);

        DrawStageIcon();
    }
    public void OnClick()
    {
        OnClicked?.Invoke(stageInfo);
    }

    private void DrawStageIcon()
    {
        if(stageInfo == null || stageIcon == null) return;

        Sprite icon = AppManager.Instance.GetStageIcon(stageInfo.type);

        if (icon != null)
        {
            stageIcon.sprite = icon;
            stageIcon.gameObject.SetActive(true);
        }
    }
}
