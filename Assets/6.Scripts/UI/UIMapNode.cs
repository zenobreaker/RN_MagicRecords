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
    }

    public void SetStage(int id)
    {
        this.mapNode.stageID = id;

        stageInfo = GameManager.Instance.GetStageInfo(id);
    }

    public void OnClick()
    {
        if(mapNode != null)
            OnClicked?.Invoke(stageInfo); 
    }
}
