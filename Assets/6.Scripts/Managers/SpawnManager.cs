using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class SpawnObject
{
    public string spawnTag;
    public Vector3 spawnPos;
    public Quaternion spawnQuat;

    public void Spawn()
    {
        ObjectPooler.DeferredSpawnFromPool(spawnTag, spawnPos, spawnQuat);
    }
}


public sealed class SpawnManager : MonoBehaviour
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
                // Char ID 
                player.CharID = id; 
                PlayerManager.Instance?.SetCurrentPlayer(player);

                //TODO : class(=job) 기능이 생기면 그 아이디로 지정해야 한다.
                int jobID = 1; 
                player.JobID = jobID;

                // Passive 등록한 이력 처리
                AppManager.Instance?.OnAcquire(jobID, playerGO);

                // Setting Skills
                player.SetActiveSkills();

                // Setting Status
                player.SetStatus();

                // Setting Passive Status 
                AppManager.Instance?.OnApplyStaticEffct(jobID, playerGO);
                AppManager.Instance?.OnApplyStaticEffct(Constants.GLOBAL_RECORD_JOB_ID, playerGO); 

                // Setting Equipment 
                player.SetEquipments();
                
                // Recalculate Status 
                if(player.TryGetComponent<StatusComponent>(out var status))
                {
                    status.RefreshAllStatus();
                }

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

        // 1. 이번에 스폰할 총 몬스터 개수 확인
        int totalToSpawn = data.monsterIDs.Count;
        int currentSpawnedCount = 0;

        foreach (var id in data.monsterIDs)
        {
#if UNITY_EDITOR
            Debug.Log($"Monster ID : {id}");
#endif
            int idx = Random.Range(0, spawnPoints.Count);

            string tag = $"NPC_{id}";


            ObjectPooler.DeferredSpawnWithCallback(tag, spawnPoints[idx], 
                (npc) =>
            // Set Stat 
            {
                var statData = AppManager.Instance.GetMonsterStatData(id);
                if (npc.TryGetComponent<Enemy>(out Enemy enemy))
                {
                    spawnedEnemies.Add(enemy);
                    // Set Stat 
                    enemy.SetStatData(statData);

                    // Set Grade 
                    enemy.SetGrade(AppManager.Instance?.GetMonsterData(id));

                    // Dead Event
                    enemy.OnDead += OnEnemyDead;

                    // Set Navigate 
                    var agent = enemy.GetComponent<NavMeshAgent>();
                    if (agent != null)
                        agent.enabled = true;

                    // Return Object Pool
                    ObjectPooler.FinishSpawn(npc);
                }

                // 2. 콜백이 실행될 때 마다 카운트 증가 
                currentSpawnedCount++; 

                // 3. 마지막 몬스터까지 세팅이 완료되었다면 이벤트 발생 
                if(currentSpawnedCount >= totalToSpawn)
                {
                    Debug.Log($"[SpawnManager] 모든 {totalToSpawn}마리의 몬스터 스폰 및 세팅 완료!");
                    OnCompleteSpawnedEnemy?.Invoke();
                }

            }); // Defered

        }//foreach(data)
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
