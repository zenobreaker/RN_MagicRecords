using System;
using System.Collections.Generic;

public class StageReplacer
{
    private Dictionary<int, StageInfo> nodeToStage = new(); // key : node id value : stage
    private Dictionary<int, int> nodeIdToStageId = new();

    public string currentChapterBiome;

    public void StartChapter(int chapter)
    {
        // 1챕터에 진입하면 '숲'인지 '습지'인지 전체 테마를 하나 뽑아둡니다.
        currentChapterBiome = AppManager.Instance.GetRandomBiome(chapter);

        // 이후 노드(맵)들을 생성하고 StageReplacer.AssignStages()를 호출!
    }

    public void AssignStages(List<List<MapNode>> levels)
    {
        nodeToStage.Clear();
        nodeIdToStageId.Clear();


        SO_Biome biomeData = AppManager.Instance.GetBiomeData(currentChapterBiome);

        int maxMapCount = biomeData.possibleRoomPrefabs.Count;

        for (int level = 0; level < levels.Count; level++)
        {
            if (level == 0)
            {
                nodeToStage[0] = new StageInfo();
                nodeIdToStageId[0] = 0;
                continue;
            }
            bool isLastLevel = (level == levels.Count - 1);

            foreach (var node in levels[level])
            {
                StageInfo stageInfo;

                if (isLastLevel)
                {
                    // 보스용 스테이지 풀에서 랜덤추출 
                    stageInfo = AppManager.Instance.CreateRandomBossStageInfo();
                }
                else
                    stageInfo = AppManager.Instance.CreateRandomStageInfo();

                stageInfo.biome = currentChapterBiome;

                if(maxMapCount > 0)
                {
                    // 보스방은 보스 전용 맵을 써야 한다면 분기 처리가 필요하지만, 
                    // 일반 노드라면 여기서 프리팹 인덱스를 결정해 줍니다.
                    stageInfo.mapIndex = UnityEngine.Random.Range(0, maxMapCount); 
                }

                nodeToStage[node.id] = stageInfo;
                nodeIdToStageId[node.id] = stageInfo.id;
            }
        }
    }

    public void RestoreStages(StageNodeData stageNodeData)
    {
        nodeToStage.Clear();
        nodeIdToStageId.Clear();

        // 💡 저장 데이터를 불러올 때도 0번(시작점)은 무조건 기본 생성.
        nodeToStage[0] = new StageInfo();
        nodeIdToStageId[0] = 0;

        foreach (var stage in stageNodeData.stages)
        {
            if (stage.stageId == 0)
                continue; 

            var stageInfo = AppManager.Instance.GetStageInfo(stage.stageId);

            stageInfo.biome = stage.biomeName;
            stageInfo.mapIndex = stage.mapIndex;

            nodeToStage[stage.mapNodeId] = stageInfo;
            nodeIdToStageId[stage.mapNodeId] = stage.stageId;
        }
    }

    public Dictionary<int, int> GetNodeToStageId() => nodeIdToStageId;
    public Dictionary<int, StageInfo> GetNodeToStage() => nodeToStage;

    public int GetStageIdByNodeId(int nodeId) => nodeToStage.TryGetValue(nodeId, out var stageInfo) ? stageInfo.id : -1;
    public StageInfo GetReplacedStageInfo(int nodeId)
    {
        if (nodeToStage.TryGetValue(nodeId, out var stageInfo))
            return stageInfo;
        else
        {
            // 스테이지 정보가 없다면 default 스테이지 정보로 던진다.
            StageInfo defaultInfo = new StageInfo();

            if (nodeId == 0)
            {
                defaultInfo.type = StageType.None;
            }

            return defaultInfo;
        }
    }

}