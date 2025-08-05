using System;
using UnityEngine;

public class UIMapNode 
    : MonoBehaviour
{
    [SerializeField] private MapNode mapNode;
    [SerializeField] private StageInfo stageInfo;

    public event Action<StageInfo> OnClicked;

    private void Start()
    {
        
    }

    public void Init(MapNode mapNode)
    {
        this.mapNode = mapNode;
        SetStage(mapNode.stageID);

        Refresh();
    }

    public void SetStage(int id)
    {
        stageInfo = AppManager.Instance.GetStageInfo(id);
    }

    public void OnClick()
    {
        if(mapNode != null)
            OnClicked?.Invoke(stageInfo); 
    }

    public void Refresh()
    {
        if(stageInfo.bIsCleared == true)
        {
            //TODO : 클리어시 잠금처리
        }
    }
}
