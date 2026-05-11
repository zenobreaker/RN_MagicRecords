using UnityEngine;

public static class NodeRouter 
{
    // 💡 노드를 클릭했을 때 UI에 띄울 제목과 설명을 가져오는 만능 함수
    public static (string title, string description) GetNodeUIData(MapNodeInfo node)
    {
        string title = "";
        string desc = "";

        switch (node.type)
        {
            case StageType.Combat:
            case StageType.Boss_Combat:
                // 전투 노드면 StageDB에 물어봅니다.
                StageInfo combatData = AppManager.Instance.GetStageInfo(node.contentId);
                title = $"전투: {combatData.biome}"; // 예시
                desc = $"웨이브 수: {combatData.wave}";
                break;

            case StageType.Event:
                // 이벤트 노드면 EventDB에 물어봅니다.
                EventInfo eventData = AppManager.Instance.GetEventInfo(node.contentId);

                // 이벤트는 진입 전까지 비밀로 하거나, 이름을 보여줄 수 있습니다.
                title = "미지의 영역";
                desc = "알 수 없는 기운이 느껴집니다...";
                break;

            case StageType.Shop:
                title = "방랑 상인";
                desc = "재화를 소모하여 아이템을 얻습니다.";
                break;
        }

        return (title, desc);
    }

    // 💡 진입 버튼을 눌렀을 때 실제로 방을 실행하는 함수
    public static void EnterNode(MapNodeInfo node)
    {
        switch (node.type)
        {
            case StageType.Combat:
                StageInfo combatData = AppManager.Instance.GetStageInfo(node.contentId);
                GameManager.Instance.EnterStage(combatData);
                break;

            case StageType.Event:
                EventInfo eventData = AppManager.Instance.GetEventInfo(node.contentId);
                UIManager.Instance.OpenExploreEventPopup(eventData);
                break;
        }
    }
}
