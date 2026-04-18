using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIStageMapNode 
    : UIMapNode
{
    [SerializeField] private StageInfo stageInfo;
    [SerializeField] private Image stageIcon;

    // 선택 시 활성화 되는 UI 
    // [SerializeField] private Image outlineImage;

    public event Action<StageInfo> OnClicked;
    private MapNodeState currentState = MapNodeState.Locked;
    public MapNodeState CurrentState => currentState;

    public override void Init(MapNode mapNode)
    {
        base.Init(mapNode);

        stageInfo = AppManager.Instance.GetStageInfoMatchedMapNode(mapNode);

        DrawStageIcon();
    }

    // 매니저가 이 노드의 상태를 결정해서 던져줌 
    public void SetState(MapNodeState state)
    {
        currentState = state;

        if (stageIcon == null) return;

        switch (state)
        {
            case MapNodeState.Locked:
                stageIcon.color = new Color(0.3f, 0.3f, 0.3f, 1f); // 어두운 회색 (잠김)
                break;
            case MapNodeState.Selectable:
                stageIcon.color = Color.white; // 원래 색상 (선택 가능!)
                break;
            case MapNodeState.Current:
                stageIcon.color = Color.green; // 현재 위치 (초록색 등으로 강조)
                break;
            case MapNodeState.Cleared:
                stageIcon.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 반투명하게 (지나온 길)
                break;
        }
    }


    public void OnClick()
    {
        // 잠겨있거나 이미 지나온 길이면 클릭이벤트 무시
        if(currentState == MapNodeState.Locked || currentState == MapNodeState.Cleared)
            return;

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
        else
        {
            stageIcon.gameObject.SetActive(false);
        }
    }
}
