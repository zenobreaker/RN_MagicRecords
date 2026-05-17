using JetBrains.Annotations;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;


// 💡 2. 파싱된 선택지 데이터 (UI 및 로직용)
[System.Serializable]
public class EventChoice
{
    public int ChoiceIndex;
    public string TextKey;
    public EventCostType CostType;
    public int CostValue;
    public EventRewardType RewardType;
    public int RewardValue;
    public int Probability;
    public string ResultTextKey;
    public string FailTextKey;
    public EventRewardType FailType;
    public int FailValue;
    public bool ChoiceIsActive;
}

// 💡 JsonLoader의 TJsonRoot 역할을 해줄 최상위 래퍼 클래스
[System.Serializable]
public class EventInfoJsonRoot
{
    public List<EventInfoJson> eventList; 
}

[System.Serializable]
public class EventInfoJson : InfoJson
{
    public string name;
    public string description;
    public int chapter;
    public int choiceIndex;
    public string textkey;
    public string costtype;
    public int costvalue;
    public string rewardtype;
    public int rewardvalue;
    public int probability;
    public string resulttextkey;
    public string failtextkey;
    public string failtype;
    public int failvalue;
    public string imagekey;
    public int event_is_active;
    public int choice_is_active; 
}

[System.Serializable]
public class EventInfo : InfoBase
{
    public int chapter;    
    public string nameKey;
    public string descriptionKey;
    public string imageKey;
    public bool isActive;
    public List<EventChoice> eventChoices = new List<EventChoice>();
}

public sealed class EventDataBase : DataBase
{
    // 전체 이벤트 보관소 (ID로 찾기용)
    private Dictionary<int, EventInfo> eventInfos = new Dictionary<int, EventInfo>();

    // 💡 챕터별 이벤트 분류 테이블 (Key: Chapter, Value: List of Event IDs)
    // Chapter 0 = 공통 이벤트 / Chapter 1~ = 챕터 전용 이벤트
    private Dictionary<int, List<int>> chapterEventTable = new Dictionary<int, List<int>>();

    public override void Initialize()
    {
        eventInfos.Clear();
        chapterEventTable.Clear();

        if (jsonAsset == null)
        {
            Debug.LogError("[EventDataBase] jsonAsset이 할당되지 않았습니다!");
            return;
        }

        int currentEventId = -1;
        // 💡 현재 파싱 중인 이벤트가 '통째로 비활성화' 상태인지 기억하기 위한 변수
        bool isCurrentEventDisabled = false;

        JsonLoader.LoadJsonList<EventInfoJsonRoot, EventInfoJson, EventInfoJson>(
            jsonAsset,
            root => root.eventList,
            json => json,
            row =>
            {
                // --- 1. 이벤트 전체 활성화 체크 (대표 행인 ChoiceIndex 1에서 결정) ---
                if (row.choiceIndex == 1)
                {
                    // 0이면 비활성이므로 이 이벤트는 이후 줄(선택지들)도 모두 무시하도록 플래그 설정
                    isCurrentEventDisabled = (row.event_is_active == 0);

                    if (isCurrentEventDisabled) return;
                }

                // 이벤트 자체가 비활성 상태면 아래 로직 수행 안 함
                if (isCurrentEventDisabled) return;

                // --- 2. 이벤트 기본 정보 조립 (새로운 ID 등장 시) ---
                if (row.id > 0)
                {
                    currentEventId = row.id;

                    EventInfo newEvent = new EventInfo();
                    newEvent.id = row.id;
                    newEvent.chapter = row.chapter;
                    newEvent.nameKey = row.name;
                    newEvent.descriptionKey = row.description;
                    newEvent.imageKey = row.imagekey;
                    newEvent.isActive = true;

                    eventInfos[currentEventId] = newEvent;

                    if (!chapterEventTable.ContainsKey(newEvent.chapter))
                    {
                        chapterEventTable[newEvent.chapter] = new List<int>();
                    }
                    chapterEventTable[newEvent.chapter].Add(currentEventId);
                }

                // --- 3. 개별 선택지 데이터 조립 ---
                if (currentEventId > 0 && eventInfos.ContainsKey(currentEventId))
                {
                    EventChoice newChoice = new EventChoice();
                    newChoice.ChoiceIndex = row.choiceIndex;
                    newChoice.TextKey = row.textkey;

                    // 💡 개별 선택지 활성화 여부 저장 (UI 스크립트에서 사용)
                    newChoice.ChoiceIsActive = (row.choice_is_active == 1);

                    // 나머지 데이터 세팅
                    if (Enum.TryParse(row.costtype, out EventCostType parsedCost))
                        newChoice.CostType = parsedCost;

                    newChoice.CostValue = row.costvalue;

                    if (Enum.TryParse(row.rewardtype, out EventRewardType parsedReward))
                        newChoice.RewardType = parsedReward;

                    newChoice.RewardValue = row.rewardvalue;
                    newChoice.Probability = row.probability == 0 ? 100 : row.probability;
                    newChoice.ResultTextKey = row.resulttextkey;
                    newChoice.FailTextKey = row.failtextkey;

                    if (Enum.TryParse(row.failtype, out EventRewardType parsedFail))
                        newChoice.FailType = parsedFail;

                    newChoice.FailValue = row.failvalue;

                    eventInfos[currentEventId].eventChoices.Add(newChoice);
                }
            }
        );

        Debug.Log($"[EventDataBase] 로드 완료. 활성 이벤트 수: {eventInfos.Count}");
    }

    public EventInfo GetEventInfo(int eventId)
    {
        if (eventInfos.TryGetValue(eventId, out EventInfo info))
        {
            return info;
        }
        return null;
    }

    public int GetRandomEventID(int chapter)
    {
        // 이번 맵 생성에서 뽑힐 수 있는 후보군 풀(Pool)
        List<int> validEventPool = new List<int>();

        // 1. 공통 이벤트 (Chapter 0) 긁어오기
        if (chapterEventTable.TryGetValue(0, out List<int> commonEvents))
        {
            validEventPool.AddRange(commonEvents);
        }

        // 2. 현재 챕터(예: 1챕터) 전용 이벤트 긁어오기
        if (chapter > 0 && chapterEventTable.TryGetValue(chapter, out List<int> chapterSpecificEvents))
        {
            validEventPool.AddRange(chapterSpecificEvents);
        }

        // 3. 예외 처리 (이벤트가 하나도 없을 경우)
        if (validEventPool.Count == 0)
        {
            Debug.LogWarning($"[EventDataBase] 챕터 {chapter}에 등장할 수 있는 이벤트가 아예 없습니다!");
            return -1; // 또는 기본 이벤트를 강제 할당 (예: 1001)
        }

        // 4. 합쳐진 후보군 중에서 무작위로 하나 픽!
        int randomIndex = UnityEngine.Random.Range(0, validEventPool.Count);
        return validEventPool[randomIndex];
    }
}
