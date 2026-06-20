using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class StageReplacer
{
    private Dictionary<int, MapNodeInfo> nodeToInfo = new(); // key : node id value : stage

    public string currentChapterBiome;
    private int currentChapter; // 스테이지 ID를 뽑기 위한 챕터 기억용 

    public void StartChapter(int chapter)
    {
        // 1챕터에 진입하면 '숲'인지 '습지'인지 전체 테마를 하나 뽑아둡니다.
        currentChapter = chapter;
        currentChapterBiome = AppManager.Instance.GetRandomBiome(chapter);
        // 이후 노드(맵)들을 생성하고 StageReplacer.AssignStages()를 호출!
    }

    public void AssignStages(List<List<MapNode>> levels)
    {
        nodeToInfo.Clear();

        SO_Biome biomeData = AppManager.Instance.GetBiomeData(currentChapterBiome);
        int maxMapCount = biomeData.possibleRoomPrefabs.Count;
        // 💡 이벤트(물음표) 노드가 등장할 확률 (예: 25%)
        float eventChance = 0.25f;

        for (int level = 0; level < levels.Count; level++)
        {
            if (level == 0)
            {
                nodeToInfo[0] = new MapNodeInfo
                {
                    nodeId = 0,
                    type = StageType.None,
                    contentId = 0,
                    mapIndex = 0
                }; // 시작 노드
                continue;
            }

            bool isLastLevel = (level == levels.Count - 1);

            foreach (var node in levels[level])
            {
                MapNodeInfo info = new MapNodeInfo();
                info.nodeId = node.id;
                info.biome = currentChapterBiome;

                if (isLastLevel)
                {
                    // 보스용 스테이지 풀에서 랜덤추출 
                    info.type = StageType.Boss_Combat;
                    StageInfo bossStage = AppManager.Instance.CreateRandomBossStage(currentChapter);
                    if (bossStage == null) continue;

                    info.contentId = bossStage.id; 
                    info.mapIndex = -1; // 보스 전용 맵
                    info.clearRewardId = bossStage.clearRewardId; 
                }
                else
                {
                    // 이벤트 vs 전투 분기
                    if(level > 1 && UnityEngine.Random.value < eventChance)
                    {
                        info.type = StageType.Event;
                        info.contentId = AppManager.Instance.GetRandomExploreEvent(currentChapter); ;
                        info.mapIndex = -1;
                    }
                    else
                    {
                        info.type = StageType.Combat;
                        StageInfo stage = AppManager.Instance.CreateRandomStage(currentChapter);
                        if (stage == null) continue;

                        info.contentId = stage.id;
                        info.mapIndex = UnityEngine.Random.Range(0, maxMapCount);
                        info.clearRewardId = stage.clearRewardId;
                    }
                }
                
                nodeToInfo[node.id] = info;
            }
        }
    }

    public void RestoreStages(StageNodeData stageNodeData)
    {
        nodeToInfo.Clear();
        nodeToInfo[0] = new MapNodeInfo { nodeId = 0, type = StageType.None, contentId = 0, mapIndex = 0 };

        // 💡 [수정됨] 복잡한 분기 처리 없이 저장된 껍데기를 그대로 꺼내옵니다.
        foreach (MapNodeInfo savedInfo in stageNodeData.nodeInfos)
        {
            if (savedInfo.contentId == 0 && savedInfo.nodeId == 0) continue;

            // 저장된 껍데기를 깊은 복사(Copy)해서 딕셔너리에 연결!
            nodeToInfo[savedInfo.nodeId] = savedInfo.Copy();
        }
    }

    public Dictionary<int, MapNodeInfo> GetNodeToInfo() => nodeToInfo;

    public MapNodeInfo GetReplacedNodeInfo(int nodeId)
    {
        if (nodeToInfo.TryGetValue(nodeId, out var info))
            return info;
        else
            return new MapNodeInfo { nodeId = nodeId, type = StageType.None  };
    }
}