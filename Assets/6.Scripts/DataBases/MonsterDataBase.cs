using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// https://docs.google.com/spreadsheets/d/1eG1U6iAh0-rkmFyIxXJ1lbkmK_2W9qiF3SJ7JvjummA/edit?gid=0#gid=0
/// </summary>

public enum MonsterGrade { NONE = 0, NORMAL = 1, ELITE, BOSS };

// json으로 이루어진 데이터를 가공하는 용도의 클래스 
[System.Serializable]
public class MonsterJson
{
    public uint monsterID;                 // 고유 식별 id
    public int monsterGrade;
    public string monsterName;
    public string monsterImage;
    public string monsterPrefabName;       // 프리팹 위치 경로값에 쓰일 이름 
    public int statID;
}

// MonsterJson 클래스를 리스트로 담고 있는 클래스
[System.Serializable]
public class MonsterJsonAllData
{
    public List<MonsterJson> monsterJsonData;
}

[System.Serializable]
public class MonsterData
{
    public int monsterID;
    public int statID;
    public string monsterName;
    public MonsterGrade monsterGrade;
}


[System.Serializable]
public class MonsterStatJson
{
    public int statID;
    public int monsterID;
    public int hp;
    public int attack;
    public int defense;
    public float speed;
    public int monsterGrade;
}

[System.Serializable]
public class MonsterStatJsonAllData
{
    public List<MonsterStatJson> monsterStatJson;
}

[System.Serializable]
public class MonsterStatData
{
    public int id;
    public int monsterID;
    public float hp;
    public float attack;
    public float defense;
    public float speed;

    public MonsterStatData(MonsterStatData other)
    {
        id = other.id; 
        monsterID = other.monsterID;
        hp = other.hp;
        attack = other.attack;
        defense = other.defense;
        speed = other.speed;
    }

    public MonsterStatData() { }
}


[System.Serializable]
public class MonsterGroupData
{
    public int id;
    public List<int> monsterIDs;
    public List<int> counts; 
}

[System.Serializable]
public class MonsterGroupJson
{
    public int id;
    public string monsterID;
    public string count;
}


[System.Serializable]
public class MonsterGroupJsonAllData
{
    public List<MonsterGroupJson> monsterGroupJson;
}


public class MonsterDataBase : MonoBehaviour
{
    [Header("몬스터 Stat Data Json")]
    [SerializeField] private TextAsset monsterStatJson;
    [SerializeField] private Dictionary<int, MonsterStatData> monsterStatDatas = new();

    [Header("몬스터 Data Json")]
    [SerializeField] private TextAsset monsterJson;
    [SerializeField] private List<MonsterData> monsterDatas = new();

    [Header("몬스터 Group Data Json")]
    [SerializeField] private TextAsset monsterGroupJson;
    [SerializeField] private Dictionary<int, MonsterGroupData> monsterGroupDatas = new();

    public void InitializeData()
    {
        Debug.Log("Monster Database Init");

        InitializeMonsterGroupData();
        InitializeMonsterStatData();
        InitializeMonsterData();


        Debug.Log("===================================================");
        Debug.Log($"Complete Message => monsterStatDatas: {monsterStatDatas.Count}");
        Debug.Log($"Complete Message => monsterGroupJson : {monsterGroupDatas.Count}");
        Debug.Log($"Complete Message => monsterDatas: {monsterDatas.Count}");
    }

    public void InitializeMonsterData()
    {
        if (monsterJson == null) return;

        JsonLoader.LoadJsonList<MonsterJsonAllData, MonsterJson, MonsterData>
            (
                monsterJson,
                root => root.monsterJsonData,

                json =>
                {
                    var monsterData = new MonsterData
                    {
                        monsterID = (int)json.monsterID,
                        statID = json.statID,
                        monsterName = json.monsterName,
                        monsterGrade = (MonsterGrade)json.monsterGrade,
                    };
                    return monsterData;
                },

                monster =>
                {
                    monsterDatas.Add(monster);
                }
            );
    }

    public void InitializeMonsterStatData()
    {
        if (monsterStatJson == null)
            return;
        
        JsonLoader.LoadJsonList<MonsterStatJsonAllData, MonsterStatJson, MonsterStatData>
            (
                monsterStatJson,
                root => root.monsterStatJson,

                json =>
                {
                    var monsterStatData = new MonsterStatData
                    {
                        id = json.statID,
                        monsterID = json.monsterID,
                        hp = json.hp,
                        attack = json.attack,
                        defense = json.defense,
                        speed = json.speed,
                    };
                    return monsterStatData;
                },

                monsterStat =>
                {
                    monsterStatDatas.TryAdd(monsterStat.monsterID, monsterStat);
                }
            );
    }

    public void InitializeMonsterGroupData()
    {
        if (monsterGroupJson == null) return;

        JsonLoader.LoadJsonList<MonsterGroupJsonAllData, MonsterGroupJson, MonsterGroupData>
            (
                monsterGroupJson,
                root => root.monsterGroupJson,
                json =>
                {
                    var monsterGroupData = new MonsterGroupData
                    {
                        id = json.id,
                        monsterIDs = JsonLoader.ParseIntList(json.monsterID),
                        counts = JsonLoader.ParseIntList(json.count),
                    };
                    return monsterGroupData;
                },
                monsterGroupData =>
                {
                    monsterGroupDatas.TryAdd(monsterGroupData.id, monsterGroupData);
                }
            );

    }

    public MonsterGroupData GetMonsterGroupData(int groupID)
    {
        return (monsterGroupDatas.TryGetValue(groupID, out var monsterGroupData)) 
            ? monsterGroupData : null;
    }

    public MonsterStatData GetMonsterStatData(int monsterID)
    {
        return (monsterStatDatas.TryGetValue(monsterID, out var monsterStatData))
            ? new MonsterStatData(monsterStatData) : null;
    }

}
