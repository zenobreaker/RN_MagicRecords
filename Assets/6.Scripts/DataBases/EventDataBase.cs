using JetBrains.Annotations;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
    public EventRewardType FailType;
    public int FailValue;
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
    public int choiceIndex;
    public string textkey;
    public string costtype;
    public int costvalue;
    public string rewardtype;
    public int rewardvalue;
    public int probability;
    public string failtype;
    public int failvalue;
}

[System.Serializable]
public class EventInfo : InfoBase
{
    public string nameKey;
    public string descriptionKey;
    public List<EventChoice> eventChoices = new List<EventChoice>();
}

public sealed class EventDataBase : DataBase
{
    private Dictionary<int, EventInfo> eventInfos = new Dictionary<int, EventInfo>();

    public override void Initialize()
    {
        eventInfos.Clear();

        if (jsonAsset == null)
        {
            Debug.LogError("[EventDataBase] jsonAsset이 할당되지 않았습니다!");
            return;
        }

        // 💡 람다식 밖에서 현재 조립 중인 이벤트 ID를 추적합니다. (Closure 활용)
        int currentEventId = -1;

        JsonLoader.LoadJsonList<EventInfoJsonRoot, EventInfoJson, EventInfoJson>(
            jsonAsset,
            // 1. extractListFunc: 루트 객체에서 리스트를 뽑아냅니다.
            root => root.eventList,

            // 2. converFunc: 변환 없이 그대로 통과시킵니다.
            json => json,

            // 3. onResult: 한 줄(row)씩 읽어올 때마다 1:N 조립 로직을 수행합니다!
            row =>
            {
                // [이벤트 제목 줄인지 확인]
                if (row.id > 0)
                {
                    currentEventId = row.id;

                    EventInfo newEvent = new EventInfo();
                    newEvent.id = row.id;
                    newEvent.nameKey = row.name;
                    newEvent.descriptionKey = row.description;

                    eventInfos[currentEventId] = newEvent;
                }

                // [선택지 데이터 조립 및 추가]
                if (currentEventId > 0 && eventInfos.ContainsKey(currentEventId))
                {
                    EventChoice newChoice = new EventChoice();
                    newChoice.ChoiceIndex = row.choiceIndex;
                    newChoice.TextKey = row.textkey;

                    if (Enum.TryParse(row.costtype, out EventCostType parsedCost))
                        newChoice.CostType = parsedCost;

                    newChoice.CostValue = row.costvalue;

                    if (Enum.TryParse(row.rewardtype, out EventRewardType parsedReward))
                        newChoice.RewardType = parsedReward;

                    newChoice.RewardValue = row.rewardvalue;

                    // 확률이 0으로 오면 100으로 보정
                    newChoice.Probability = row.probability == 0 ? 100 : row.probability;

                    if (Enum.TryParse(row.failtype, out EventRewardType parsedFail))
                        newChoice.FailType = parsedFail;

                    newChoice.FailValue = row.failvalue;

                    // 현재 조립 중인 이벤트의 선택지 리스트에 쏙!
                    eventInfos[currentEventId].eventChoices.Add(newChoice);
                }
            }
        );

        Debug.Log($"[EventDataBase] 성공적으로 {eventInfos.Count}개의 이벤트를 로드했습니다.");
    }

    public EventInfo GetEventInfo(int eventId)
    {
        if (eventInfos.TryGetValue(eventId, out EventInfo info))
        {
            return info;
        }
        return null;
    }
}
