using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIStageMapNode : UIMapNode
{
    // 💡 1. StageInfo -> MapNodeInfo로 가볍게 교체!
    [SerializeField] private MapNodeInfo nodeInfo;
    [SerializeField] private Image stageIcon;

    // 선택 시 활성화 되는 UI 
    // [SerializeField] private Image outlineImage;

    // 💡 2. 클릭 이벤트도 껍데기(MapNodeInfo)를 던지도록 수정
    public event Action<MapNodeInfo> OnClicked;

    private MapNodeState currentState = MapNodeState.Locked;
    public MapNodeState CurrentState => currentState;

    public override void Init(MapNode mapNode)
    {
        base.Init(mapNode);

        // 💡 3. AppManager에서 MapNodeInfo를 받아오도록 연결
        // (이전 단계에서 AppManager의 이 함수 반환형을 MapNodeInfo로 수정하셨을 겁니다!)
        nodeInfo = AppManager.Instance.GetNodeInfoMatchedMapNode(mapNode);

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
#if UNITY_EDITOR
        if (AppManager.Instance.Cheat)
        {
            // 💡 4. 치트 클릭 시에도 nodeInfo 전달
            OnClicked?.Invoke(nodeInfo);
            return;
        }
#endif
        // 잠겨있거나 이미 지나온 길이면 클릭이벤트 무시
        if (currentState == MapNodeState.Locked || currentState == MapNodeState.Cleared)
            return;

        // 💡 5. 정상 클릭 시 nodeInfo 전달
        OnClicked?.Invoke(nodeInfo);
    }

    private void DrawStageIcon()
    {
        // 💡 6. stageInfo 검사를 nodeInfo 검사로 변경
        if (nodeInfo == null || stageIcon == null) return;

        // nodeInfo.type(전투, 이벤트, 상점 등)에 맞춰서 아이콘을 가져옵니다!
        Sprite icon = AppManager.Instance.GetStageIcon(nodeInfo.type);

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