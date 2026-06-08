using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
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

    public event Action OnAllPlayersDead;
    public event Action OnAllEnemiesDead;

    // 💡 StageManager가 대기할 때 사용할 프로퍼티 추가
    public int ActivePlayerCount => spawnedPlayers.Count;
    public int ActiveEnemyCount => spawnedEnemies.Count;

    private void Awake()
    {
        if (soPlayerObject != null)
            soPlayerObject.Init();

        if (soNpcObject != null)
            soNpcObject.Init();
    }

    // 플레이어 스폰 비동기 래퍼 
    public UniTask SpawnCharacterAsync(int id, List<Transform> points, CancellationToken token)
    {
        SpawnCharacter(id, points);
        return UniTask.CompletedTask;
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
                PlayerManager.Instance.SafeInvoke(v => v.SetCurrentPlayer(player));

                //TODO : class(=job) 기능이 생기면 그 아이디로 지정해야 한다.
                int jobID = 1; 
                player.JobID = jobID;

                // Passive 등록한 이력 처리
                AppManager.Instance.SafeInvoke(v => v.OnAcquire(jobID, playerGO));

                // Setting Skills
                player.SetActiveSkills();

                // Setting Status
                player.SetStatus();

                // Setting Passive Status 
                AppManager.Instance.SafeInvoke(v=> v.OnApplyStaticEffct(jobID, playerGO));
                AppManager.Instance.SafeInvoke(v => v.OnApplyStaticEffct(Constants.GLOBAL_RECORD_JOB_ID, playerGO));

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
    }

    // 💡 풀러 콜백을 기다려주는 진짜 비동기 NPC 스폰 함수!
    public async UniTask SpawnNPCAsync(int groupID, List<Transform> spawnPoints, bool isEnemy, CancellationToken token)
    {
        if (spawnPoints == null || spawnPoints.Count <= 0) return;

        MonsterGroupData data = AppManager.Instance.GetGroupData(groupID);
        if (data == null || data.monsterIDs.Count == 0) return;

        var tcs = new UniTaskCompletionSource();
        int totalToSpawn = data.monsterIDs.Count;
        int spawnedCount = 0;

        foreach (var id in data.monsterIDs)
        {
            int idx = Random.Range(0, spawnPoints.Count);
            string tag = $"NPC_{id}";

            ObjectPooler.DeferredSpawnWithCallback(tag, spawnPoints[idx], (npc) =>
            {
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer != -1 && isEnemy)
                    SetLayerRecursively(npc, enemyLayer);

                var statData = AppManager.Instance.GetMonsterStatData(id);
                if (npc.TryGetComponent<Enemy>(out Enemy enemy))
                {
                    spawnedEnemies.Add(enemy);
                    enemy.SetStatData(statData);

                    if(AppManager.Instance != null)
                        enemy.SetGrade(AppManager.Instance?.GetMonsterData(id));
                    
                    enemy.OnDead += OnEnemyDead;

                    if (enemy.TryGetComponent<NavMeshAgent>(out var agent)) 
                        agent.enabled = true;

                    ObjectPooler.FinishSpawn(npc);
                }

                spawnedCount++;
                if (spawnedCount >= totalToSpawn)
                {
                    tcs.TrySetResult(); // 스폰 완료!
                }
            });
        }

        await tcs.Task;
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

    // 자식 오브젝트들까지 모조리 레이어를 바꿔주는 마법의 헬퍼 함수
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}
