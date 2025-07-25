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
}



public class MonsterDataBase : MonoBehaviour
{
    [Header("몬스터 Stat Data Json")]
    [SerializeField] private TextAsset monsterStatJson;
    [SerializeField] private Dictionary<int, MonsterStatData> monsterStatDatas = new();


    [Header("몬스터 Data Json")]
    [SerializeField] private TextAsset monsterJson;
    [SerializeField] private List<MonsterData> monsterDatas = new();

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
        
        Debug.Log("Monster Database Init");

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
                    monsterStatDatas.TryAdd(monsterStat.id, monsterStat);
                }
            );

        Debug.Log("===================================================");
        Debug.Log($"Complete Message => monsterStatDatas: {monsterStatDatas.Count}");
    }

}
