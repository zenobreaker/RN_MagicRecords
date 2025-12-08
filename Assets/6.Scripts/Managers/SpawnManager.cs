using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnObject
{
    public string spawnTag;
    public Vector3 spawnPos;
    public Quaternion spawnQuat;

    public void Spawn()
    {
        ObjectPooler.DeferedSpawnFromPool(spawnTag, spawnPos, spawnQuat);
    }
}


public class SpawnManager : MonoBehaviour
{
    public SO_PlayerObjects soPlayerObject;
    public SO_NPCObjects soNpcObject;

    private List<Character> spawnedPlayers = new();
    private List<Character> spawnedEnemies = new();

    public event Action OnCompleteSpawnedPlayer;
    public event Action OnCompleteSpawnedEnemy;
    public event Action OnAllPlayersDead;
    public event Action OnAllEnemiesDead;

    private void Awake()
    {
        if (soPlayerObject != null)
            soPlayerObject.Init();

        if (soNpcObject != null)
            soNpcObject.Init();
    }


    public void SpawnCharacter(int id, List<Transform> spawnPoints)
    {
        if (spawnPoints == null || spawnPoints.Count <= 0)
        {
#if UNITY_EDITOR
            Debug.Log($"Spawn Point Don't exist");
#endif
            return;
        }

#if UNITY_EDITOR
        Debug.Log($"Spawn Player");
#endif

        //TODO : 여러 캐릭터를 조작해야하면 여기를 수정
        GameObject playerObj = soPlayerObject.GetPlayerObject(id);
        if (playerObj != null)
        {
            var playerGO = Instantiate(playerObj, spawnPoints[0].position, spawnPoints[0].rotation);

            // Connect Camera
            if (Camera.main.TryGetComponent<CinemachineCamera>(out var cc))
                cc.Target.TrackingTarget = playerGO.transform;

            if(playerGO.TryGetComponent<Player>(out var player))
            {
                //TODO : class(=job) 기능이 생기면 그 아이디로 지정해야 한다.
                // Passive 등록한 이력 처리
                AppManager.Instance.OnAcquire(1, playerGO);

                // Setting Skills
                player.SetActiveSkills();

                // Setting Status
                player.SetStatus();

                // Setting Equipment 
                player.SetEquipments();

                // Add to List
                spawnedPlayers.Add(player);

                // Dead Event 
                player.OnDead += OnPlayerDead;
            }
        }

        OnCompleteSpawnedPlayer?.Invoke();
    }

    public void SpawnNPC(int groupID, List<Transform> spawnPoints)
    {
        if (spawnPoints == null || spawnPoints.Count <= 0)
        {
#if UNITY_EDITOR
            Debug.Log($"Spawn Point Don't exist");
#endif
            return;
        }

        MonsterGroupData data = AppManager.Instance.GetGroupData(groupID);
        if (data == null) return;

        foreach (var id in data.monsterIDs)
        {
#if UNITY_EDITOR
            Debug.Log($"Monster ID : {id}");
#endif
            int idx = Random.Range(0, spawnPoints.Count);

            string tag = $"NPC_{id}";
            GameObject npc = ObjectPooler.DeferedSpawnFromPool(tag, spawnPoints[idx]);

            // Set Stat 
            {
                var statData = AppManager.Instance.GetMonsterStatData(id);
                if (npc.TryGetComponent<Enemy>(out Enemy enemy))
                {
                    spawnedEnemies.Add(enemy);
                    // Set Stat 
                    enemy.SetStatData(statData);

                    // Dead Event
                    enemy.OnDead += OnEnemyDead;

                    // Return Object Pool
                    ObjectPooler.FinishSpawn(npc);
                }
            }
        }//foreach(data)

        OnCompleteSpawnedEnemy?.Invoke();
    }


    public void OnEndSpawn() { }

    private void OnPlayerDead(Character player)
    {
        spawnedPlayers.Remove(player);

        if (spawnedPlayers.Count == 0)
            OnAllPlayersDead?.Invoke();
    }

    private void OnEnemyDead(Character enemy)
    {
        spawnedEnemies.Remove(enemy);

        if (spawnedEnemies.Count == 0)
            OnAllEnemiesDead?.Invoke(); 
    }
}
