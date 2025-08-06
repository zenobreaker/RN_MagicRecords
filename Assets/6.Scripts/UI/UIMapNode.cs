using System;
using UnityEngine;
using UnityEngine.UI;

public class UIMapNode 
    : MonoBehaviour
{
    [SerializeField] private MapNode mapNode;
    public MapNode Node { get { return mapNode; } }
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
        OnClicked?.Invoke(stageInfo); 
    }

    public void Refresh()
    {
        // 여기서 갈 수 있는 노드면 갈 수 있도록 처리 
        if (gameObject.TryGetComponent<Button>(out Button button))
        {
            // 갈 수 없는 곳이면 비활성화 UI  
            button.enabled = AppManager.Instance.EnableNode(mapNode.id);
        }

        if(stageInfo?.bIsCleared == true)
        {
            //TODO : 클리어시 잠금처리
        }
    }
}
